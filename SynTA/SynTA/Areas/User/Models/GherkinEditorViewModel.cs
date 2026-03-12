using System.ComponentModel.DataAnnotations;

namespace SynTA.Areas.User.Models
{
    public class GherkinEditorViewModel
    {
        public int Id { get; set; }

        public int UserStoryId { get; set; }

        public string UserStoryTitle { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Scenario Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Gherkin Content")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
