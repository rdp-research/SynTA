namespace SynTA.Areas.User.Models
{
    public class UserStoryDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string UserStoryText { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int GherkinScenarioCount { get; set; }
        public int CypressScriptCount { get; set; }
    }
}
