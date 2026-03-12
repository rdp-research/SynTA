using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SynTA.Models.Domain
{
    public class UserStory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Short title used for display, file naming, and organization.
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The actual user story text (e.g., "As a user, I want to...").
        /// This is passed to the AI for generation.
        /// </summary>
        [Required]
        public string UserStoryText { get; set; } = string.Empty;

        /// <summary>
        /// Additional description or context for the user story.
        /// </summary>
        public string? Description { get; set; }

        public string? AcceptanceCriteria { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Foreign key to Project
        [Required]
        public int ProjectId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        public virtual ICollection<GherkinScenario> GherkinScenarios { get; set; } = new List<GherkinScenario>();

        public virtual ICollection<CypressScript> CypressScripts { get; set; } = new List<CypressScript>();
    }
}
