using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AgileTaskManagerAPI.Model
{
    public class Subtask
    {
        [Key]
        public int SubtaskId { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        
        public bool IsCompleted { get; set; }

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? DueDate { get; set; }

        public string Tags { get; set; } = string.Empty;
        
        public int? ColumnId { get; set; }
        
        [ForeignKey("ColumnId")]
        [JsonIgnore]
        public KanbanColumn? Column { get; set; }

        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [JsonIgnore]
        public Project? Project { get; set; }

        public int? AssigneeId { get; set; }

        [ForeignKey("AssigneeId")]
        public User? Assignee { get; set; }

        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        [JsonIgnore]
        public AppTask? Task { get; set; }
    }
}
