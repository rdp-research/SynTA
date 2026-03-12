using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynTA.Services.Analytics;

namespace SynTA.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = await _dashboardService.GetDashboardStatisticsAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return View(new SynTA.Areas.Admin.Models.DashboardViewModel());
            }
        }
    }
}
