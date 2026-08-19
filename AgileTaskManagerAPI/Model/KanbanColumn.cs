using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgileTaskManagerAPI.Model
{
    [Table("KanbanColumns")]
    public class KanbanColumn
    {
        [Key]
        public int ColumnId { get; set; }

        [Required]
        public string ColumnName { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        // Khóa ngoại liên kết với Project
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }
    }
}
