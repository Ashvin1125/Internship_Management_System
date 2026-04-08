using System;

namespace InternshipManagementSystem.DTOs
{
    public class TaskDto
    {
        public int TaskId { get; set; }
        public int GuideId { get; set; }
        public int StudentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; }
    }
}
