using System.ComponentModel.DataAnnotations;

namespace SynTA.Areas.User.Models
{
    public class UserStoryCreateViewModel
    {
        [Required]
        public int ProjectId { get; set; }

        public string? ProjectName { get; set; }

        [Required]
        [StringLength(300, ErrorMessage = "Title cannot exceed 300 characters.")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "User Story")]
        public string UserStoryText { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Acceptance Criteria")]
        public string? AcceptanceCriteria { get; set; }
    }
}
