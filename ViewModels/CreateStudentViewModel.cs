using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.ViewModels
{
    public class CreateStudentViewModel
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
        [Display(Name = "Enrollment Number")]
        [MaxLength(50)]
        public string EnrollmentNumber { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Department { get; set; }
        
        [Required]
        public int Semester { get; set; }
    }
}
