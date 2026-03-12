using System.ComponentModel.DataAnnotations;

namespace SynTA.Areas.User.Models
{
    public class CypressReviewViewModel
    {
        public int Id { get; set; }

        public int UserStoryId { get; set; }

        public string UserStoryTitle { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public int? GherkinScenarioId { get; set; }

        public string GherkinContent { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "TypeScript Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Target URL")]
        public string? TargetUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
