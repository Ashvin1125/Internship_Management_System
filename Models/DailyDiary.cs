using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class DailyDiary
    {
        [Key]
        public int DiaryId { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        public DateTime WorkDate { get; set; }
        
        [Required]
        public string WorkDescription { get; set; }
        
        // Pending, Approved, Rejected
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";
        
        public string? GuideComment { get; set; }
    }
}
