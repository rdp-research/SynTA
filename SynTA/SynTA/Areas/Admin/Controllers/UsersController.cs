using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynTA.Areas.Admin.Models;
using SynTA.Data;
using SynTA.Models.Domain;

namespace SynTA.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userViewModels = new List<UserViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var projects = await _context.Projects
                        .Where(p => p.UserId == user.Id)
                        .Include(p => p.UserStories)
                            .ThenInclude(us => us.GherkinScenarios)
                        .Include(p => p.UserStories)
                            .ThenInclude(us => us.CypressScripts)
                        .ToListAsync();

                    var lastActivity = projects.Any()
                        ? projects.Max(p => p.UpdatedAt ?? p.CreatedAt)
                        : (DateTime?)null;

                    userViewModels.Add(new UserViewModel
                    {
                        Id = user.Id,
                        Email = user.Email ?? "",
                        UserName = user.UserName,
                        EmailConfirmed = user.EmailConfirmed,
                        LockoutEnabled = user.LockoutEnabled,
                        LockoutEnd = user.LockoutEnd,
                        IsAdmin = roles.Contains("Admin"),
                        ProjectCount = projects.Count,
                        UserStoryCount = projects.SelectMany(p => p.UserStories).Count(),
                        GherkinScenarioCount = projects.SelectMany(p => p.UserStories)
                            .SelectMany(us => us.GherkinScenarios).Count(),
                        CypressScriptCount = projects.SelectMany(p => p.UserStories)
                            .SelectMany(us => us.CypressScripts).Count(),
                        LastActivity = lastActivity
                    });
                }

                return View(userViewModels.OrderByDescending(u => u.LastActivity).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                TempData["ErrorMessage"] = "An error occurred while loading users.";
                return View(new List<UserViewModel>());
            }
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var roles = await _userManager.GetRolesAsync(user);
                var projects = await _context.Projects
                    .Where(p => p.UserId == user.Id)
                    .Include(p => p.UserStories)
                        .ThenInclude(us => us.GherkinScenarios)
                    .Include(p => p.UserStories)
                        .ThenInclude(us => us.CypressScripts)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var lastActivity = projects.Any()
                    ? projects.Max(p => p.UpdatedAt ?? p.CreatedAt)
                    : (DateTime?)null;

                var viewModel = new UserDetailViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    IsAdmin = roles.Contains("Admin"),
                    ProjectCount = projects.Count,
                    UserStoryCount = projects.SelectMany(p => p.UserStories).Count(),
                    GherkinScenarioCount = projects.SelectMany(p => p.UserStories)
                        .SelectMany(us => us.GherkinScenarios).Count(),
                    CypressScriptCount = projects.SelectMany(p => p.UserStories)
                        .SelectMany(us => us.CypressScripts).Count(),
                    LastActivity = lastActivity,
                    Projects = projects.Select(p => new ProjectSummary
                    {
                        Id = p.Id,
                        Name = p.Name,
                        UserStoryCount = p.UserStories.Count,
                        CreatedAt = p.CreatedAt
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Users/ToggleLockout/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Don't allow locking out yourself
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == user.Id)
                {
                    TempData["ErrorMessage"] = "You cannot lock out your own account.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Toggle lockout
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    // Currently locked out - unlock
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    TempData["SuccessMessage"] = $"User {user.Email} has been unlocked.";
                }
                else
                {
                    // Not locked out - lock for 100 years
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                    TempData["SuccessMessage"] = $"User {user.Email} has been locked out.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling lockout for user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the user lockout status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Users/ToggleAdmin/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Don't allow removing admin from yourself
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == user.Id)
                {
                    TempData["ErrorMessage"] = "You cannot remove admin role from your own account.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    TempData["SuccessMessage"] = $"Admin role removed from {user.Email}.";
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    TempData["SuccessMessage"] = $"Admin role granted to {user.Email}.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling admin role for user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the user role.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Don't allow deleting yourself
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == user.Id)
                {
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin deleted user {Email}", user.Email);
                    TempData["SuccessMessage"] = $"User {user.Email} has been deleted.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error deleting user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
