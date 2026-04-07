using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.ViewModels
{
    public class CreateGuideViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }

        [Required]
        [MaxLength(100)]
        public string Department { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Designation { get; set; }
    }
}
