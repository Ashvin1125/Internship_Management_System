using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class Document : AuditableEntity
    {
        [Key]
        public int DocumentId { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }
        
        [Required]
        public string FilePath { get; set; }
        
        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
}
