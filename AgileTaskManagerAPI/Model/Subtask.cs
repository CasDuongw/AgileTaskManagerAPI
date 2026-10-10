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
        public string Title { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public int TaskId { get; set; }
        
        [ForeignKey("TaskId")]
        [JsonIgnore]
        public AppTask Task { get; set; }
    }
}
