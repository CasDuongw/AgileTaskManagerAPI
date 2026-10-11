using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubtasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubtasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{taskId}")]
        public async Task<ActionResult<Subtask>> CreateSubtask(int taskId, [FromBody] Subtask request)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return NotFound("Task not found.");

            request.TaskId = taskId;
            request.ColumnId = task.ColumnId;
            request.ProjectId = task.ProjectId;
            request.IsActive = true;
            _context.Subtasks.Add(request);
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPut("{subtaskId}")]
        public async Task<IActionResult> UpdateSubtask(int subtaskId, [FromBody] Subtask request)
        {
            var subtask = await _context.Subtasks.FindAsync(subtaskId);
            if (subtask == null) return NotFound();

            subtask.Title = request.Title;
            subtask.IsCompleted = request.IsCompleted;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{subtaskId}")]
        public async Task<IActionResult> DeleteSubtask(int subtaskId)
        {
            var subtask = await _context.Subtasks.FindAsync(subtaskId);
            if (subtask == null) return NotFound();

            _context.Subtasks.Remove(subtask);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
