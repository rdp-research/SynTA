using SynTA.Models.Domain;

namespace SynTA.Services.Database
{
    public interface ICypressScriptService
    {
        Task<IEnumerable<CypressScript>> GetScriptsByUserStoryIdAsync(int userStoryId);
        Task<CypressScript?> GetScriptByIdAsync(int scriptId);
        Task<CypressScript> CreateScriptAsync(CypressScript script);
        Task<CypressScript> UpdateScriptAsync(CypressScript script);
        Task<bool> DeleteScriptAsync(int scriptId);
        Task<int> DeleteAllByUserStoryIdAsync(int userStoryId);
    }
}
