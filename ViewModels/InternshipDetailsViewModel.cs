using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.ViewModels
{
    public class InternshipDetailsViewModel
    {
        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "Role / Position")]
        public string Role { get; set; }

        [Display(Name = "Technology Used")]
        public string? TechnologyUsed { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public string? Description { get; set; }
    }
}
