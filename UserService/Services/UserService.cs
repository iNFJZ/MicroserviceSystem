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
    private readonly IUserCacheService _userCacheService;

    public UserService(
        IUserRepository userRepository, 
        IMapper mapper, 
        ISessionService sessionService, 
        IEmailMessageService emailMessageService,
        IUserCacheService userCacheService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _sessionService = sessionService;
        _emailMessageService = emailMessageService;
        _userCacheService = userCacheService;
    }

    public async Task<(List<UserDto> Users, int TotalCount, int TotalPages)> GetUsersAsync(UserQueryDto query)
    {
        List<User> users;
        
        if (query.IncludeDeleted)
        {
            users = await _userRepository.GetAllAsync();
        }
        else
        {
            users = await _userRepository.GetAllActiveAsync();
        }

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

    public async Task<(List<UserDto> Users, int TotalCount, int TotalPages)> GetDeletedUsersAsync(UserQueryDto query)
    {
        var users = await _userRepository.GetAllDeletedAsync();

        if (!string.IsNullOrEmpty(query.Search))
        {
            users = users.Where(u => 
                u.Username.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                (u.FullName != null && u.FullName.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
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
                "deletedat" => query.SortOrder?.ToLower() == "desc"
                    ? users.OrderByDescending(u => u.DeletedAt).ToList()
                    : users.OrderBy(u => u.DeletedAt).ToList(),
                "createdat" => query.SortOrder?.ToLower() == "desc"
                    ? users.OrderByDescending(u => u.CreatedAt).ToList()
                    : users.OrderBy(u => u.CreatedAt).ToList(),
                _ => users.OrderByDescending(u => u.DeletedAt).ToList()
            };
        }
        else
        {
            users = users.OrderByDescending(u => u.DeletedAt).ToList();
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
        var cachedUser = await _userCacheService.GetUserByIdAsync(id);
        if (cachedUser != null)
        {
            return _mapper.Map<UserDto>(cachedUser);
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            await _userCacheService.SetUserAsync(user, TimeSpan.FromMinutes(30));
        }
        
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var cachedUser = await _userCacheService.GetUserByEmailAsync(email);
        if (cachedUser != null)
        {
            return _mapper.Map<UserDto>(cachedUser);
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user != null)
        {
            await _userCacheService.SetUserAsync(user, TimeSpan.FromMinutes(30));
        }
        
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user != null)
        {
            await _userCacheService.SetUserAsync(user, TimeSpan.FromMinutes(30));
        }
        
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
            var newStatus = dto.Status.Value;
            var oldStatus = user.Status;
            user.Status = newStatus;
            if (newStatus == UserStatus.Banned && user.DeletedAt == null)
            {
                user.DeletedAt = DateTime.UtcNow;
            }
            else if (oldStatus == UserStatus.Banned && newStatus != UserStatus.Banned && user.DeletedAt != null)
            {
                user.DeletedAt = null;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.ProfilePicture))
        {
            if (dto.ProfilePicture.Length > 500)
                throw new ArgumentException("Profile picture URL must be less than 500 characters");
            user.ProfilePicture = dto.ProfilePicture.Trim();
        }

        if (user.Status == UserStatus.Active || user.Status == UserStatus.Suspended)
        {
            user.IsVerified = true;
        } else {
            user.IsVerified = false;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        
        await _userCacheService.SetUserAsync(user, TimeSpan.FromMinutes(30));
        
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return false;

        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserStatus.Banned;
        user.IsVerified = false;
        await _userRepository.UpdateAsync(user);

        await _userCacheService.DeleteUserAsync(userId);

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
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || !user.IsDeleted)
            return false;

        var result = await _userRepository.RestoreAsync(id);
        if (result)
        {
            await _userCacheService.SetUserAsync(user, TimeSpan.FromMinutes(30));
            
            await _emailMessageService.PublishRestoreAccountNotificationAsync(new RestoreAccountEmailEvent
            {
                To = user.Email,
                Username = user.FullName ?? user.Username,
                RestoredAt = DateTime.UtcNow,
                Reason = "Account restored by administrator"
            });
        }
        return result;
    }

    public async Task<UserDto> CreateUserAsync(User user)
    {
        var createdUser = await _userRepository.AddAsync(user);
        
        await _userCacheService.SetUserAsync(createdUser, TimeSpan.FromMinutes(30));
        
        return _mapper.Map<UserDto>(createdUser);
    }

    public async Task<object> GetStatisticsAsync()
    {
        var users = await _userRepository.GetAllActiveAsync();
        
        var totalUsers = users.Count;
        var activeUsers = users.Count(u => u.Status == UserStatus.Active);
        var inactiveUsers = users.Count(u => u.Status == UserStatus.Inactive);
        var bannedUsers = users.Count(u => u.Status == UserStatus.Banned);
        var verifiedUsers = users.Count(u => u.IsVerified);
        var unverifiedUsers = users.Count(u => !u.IsVerified);
        
        var usersByProvider = users.GroupBy(u => u.LoginProvider)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var recentUsers = users.Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(7)).Count();
        
        return new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = inactiveUsers,
            BannedUsers = bannedUsers,
            VerifiedUsers = verifiedUsers,
            UnverifiedUsers = unverifiedUsers,
            UsersByProvider = usersByProvider,
            RecentUsers = recentUsers
        };
    }
} 