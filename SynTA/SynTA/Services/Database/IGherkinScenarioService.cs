using SynTA.Models.Domain;

namespace SynTA.Services.Database
{
    public interface IGherkinScenarioService
    {
        Task<IEnumerable<GherkinScenario>> GetScenariosByUserStoryIdAsync(int userStoryId);
        Task<GherkinScenario?> GetScenarioByIdAsync(int scenarioId);
        Task<GherkinScenario> CreateScenarioAsync(GherkinScenario scenario);
        Task<GherkinScenario> UpdateScenarioAsync(GherkinScenario scenario);
        Task<bool> DeleteScenarioAsync(int scenarioId);
        Task<int> DeleteAllByUserStoryIdAsync(int userStoryId);
    }
}
