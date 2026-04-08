using System;

namespace InternshipManagementSystem.DTOs
{
    public class DailyDiaryDto
    {
        public int DiaryId { get; set; }
        public int StudentId { get; set; }
        public DateTime WorkDate { get; set; }
        public string WorkDescription { get; set; }
        public string Status { get; set; }
        public string? GuideComment { get; set; }
    }
}
