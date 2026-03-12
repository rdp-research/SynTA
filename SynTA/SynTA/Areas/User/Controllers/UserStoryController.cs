using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SynTA.Areas.User.Models;
using SynTA.Models.Domain;
using SynTA.Services.Database;
using SynTA.Services.Workflows;

namespace SynTA.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class UserStoryController : Controller
{
    private readonly IUserStoryService _userStoryService;
    private readonly IProjectService _projectService;
    private readonly ITestGenerationWorkflowService _testGenerationWorkflow;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserStoryController> _logger;

    public UserStoryController(
        IUserStoryService userStoryService,
        IProjectService projectService,
        ITestGenerationWorkflowService testGenerationWorkflow,
        UserManager<ApplicationUser> userManager,
        ILogger<UserStoryController> logger)
    {
        _userStoryService = userStoryService;
        _projectService = projectService;
        _testGenerationWorkflow = testGenerationWorkflow;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: User/UserStory?projectId=5
    public async Task<IActionResult> Index(int projectId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns this project
        var project = await _projectService.GetProjectByIdAsync(projectId, userId);
        if (project == null)
        {
            return NotFound();
        }

        ViewBag.ProjectId = projectId;
        ViewBag.ProjectName = project.Name;

        var userStories = await _userStoryService.GetAllUserStoriesByProjectIdAsync(projectId);

        var viewModels = userStories.Select(us => new UserStoryDetailViewModel
        {
            Id = us.Id,
            Title = us.Title,
            UserStoryText = us.UserStoryText,
            Description = us.Description,
            AcceptanceCriteria = us.AcceptanceCriteria,
            CreatedAt = us.CreatedAt,
            UpdatedAt = us.UpdatedAt,
            ProjectId = us.ProjectId,
            ProjectName = project.Name,
            GherkinScenarioCount = us.GherkinScenarios.Count,
            CypressScriptCount = us.CypressScripts.Count
        }).ToList();

        return View(viewModels);
    }

    // GET: User/UserStory/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userStory = await _userStoryService.GetUserStoryByIdAsync(id);
        if (userStory == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId))
        {
            return Forbid();
        }

        var viewModel = new UserStoryDetailViewModel
        {
            Id = userStory.Id,
            Title = userStory.Title,
            UserStoryText = userStory.UserStoryText,
            Description = userStory.Description,
            AcceptanceCriteria = userStory.AcceptanceCriteria,
            CreatedAt = userStory.CreatedAt,
            UpdatedAt = userStory.UpdatedAt,
            ProjectId = userStory.ProjectId,
            ProjectName = userStory.Project?.Name ?? "",
            GherkinScenarioCount = userStory.GherkinScenarios.Count,
            CypressScriptCount = userStory.CypressScripts.Count
        };

        return View(viewModel);
    }

    // GET: User/UserStory/Create?projectId=5
    public async Task<IActionResult> Create(int projectId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        var project = await _projectService.GetProjectByIdAsync(projectId, userId);
        if (project == null)
        {
            return NotFound();
        }

        var viewModel = new UserStoryCreateViewModel
        {
            ProjectId = projectId,
            ProjectName = project.Name
        };

        return View(viewModel);
    }

    // POST: User/UserStory/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserStoryCreateViewModel model)
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

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(model.ProjectId, userId))
        {
            return Forbid();
        }

        try
        {
            var userStory = new UserStory
            {
                Title = model.Title,
                UserStoryText = model.UserStoryText,
                Description = model.Description,
                AcceptanceCriteria = model.AcceptanceCriteria,
                ProjectId = model.ProjectId
            };

            await _userStoryService.CreateUserStoryAsync(userStory);
            TempData["SuccessMessage"] = "User story created successfully!";
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user story");
            ModelState.AddModelError("", "An error occurred while creating the user story. Please try again.");
            return View(model);
        }
    }

    // GET: User/UserStory/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var userStory = await _userStoryService.GetUserStoryByIdAsync(id);
        if (userStory == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId))
        {
            return Forbid();
        }

        var viewModel = new UserStoryCreateViewModel
        {
            ProjectId = userStory.ProjectId,
            ProjectName = userStory.Project?.Name,
            Title = userStory.Title,
            UserStoryText = userStory.UserStoryText,
            Description = userStory.Description,
            AcceptanceCriteria = userStory.AcceptanceCriteria
        };

        ViewBag.UserStoryId = id;
        return View(viewModel);
    }

    // POST: User/UserStory/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserStoryCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UserStoryId = id;
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(model.ProjectId, userId))
        {
            return Forbid();
        }

        try
        {
            var userStory = await _userStoryService.GetUserStoryByIdAsync(id);
            if (userStory == null)
            {
                return NotFound();
            }

            userStory.Title = model.Title;
            userStory.UserStoryText = model.UserStoryText;
            userStory.Description = model.Description;
            userStory.AcceptanceCriteria = model.AcceptanceCriteria;

            await _userStoryService.UpdateUserStoryAsync(userStory);
            TempData["SuccessMessage"] = "User story updated successfully!";
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user story {UserStoryId}", id);
            ModelState.AddModelError("", "An error occurred while updating the user story. Please try again.");
            ViewBag.UserStoryId = id;
            return View(model);
        }
    }

    // GET: User/UserStory/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var userStory = await _userStoryService.GetUserStoryByIdAsync(id);
        if (userStory == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId))
        {
            return Forbid();
        }

        var viewModel = new UserStoryDetailViewModel
        {
            Id = userStory.Id,
            Title = userStory.Title,
            Description = userStory.Description,
            AcceptanceCriteria = userStory.AcceptanceCriteria,
            CreatedAt = userStory.CreatedAt,
            UpdatedAt = userStory.UpdatedAt,
            ProjectId = userStory.ProjectId,
            ProjectName = userStory.Project?.Name ?? "",
            GherkinScenarioCount = userStory.GherkinScenarios.Count,
            CypressScriptCount = userStory.CypressScripts.Count
        };

        return View(viewModel);
    }

    // POST: User/UserStory/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userStory = await _userStoryService.GetUserStoryByIdAsync(id);
        if (userStory == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        // Verify user owns the project
        if (!await _projectService.UserOwnsProjectAsync(userStory.ProjectId, userId))
        {
            return Forbid();
        }

        var projectId = userStory.ProjectId;

        try
        {
            await _userStoryService.DeleteUserStoryAsync(id);
            TempData["SuccessMessage"] = "User story deleted successfully!";
            return RedirectToAction(nameof(Index), new { projectId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user story {UserStoryId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the user story. Please try again.";
            return RedirectToAction(nameof(Index), new { projectId });
        }
    }

    // POST: User/UserStory/GenerateAll/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAll(int id, string? targetUrl)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account", new { area = "" });

        try
        {
            // Delegate the entire workflow to the service
            var result = await _testGenerationWorkflow.GenerateCompleteTestSuiteAsync(id, userId, targetUrl);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Successfully generated Gherkin scenarios and Cypress scripts!";
                return RedirectToAction("ReviewAndExport", "Cypress", new { area = "User", id = result.CypressScriptId });
            }
            else
            {
                _logger.LogWarning("Test generation failed for UserStoryId: {UserStoryId} - {Error}", id, result.ErrorMessage);
                TempData["ErrorMessage"] = result.ErrorMessage ?? "An error occurred during generation. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GenerateAll controller action for UserStoryId: {UserStoryId}", id);
            TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
