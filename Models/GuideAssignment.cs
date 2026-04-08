using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class GuideAssignment : AuditableEntity
    {
        [Key]
        public int AssignmentId { get; set; }
        
        public int GuideId { get; set; }
        [ForeignKey("GuideId")]
        public Guide Guide { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
    }
}
