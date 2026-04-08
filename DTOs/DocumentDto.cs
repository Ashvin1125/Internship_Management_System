using System;

namespace InternshipManagementSystem.DTOs
{
    public class DocumentDto
    {
        public int DocumentId { get; set; }
        public int StudentId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
