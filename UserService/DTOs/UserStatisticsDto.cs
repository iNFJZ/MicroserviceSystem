namespace UserService.DTOs;

public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int BannedUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int UnverifiedUsers { get; set; }
    public int DeletedUsers { get; set; }
    public int GoogleUsers { get; set; }
    public int LocalUsers { get; set; }
    public int RecentUsers { get; set; }
} 