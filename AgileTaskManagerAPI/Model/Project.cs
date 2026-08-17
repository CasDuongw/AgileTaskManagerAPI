using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgileTaskManagerAPI.Model
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- KHÓA NGOẠI ---
        public int OwnerId { get; set; }

        [ForeignKey("OwnerId")] // Báo cho EF Core biết OwnerId tham chiếu đến bảng User
        public User? Owner { get; set; }
    }
}
