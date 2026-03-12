namespace SynTA.Areas.User.Models
{
    public class GherkinGenerateViewModel
    {
        public int UserStoryId { get; set; }
        public string UserStoryTitle { get; set; } = string.Empty;
        public string UserStoryText { get; set; } = string.Empty;
        public string? UserStoryDescription { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public bool IsGenerating { get; set; }
    }
}
