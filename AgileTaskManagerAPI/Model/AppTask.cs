using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgileTaskManagerAPI.Model
{
    public class AppTask
    {
        [Key]
        public int TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "ToDo";

        // --- KHÓA NGOẠI 1: Liên kết với Project ---
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        // --- KHÓA NGOẠI 2: Liên kết với User (Người được giao) ---
        public int? AssigneeId { get; set; } // Có thể null (chưa giao ai)

        [ForeignKey("AssigneeId")]
        public User? Assignee { get; set; }
    }
}
