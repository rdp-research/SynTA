using Microsoft.AspNetCore.Identity;

namespace SynTA.Models.Domain
{
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Date and time when the user account was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
