using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class Notification : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty; // e.g., "Task", "Diary", "Chat"

        public bool IsRead { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
