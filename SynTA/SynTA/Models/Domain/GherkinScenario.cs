using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SynTA.Models.Domain
{
    public class GherkinScenario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Foreign key to UserStory
        [Required]
        public int UserStoryId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserStoryId))]
        public virtual UserStory? UserStory { get; set; }
    }
}
