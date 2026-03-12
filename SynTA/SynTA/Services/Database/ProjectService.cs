using Microsoft.EntityFrameworkCore;
using SynTA.Data;
using SynTA.Models.Domain;

namespace SynTA.Services.Database
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Project>> GetAllProjectsByUserIdAsync(string userId)
        {
            try
            {
                _logger.LogDebug("Retrieving all projects for user {UserId}", userId);
                var projects = await _context.Projects
                    .Where(p => p.UserId == userId)
                    .Include(p => p.UserStories)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
                _logger.LogDebug("Retrieved {Count} projects for user {UserId}", projects.Count, userId);
                return projects;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for user {UserId} - Error: {ErrorMessage}", userId, ex.Message);
                throw;
            }
        }

        public async Task<Project?> GetProjectByIdAsync(int projectId, string userId)
        {
            try
            {
                _logger.LogDebug("Retrieving project {ProjectId} for user {UserId}", projectId, userId);
                return await _context.Projects
                    .Include(p => p.UserStories)
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project {ProjectId} for user {UserId} - Error: {ErrorMessage}", projectId, userId, ex.Message);
                throw;
            }
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            try
            {
                project.CreatedAt = DateTime.UtcNow;
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created project - ProjectId: {ProjectId}, Name: '{ProjectName}', UserId: {UserId}", 
                    project.Id, project.Name, project.UserId);
                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project for user {UserId} - ProjectName: '{ProjectName}', Error: {ErrorMessage}", 
                    project.UserId, project.Name, ex.Message);
                throw;
            }
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            try
            {
                project.UpdatedAt = DateTime.UtcNow;
                _context.Projects.Update(project);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated project - ProjectId: {ProjectId}, Name: '{ProjectName}'", project.Id, project.Name);
                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId} - Error: {ErrorMessage}", project.Id, ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteProjectAsync(int projectId, string userId)
        {
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

                if (project == null)
                {
                    _logger.LogWarning("Delete project failed - Project not found or access denied - ProjectId: {ProjectId}, UserId: {UserId}", 
                        projectId, userId);
                    return false;
                }

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted project - ProjectId: {ProjectId}, Name: '{ProjectName}', UserId: {UserId}", 
                    projectId, project.Name, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId} - Error: {ErrorMessage}", projectId, ex.Message);
                throw;
            }
        }

        public async Task<bool> UserOwnsProjectAsync(int projectId, string userId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.UserId == userId);
        }
    }
}
