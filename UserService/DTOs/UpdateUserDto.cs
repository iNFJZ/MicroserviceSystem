using System.ComponentModel.DataAnnotations;
using UserService.Models;

namespace UserService.DTOs;

public class UpdateUserDto
{
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]*$", ErrorMessage = "Full name can only contain letters, spaces, and Vietnamese characters")]
    public string? FullName { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public UserStatus? Status { get; set; }

    public bool? IsVerified { get; set; }

    public string? ProfilePicture { get; set; }
} 