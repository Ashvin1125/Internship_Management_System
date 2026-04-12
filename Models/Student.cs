using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }
        
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string EnrollmentNumber { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Department { get; set; }
        
        public int Semester { get; set; }
    }
}
