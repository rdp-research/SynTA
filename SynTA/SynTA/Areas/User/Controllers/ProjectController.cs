using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynTA.Areas.User.Models;
using SynTA.Models.Domain;
using SynTA.Services.Database;

namespace SynTA.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProjectController> _logger;

        public ProjectController(
            IProjectService projectService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProjectController> logger)
        {
            _projectService = projectService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: User/Project
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var projects = await _projectService.GetAllProjectsByUserIdAsync(userId);
            
            var viewModels = projects.Select(p => new ProjectViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UserStoryCount = p.UserStories.Count
            }).ToList();

            return View(viewModels);
        }

        // GET: User/Project/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var project = new Project
                {
                    Name = model.Name,
                    Description = model.Description,
                    UserId = userId
                };

                await _projectService.CreateProjectAsync(project);
                TempData["SuccessMessage"] = "Project created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                ModelState.AddModelError("", "An error occurred while creating the project. Please try again.");
                return View(model);
            }
        }

        // GET: User/Project/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var project = await _projectService.GetProjectByIdAsync(id, userId);
            if (project == null)
            {
                return NotFound();
            }

            var viewModel = new ProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            };

            return View(viewModel);
        }

        // POST: User/Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var project = await _projectService.GetProjectByIdAsync(id, userId);
                if (project == null)
                {
                    return NotFound();
                }

                project.Name = model.Name;
                project.Description = model.Description;

                await _projectService.UpdateProjectAsync(project);
                TempData["SuccessMessage"] = "Project updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", id);
                ModelState.AddModelError("", "An error occurred while updating the project. Please try again.");
                return View(model);
            }
        }

        // GET: User/Project/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var project = await _projectService.GetProjectByIdAsync(id, userId);
            if (project == null)
            {
                return NotFound();
            }

            var viewModel = new ProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                UserStoryCount = project.UserStories.Count
            };

            return View(viewModel);
        }

        // POST: User/Project/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var result = await _projectService.DeleteProjectAsync(id, userId);
                if (!result)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] = "Project deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the project. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
