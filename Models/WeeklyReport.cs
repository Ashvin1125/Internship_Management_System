using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class WeeklyReport : AuditableEntity
    {
        [Key]
        public int ReportId { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        public int WeekNumber { get; set; }
        
        [Required]
        public string Summary { get; set; }
        
        [MaxLength(50)]
        // Pending, Approved, Rejected
        public string Status { get; set; } = "Pending";
        
        public string? GuideComment { get; set; }
    }
}
