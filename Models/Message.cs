using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementSystem.Models
{
    public class Message : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        [Required]
        public string Sid { get; set; } = string.Empty;

        // Soft delete flags
        public bool IsDeletedBySender { get; set; }
        public bool IsDeletedByReceiver { get; set; }
        public bool IsDeletedForEveryone { get; set; }

        [ForeignKey("SenderId")]
        public virtual User? Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual User? Receiver { get; set; }
    }
}
