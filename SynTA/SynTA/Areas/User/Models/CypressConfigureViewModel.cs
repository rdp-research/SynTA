using System.ComponentModel.DataAnnotations;

namespace SynTA.Areas.User.Models
{
    public class CypressConfigureViewModel
    {
        public int GherkinScenarioId { get; set; }

        public string GherkinTitle { get; set; } = string.Empty;

        public string GherkinContent { get; set; } = string.Empty;

        public int UserStoryId { get; set; }

        public string UserStoryTitle { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        [Required]
        [Url]
        [Display(Name = "Target URL")]
        public string TargetUrl { get; set; } = string.Empty;

        [Display(Name = "Fetch HTML Context")]
        public bool FetchHtmlContext { get; set; } = true;
    }
}
