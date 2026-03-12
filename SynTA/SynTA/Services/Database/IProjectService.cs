using SynTA.Models.Domain;

namespace SynTA.Services.Database
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsByUserIdAsync(string userId);
        Task<Project?> GetProjectByIdAsync(int projectId, string userId);
        Task<Project> CreateProjectAsync(Project project);
        Task<Project> UpdateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(int projectId, string userId);
        Task<bool> UserOwnsProjectAsync(int projectId, string userId);
    }
}
