using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class InternshipTask : BaseEntity
    {
        [Key]
        public int TaskId { get; set; }
        
        public int GuideId { get; set; }
        [ForeignKey("GuideId")]
        public Guide Guide { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }
        
        public string? Description { get; set; }
        
        public DateTime Deadline { get; set; }
        
        [MaxLength(50)]
        // Assigned, InProgress, Completed
        public string Status { get; set; } = "Assigned";
    }
}
