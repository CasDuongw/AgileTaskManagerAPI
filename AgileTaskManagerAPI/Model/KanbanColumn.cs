using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AgileTaskManagerAPI.Model
{
    public class KanbanColumn
    {
        [Key]
        public int ColumnId { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;
        
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "Vừa giao"; 

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [JsonIgnore]
        public ICollection<AppTask> Tasks { get; set; } = new List<AppTask>();
    }
}



