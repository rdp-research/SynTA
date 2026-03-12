using System.ComponentModel.DataAnnotations;

namespace SynTA.Areas.Admin.Models
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string? UserName { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Lockout Enabled")]
        public bool LockoutEnabled { get; set; }

        [Display(Name = "Lockout End")]
        public DateTimeOffset? LockoutEnd { get; set; }

        [Display(Name = "Is Admin")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Projects")]
        public int ProjectCount { get; set; }

        [Display(Name = "User Stories")]
        public int UserStoryCount { get; set; }

        [Display(Name = "Gherkin Scenarios")]
        public int GherkinScenarioCount { get; set; }

        [Display(Name = "Cypress Scripts")]
        public int CypressScriptCount { get; set; }

        [Display(Name = "Joined")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Last Activity")]
        public DateTime? LastActivity { get; set; }
    }

    public class UserDetailViewModel : UserViewModel
    {
        public List<ProjectSummary> Projects { get; set; } = new();
    }

    public class ProjectSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UserStoryCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
