using System.ComponentModel.DataAnnotations;
using UserService.Models;

namespace UserService.DTOs;

public class UpdateUserDto
{
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]*$", ErrorMessage = "Full name can only contain letters, spaces, and Vietnamese characters")]
    public string? FullName { get; set; }

    [StringLength(20)]
    [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Phone number must be 10-11 digits and contain only numbers")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public UserStatus? Status { get; set; }

    public bool? IsVerified { get; set; }

    [StringLength(200000)]
    public string? ProfilePicture { get; set; }
} 