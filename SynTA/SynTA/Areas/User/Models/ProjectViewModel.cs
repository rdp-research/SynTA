using System.ComponentModel.DataAnnotations;

namespace SynTA.Areas.User.Models
{
    public class ProjectViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Project name cannot exceed 200 characters.")]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int UserStoryCount { get; set; }
    }
}
