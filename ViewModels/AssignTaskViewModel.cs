using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.ViewModels
{
    public class AssignTaskViewModel
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime Deadline { get; set; }
    }
}
