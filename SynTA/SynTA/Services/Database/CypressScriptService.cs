using Microsoft.EntityFrameworkCore;
using SynTA.Data;
using SynTA.Models.Domain;

namespace SynTA.Services.Database
{
    public class CypressScriptService : ICypressScriptService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CypressScriptService> _logger;

        public CypressScriptService(ApplicationDbContext context, ILogger<CypressScriptService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CypressScript>> GetScriptsByUserStoryIdAsync(int userStoryId)
        {
            try
            {
                return await _context.CypressScripts
                    .Where(cs => cs.UserStoryId == userStoryId)
                    .OrderByDescending(cs => cs.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Cypress scripts for user story {UserStoryId}", userStoryId);
                throw;
            }
        }

        public async Task<CypressScript?> GetScriptByIdAsync(int scriptId)
        {
            try
            {
                return await _context.CypressScripts
                    .Include(cs => cs.UserStory)
                    .ThenInclude(us => us!.Project)
                    .FirstOrDefaultAsync(cs => cs.Id == scriptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Cypress script {ScriptId}", scriptId);
                throw;
            }
        }

        public async Task<CypressScript> CreateScriptAsync(CypressScript script)
        {
            try
            {
                script.CreatedAt = DateTime.UtcNow;
                _context.CypressScripts.Add(script);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created Cypress script {ScriptId} for user story {UserStoryId}",
                    script.Id, script.UserStoryId);
                return script;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Cypress script for user story {UserStoryId}", script.UserStoryId);
                throw;
            }
        }

        public async Task<CypressScript> UpdateScriptAsync(CypressScript script)
        {
            try
            {
                script.UpdatedAt = DateTime.UtcNow;
                _context.CypressScripts.Update(script);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated Cypress script {ScriptId}", script.Id);
                return script;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Cypress script {ScriptId}", script.Id);
                throw;
            }
        }

        public async Task<bool> DeleteScriptAsync(int scriptId)
        {
            try
            {
                var script = await _context.CypressScripts.FindAsync(scriptId);
                if (script == null)
                    return false;

                _context.CypressScripts.Remove(script);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted Cypress script {ScriptId}", scriptId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Cypress script {ScriptId}", scriptId);
                throw;
            }
        }

        public async Task<int> DeleteAllByUserStoryIdAsync(int userStoryId)
        {
            try
            {
                var scripts = await _context.CypressScripts
                    .Where(cs => cs.UserStoryId == userStoryId)
                    .ToListAsync();

                if (!scripts.Any())
                    return 0;

                var count = scripts.Count;
                _context.CypressScripts.RemoveRange(scripts);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} Cypress scripts for user story {UserStoryId}", count, userStoryId);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Cypress scripts for user story {UserStoryId}", userStoryId);
                throw;
            }
        }
    }
}
