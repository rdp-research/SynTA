using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SynTA.Models.Domain
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Foreign key to ApplicationUser
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<UserStory> UserStories { get; set; } = new List<UserStory>();
    }
}
