using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class Feedback : BaseEntity
    {
        [Key]
        public int FeedbackId { get; set; }
        
        public int GuideId { get; set; }
        [ForeignKey("GuideId")]
        public Guide Guide { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        public int Rating { get; set; } // e.g., 1 to 5
        
        public string? Comments { get; set; }
    }
}
