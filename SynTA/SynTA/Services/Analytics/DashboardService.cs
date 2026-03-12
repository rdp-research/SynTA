using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SynTA.Areas.Admin.Models;
using SynTA.Data;
using SynTA.Models.Domain;

namespace SynTA.Services.Analytics;

/// <summary>
/// Service responsible for calculating dashboard statistics and analytics.
/// Extracts complex business logic from controllers for better testability and maintainability.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardStatisticsAsync()
    {
        _logger.LogInformation("Calculating dashboard statistics");

        try
        {
            var viewModel = new DashboardViewModel();

            // Get total counts - run in parallel for better performance
            var totalUsersTask = _userManager.Users.CountAsync();
            var totalProjectsTask = _context.Projects.CountAsync();
            var totalUserStoriesTask = _context.UserStories.CountAsync();
            var totalGherkinScenariosTask = _context.GherkinScenarios.CountAsync();
            var totalCypressScriptsTask = _context.CypressScripts.CountAsync();

            await Task.WhenAll(
                totalUsersTask,
                totalProjectsTask,
                totalUserStoriesTask,
                totalGherkinScenariosTask,
                totalCypressScriptsTask
            );

            viewModel.TotalUsers = totalUsersTask.Result;
            viewModel.TotalProjects = totalProjectsTask.Result;
            viewModel.TotalUserStories = totalUserStoriesTask.Result;
            viewModel.TotalGherkinScenarios = totalGherkinScenariosTask.Result;
            viewModel.TotalCypressScripts = totalCypressScriptsTask.Result;

            // Calculate monthly statistics
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            viewModel.NewUsersThisMonth = await CalculateNewUsersThisMonthAsync(firstDayOfMonth);
            viewModel.ActiveUsersThisMonth = await CalculateActiveUsersThisMonthAsync(firstDayOfMonth);

            // Get top users by content
            viewModel.TopUsers = await GetTopUsersByContentAsync();

            // Get recent activities
            viewModel.RecentActivities = await GetRecentActivitiesAsync();

            _logger.LogInformation("Dashboard statistics calculated successfully - TotalUsers: {TotalUsers}, TotalProjects: {TotalProjects}",
                viewModel.TotalUsers, viewModel.TotalProjects);

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dashboard statistics");
            throw;
        }
    }

    /// <summary>
    /// Calculates the number of new users this month.
    /// Uses ApplicationUser.CreatedAt property for accurate user registration tracking.
    /// </summary>
    private async Task<int> CalculateNewUsersThisMonthAsync(DateTime firstDayOfMonth)
    {
        try
        {
            // Count users who were created this month
            var newUserCount = await _userManager.Users
                .Where(u => u.CreatedAt >= firstDayOfMonth)
                .CountAsync();

            return newUserCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating new users this month");
            return 0;
        }
    }

    /// <summary>
    /// Calculates the number of active users this month (users who created or updated content).
    /// </summary>
    private async Task<int> CalculateActiveUsersThisMonthAsync(DateTime firstDayOfMonth)
    {
        try
        {
            var activeUserIds = await _context.Projects
                .Where(p => p.CreatedAt >= firstDayOfMonth || (p.UpdatedAt.HasValue && p.UpdatedAt >= firstDayOfMonth))
                .Select(p => p.UserId)
                .Distinct()
                .CountAsync();

            return activeUserIds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating active users this month");
            return 0;
        }
    }

    /// <summary>
    /// Gets the top 5 users by total test count (Gherkin scenarios + Cypress scripts).
    /// </summary>
    private async Task<List<UserStatistic>> GetTopUsersByContentAsync()
    {
        try
        {
            var topUsers = await _context.Users
                .Include(u => u.Projects)
                    .ThenInclude(p => p.UserStories)
                        .ThenInclude(us => us.GherkinScenarios)
                .Include(u => u.Projects)
                    .ThenInclude(p => p.UserStories)
                        .ThenInclude(us => us.CypressScripts)
                .Select(u => new UserStatistic
                {
                    UserId = u.Id,
                    Email = u.Email ?? "",
                    ProjectCount = u.Projects.Count,
                    UserStoryCount = u.Projects.SelectMany(p => p.UserStories).Count(),
                    TestCount = u.Projects.SelectMany(p => p.UserStories)
                        .SelectMany(us => us.GherkinScenarios).Count() +
                        u.Projects.SelectMany(p => p.UserStories)
                        .SelectMany(us => us.CypressScripts).Count(),
                    LastActivity = u.Projects.Any()
                        ? u.Projects.Max(p => p.UpdatedAt ?? p.CreatedAt)
                        : DateTime.MinValue
                })
                .OrderByDescending(u => u.TestCount)
                .Take(5)
                .ToListAsync();

            return topUsers;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting top users by content");
            return new List<UserStatistic>();
        }
    }

    /// <summary>
    /// Gets the 10 most recent activities across projects, user stories, and Cypress scripts.
    /// </summary>
    private async Task<List<RecentActivity>> GetRecentActivitiesAsync()
    {
        try
        {
            // Fetch recent activities from different entities in parallel
            var recentProjectsTask = GetRecentProjectActivitiesAsync();
            var recentUserStoriesTask = GetRecentUserStoryActivitiesAsync();
            var recentCypressScriptsTask = GetRecentCypressScriptActivitiesAsync();

            await Task.WhenAll(recentProjectsTask, recentUserStoriesTask, recentCypressScriptsTask);

            // Combine and sort all activities
            var allActivities = recentProjectsTask.Result
                .Concat(recentUserStoriesTask.Result)
                .Concat(recentCypressScriptsTask.Result)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToList();

            return allActivities;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting recent activities");
            return new List<RecentActivity>();
        }
    }

    private async Task<List<RecentActivity>> GetRecentProjectActivitiesAsync()
    {
        return await _context.Projects
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentActivity
            {
                UserEmail = p.User!.Email ?? "",
                ActivityType = "Project Created",
                Description = $"Created project '{p.Name}'",
                Timestamp = p.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<List<RecentActivity>> GetRecentUserStoryActivitiesAsync()
    {
        return await _context.UserStories
            .Include(us => us.Project)
                .ThenInclude(p => p!.User)
            .OrderByDescending(us => us.CreatedAt)
            .Take(5)
            .Select(us => new RecentActivity
            {
                UserEmail = us.Project!.User!.Email ?? "",
                ActivityType = "User Story Created",
                Description = $"Created user story '{us.Title}'",
                Timestamp = us.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<List<RecentActivity>> GetRecentCypressScriptActivitiesAsync()
    {
        return await _context.CypressScripts
            .Include(cs => cs.UserStory)
                .ThenInclude(us => us!.Project)
                    .ThenInclude(p => p!.User)
            .OrderByDescending(cs => cs.CreatedAt)
            .Take(5)
            .Select(cs => new RecentActivity
            {
                UserEmail = cs.UserStory!.Project!.User!.Email ?? "",
                ActivityType = "Cypress Script Generated",
                Description = $"Generated script '{cs.FileName}'",
                Timestamp = cs.CreatedAt
            })
            .ToListAsync();
    }
}
