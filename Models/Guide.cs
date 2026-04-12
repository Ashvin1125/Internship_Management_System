using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class Guide
    {
        [Key]
        public int GuideId { get; set; }
        
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Department { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Designation { get; set; }
    }
}
