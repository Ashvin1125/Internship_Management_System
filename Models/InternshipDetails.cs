using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class InternshipDetails : AuditableEntity
    {
        [Key]
        public int InternshipId { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Role { get; set; }
        
        [MaxLength(200)]
        public string TechnologyUsed { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public string? Description { get; set; }
    }
}
