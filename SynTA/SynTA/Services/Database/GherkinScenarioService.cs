using Microsoft.EntityFrameworkCore;
using SynTA.Data;
using SynTA.Models.Domain;

namespace SynTA.Services.Database
{
    public class GherkinScenarioService : IGherkinScenarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GherkinScenarioService> _logger;

        public GherkinScenarioService(ApplicationDbContext context, ILogger<GherkinScenarioService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<GherkinScenario>> GetScenariosByUserStoryIdAsync(int userStoryId)
        {
            try
            {
                return await _context.GherkinScenarios
                    .Where(gs => gs.UserStoryId == userStoryId)
                    .OrderByDescending(gs => gs.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Gherkin scenarios for user story {UserStoryId}", userStoryId);
                throw;
            }
        }

        public async Task<GherkinScenario?> GetScenarioByIdAsync(int scenarioId)
        {
            try
            {
                return await _context.GherkinScenarios
                    .Include(gs => gs.UserStory)
                    .ThenInclude(us => us!.Project)
                    .FirstOrDefaultAsync(gs => gs.Id == scenarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Gherkin scenario {ScenarioId}", scenarioId);
                throw;
            }
        }

        public async Task<GherkinScenario> CreateScenarioAsync(GherkinScenario scenario)
        {
            try
            {
                scenario.CreatedAt = DateTime.UtcNow;
                _context.GherkinScenarios.Add(scenario);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created Gherkin scenario {ScenarioId} for user story {UserStoryId}",
                    scenario.Id, scenario.UserStoryId);
                return scenario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Gherkin scenario for user story {UserStoryId}", scenario.UserStoryId);
                throw;
            }
        }

        public async Task<GherkinScenario> UpdateScenarioAsync(GherkinScenario scenario)
        {
            try
            {
                scenario.UpdatedAt = DateTime.UtcNow;
                _context.GherkinScenarios.Update(scenario);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated Gherkin scenario {ScenarioId}", scenario.Id);
                return scenario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Gherkin scenario {ScenarioId}", scenario.Id);
                throw;
            }
        }

        public async Task<bool> DeleteScenarioAsync(int scenarioId)
        {
            try
            {
                var scenario = await _context.GherkinScenarios.FindAsync(scenarioId);
                if (scenario == null)
                    return false;

                _context.GherkinScenarios.Remove(scenario);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted Gherkin scenario {ScenarioId}", scenarioId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Gherkin scenario {ScenarioId}", scenarioId);
                throw;
            }
        }

        public async Task<int> DeleteAllByUserStoryIdAsync(int userStoryId)
        {
            try
            {
                var scenarios = await _context.GherkinScenarios
                    .Where(gs => gs.UserStoryId == userStoryId)
                    .ToListAsync();

                if (!scenarios.Any())
                    return 0;

                var count = scenarios.Count;
                _context.GherkinScenarios.RemoveRange(scenarios);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} Gherkin scenarios for user story {UserStoryId}", count, userStoryId);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Gherkin scenarios for user story {UserStoryId}", userStoryId);
                throw;
            }
        }
    }
}
