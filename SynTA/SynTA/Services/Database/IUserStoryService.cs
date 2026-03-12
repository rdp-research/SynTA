using SynTA.Models.Domain;

namespace SynTA.Services.Database
{
    public interface IUserStoryService
    {
        Task<IEnumerable<UserStory>> GetAllUserStoriesByProjectIdAsync(int projectId);
        Task<UserStory?> GetUserStoryByIdAsync(int userStoryId);
        Task<UserStory> CreateUserStoryAsync(UserStory userStory);
        Task<UserStory> UpdateUserStoryAsync(UserStory userStory);
        Task<bool> DeleteUserStoryAsync(int userStoryId);
        Task<bool> UserStoryExistsAsync(int userStoryId);
    }
}
