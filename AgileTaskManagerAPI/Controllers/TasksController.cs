using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdStr, out int userId) ? userId : 0;
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<AppTask>>> GetTasksByProject(int projectId)
        {
            int currentUserId = GetCurrentUserId();
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.OwnerId != currentUserId) return Forbid();

            return await _context.Tasks.Where(t => t.ProjectId == projectId && t.IsActive).OrderBy(t => t.OrderIndex).ToListAsync();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppTask>>> GetAllTasks()
        {
            return await _context.Tasks.Where(t => t.IsActive).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<AppTask>> CreateTask(AppTask task)
        {
            int currentUserId = GetCurrentUserId();
            var project = await _context.Projects.FindAsync(task.ProjectId);
            if (project == null || project.OwnerId != currentUserId) return Forbid();

            task.IsActive = true;
            var maxOrder = await _context.Tasks.Where(t => t.ColumnId == task.ColumnId).MaxAsync(t => (int?)t.OrderIndex) ?? -1;
            task.OrderIndex = maxOrder + 1;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null) return NotFound();
            if (task.Project == null || task.Project.OwnerId != GetCurrentUserId()) return Forbid();

            task.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("{id}/column")]
        public async Task<IActionResult> UpdateTaskColumn(int id, [FromBody] int newColumnId)
        {
            var task = await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null) return NotFound();
            if (task.Project == null || task.Project.OwnerId != GetCurrentUserId()) return Forbid();

            task.ColumnId = newColumnId;
            var maxOrder = await _context.Tasks.Where(t => t.ColumnId == newColumnId).MaxAsync(t => (int?)t.OrderIndex) ?? -1;
            task.OrderIndex = maxOrder + 1;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cap nhat thanh cong", task });
        }

        public class TaskReorderRequest
        {
            public int TaskId { get; set; }
            public int OrderIndex { get; set; }
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderTasks([FromBody] List<TaskReorderRequest> reorders)
        {
            if (reorders == null || !reorders.Any())
                return BadRequest("Khong co du lieu");

            foreach (var req in reorders)
            {
                var task = await _context.Tasks.FindAsync(req.TaskId);
                if (task != null)
                {
                    task.OrderIndex = req.OrderIndex;
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cap nhat thanh cong" });
        }
    }
}
