using SynTA.Areas.Admin.Models;

namespace SynTA.Services.Analytics;

/// <summary>
/// Interface for dashboard and analytics services.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets comprehensive dashboard statistics including user counts, content counts, and activity metrics.
    /// </summary>
    /// <returns>Dashboard view model with all statistics</returns>
    Task<DashboardViewModel> GetDashboardStatisticsAsync();
}
