namespace SynTA.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalUserStories { get; set; }
        public int TotalGherkinScenarios { get; set; }
        public int TotalCypressScripts { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsersThisMonth { get; set; }
        public List<RecentActivity> RecentActivities { get; set; } = new();
        public List<UserStatistic> TopUsers { get; set; } = new();
    }

    public class RecentActivity
    {
        public string UserEmail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class UserStatistic
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ProjectCount { get; set; }
        public int UserStoryCount { get; set; }
        public int TestCount { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
