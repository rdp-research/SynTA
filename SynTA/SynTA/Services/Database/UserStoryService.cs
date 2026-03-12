using Microsoft.EntityFrameworkCore;
using SynTA.Data;
using SynTA.Models.Domain;
using Microsoft.Extensions.Logging;

namespace SynTA.Services.Database
{
    public class UserStoryService : IUserStoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserStoryService> _logger;

        public UserStoryService(ApplicationDbContext context, ILogger<UserStoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<UserStory>> GetAllUserStoriesByProjectIdAsync(int projectId)
        {
            try
            {
                return await _context.UserStories
                    .Where(us => us.ProjectId == projectId)
                    .Include(us => us.GherkinScenarios)
                    .Include(us => us.CypressScripts)
                    .OrderByDescending(us => us.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user stories for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<UserStory?> GetUserStoryByIdAsync(int userStoryId)
        {
            try
            {
                return await _context.UserStories
                    .Include(us => us.Project)
                    .Include(us => us.GherkinScenarios)
                    .Include(us => us.CypressScripts)
                    .FirstOrDefaultAsync(us => us.Id == userStoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user story {UserStoryId}", userStoryId);
                throw;
            }
        }

        public async Task<UserStory> CreateUserStoryAsync(UserStory userStory)
        {
            try
            {
                userStory.CreatedAt = DateTime.UtcNow;
                _context.UserStories.Add(userStory);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created user story {UserStoryId} for project {ProjectId}", 
                    userStory.Id, userStory.ProjectId);
                return userStory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user story for project {ProjectId}", userStory.ProjectId);
                throw;
            }
        }

        public async Task<UserStory> UpdateUserStoryAsync(UserStory userStory)
        {
            try
            {
                userStory.UpdatedAt = DateTime.UtcNow;
                _context.UserStories.Update(userStory);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated user story {UserStoryId}", userStory.Id);
                return userStory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user story {UserStoryId}", userStory.Id);
                throw;
            }
        }

        public async Task<bool> DeleteUserStoryAsync(int userStoryId)
        {
            try
            {
                var userStory = await _context.UserStories.FindAsync(userStoryId);
                
                if (userStory == null)
                    return false;

                _context.UserStories.Remove(userStory);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted user story {UserStoryId}", userStoryId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user story {UserStoryId}", userStoryId);
                throw;
            }
        }

        public async Task<bool> UserStoryExistsAsync(int userStoryId)
        {
            return await _context.UserStories.AnyAsync(us => us.Id == userStoryId);
        }
    }
}
