namespace UserService.DTOs;

public class UserQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
    public bool IncludeDeleted { get; set; } = false;
} 