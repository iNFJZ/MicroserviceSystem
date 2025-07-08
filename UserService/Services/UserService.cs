using AutoMapper;
using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;
using Shared.EmailModels;
using UserService.Services;

namespace UserService.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ISessionService _sessionService;
    private readonly IEmailMessageService _emailMessageService;

    public UserService(IUserRepository userRepository, IMapper mapper, ISessionService sessionService, IEmailMessageService emailMessageService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _sessionService = sessionService;
        _emailMessageService = emailMessageService;
    }

    public async Task<(List<UserDto> Users, int TotalCount, int TotalPages)> GetUsersAsync(UserQueryDto query)
    {
        var users = await _userRepository.GetAllActiveAsync();

        if (!string.IsNullOrEmpty(query.Search))
        {
            users = users.Where(u => 
                u.Username.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                (u.FullName != null && u.FullName.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<UserStatus>(query.Status, true, out var userStatus))
        {
            users = users.Where(u => u.Status == userStatus).ToList();
        }

        if (!string.IsNullOrEmpty(query.Role))
        {
            users = users.Where(u => u.LoginProvider.Equals(query.Role, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrEmpty(query.SortBy))
        {
            users = query.SortBy.ToLower() switch
            {
                "username" => query.SortOrder?.ToLower() == "desc" 
                    ? users.OrderByDescending(u => u.Username).ToList()
                    : users.OrderBy(u => u.Username).ToList(),
                "email" => query.SortOrder?.ToLower() == "desc"
                    ? users.OrderByDescending(u => u.Email).ToList()
                    : users.OrderBy(u => u.Email).ToList(),
                "fullname" => query.SortOrder?.ToLower() == "desc"
                    ? users.OrderByDescending(u => u.FullName).ToList()
                    : users.OrderBy(u => u.FullName).ToList(),
                "createdat" => query.SortOrder?.ToLower() == "desc"
                    ? users.OrderByDescending(u => u.CreatedAt).ToList()
                    : users.OrderBy(u => u.CreatedAt).ToList(),
                "lastloginat" => query.SortOrder?.ToLower() == "desc"
                    ? users.OrderByDescending(u => u.LastLoginAt).ToList()
                    : users.OrderBy(u => u.LastLoginAt).ToList(),
                _ => users.OrderBy(u => u.CreatedAt).ToList()
            };
        }
        else
        {
            users = users.OrderByDescending(u => u.CreatedAt).ToList();
        }

        var totalCount = users.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
        var skip = (query.Page - 1) * query.PageSize;
        var pagedUsers = users.Skip(skip).Take(query.PageSize).ToList();

        var userDtos = _mapper.Map<List<UserDto>>(pagedUsers);

        return (userDtos, totalCount, totalPages);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            return false;

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.FullName, @"^[a-zA-ZÀ-ỹ\s]*$"))
                throw new ArgumentException("Full name can only contain letters, spaces, and Vietnamese characters");
            user.FullName = dto.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            if (dto.PhoneNumber.Length > 11)
                throw new ArgumentException("Phone number must be less than 11 characters");
            user.PhoneNumber = dto.PhoneNumber.Trim();
        }

        if (dto.DateOfBirth.HasValue)
        {
            if (dto.DateOfBirth.Value > DateTime.UtcNow)
                throw new ArgumentException("Date of birth cannot be in the future");
            user.DateOfBirth = dto.DateOfBirth.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.Address))
        {
            if (dto.Address.Length > 200)
                throw new ArgumentException("Address must be less than 200 characters");
            user.Address = dto.Address.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Bio))
        {
            if (dto.Bio.Length > 500)
                throw new ArgumentException("Bio must be less than 500 characters");
            user.Bio = dto.Bio.Trim();
        }

        if (dto.Status.HasValue)
        {
            user.Status = dto.Status.Value;
        }

        if (dto.IsVerified.HasValue)
        {
            user.IsVerified = dto.IsVerified.Value;
            if (dto.IsVerified.Value)
                user.Status = UserStatus.Active;
            else
                user.Status = UserStatus.Inactive;
        }

        if (!string.IsNullOrWhiteSpace(dto.ProfilePicture))
        {
            if (dto.ProfilePicture.Length > 500)
                throw new ArgumentException("Profile picture URL must be less than 500 characters");
            user.ProfilePicture = dto.ProfilePicture.Trim();
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return false;

        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserStatus.Banned;
        await _userRepository.UpdateAsync(user);

        await _sessionService.RemoveAllUserSessionsAsync(user.Id);
        await _sessionService.RemoveAllActiveTokensForUserAsync(user.Id);
        await _sessionService.SetUserLoginStatusAsync(user.Id, false);

        await _emailMessageService.PublishDeactivateAccountNotificationAsync(new DeactivateAccountEmailEvent
        {
            To = user.Email,
            Username = user.FullName ?? user.Username,
            DeactivatedAt = DateTime.UtcNow,
            Reason = "Account deactivated by administrator"
        });

        return true;
    }

    public async Task<bool> RestoreUserAsync(Guid id)
    {
        return await _userRepository.RestoreAsync(id);
    }

    public async Task<UserStatisticsDto> GetUserStatisticsAsync()
    {
        var users = await _userRepository.GetAllAsync();
        
        return new UserStatisticsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.Status == UserStatus.Active && !u.IsDeleted),
            InactiveUsers = users.Count(u => u.Status == UserStatus.Inactive && !u.IsDeleted),
            SuspendedUsers = users.Count(u => u.Status == UserStatus.Suspended && !u.IsDeleted),
            BannedUsers = users.Count(u => u.Status == UserStatus.Banned && !u.IsDeleted),
            VerifiedUsers = users.Count(u => u.IsVerified && !u.IsDeleted),
            UnverifiedUsers = users.Count(u => !u.IsVerified && !u.IsDeleted),
            DeletedUsers = users.Count(u => u.IsDeleted),
            GoogleUsers = users.Count(u => u.LoginProvider == "Google" && !u.IsDeleted),
            LocalUsers = users.Count(u => u.LoginProvider == "Local" && !u.IsDeleted),
            RecentUsers = users.Count(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !u.IsDeleted)
        };
    }

    public async Task<UserDto> CreateUserAsync(User user)
    {
        var createdUser = await _userRepository.AddAsync(user);
        return _mapper.Map<UserDto>(createdUser);
    }
} 