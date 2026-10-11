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
            bool isAdmin = User.FindFirst("username")?.Value?.ToLower() == "admin";
            if (project == null || (!isAdmin && project.OwnerId != currentUserId)) return Forbid();

            // Ưu tiên đọc trực tiếp từ bảng Subtasks (nơi lưu toàn bộ task con của cột)
            var subtasks = await _context.Subtasks
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId && s.IsActive)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            if (subtasks.Any())
            {
                return subtasks.Select(s => new AppTask
                {
                    TaskId = s.SubtaskId,
                    TaskName = s.Title,
                    Description = s.Description,
                    OrderIndex = s.OrderIndex,
                    IsActive = s.IsActive,
                    DueDate = s.DueDate,
                    Tags = s.Tags,
                    ColumnId = s.ColumnId,
                    ProjectId = s.ProjectId,
                    AssigneeId = s.AssigneeId
                }).ToList();
            }

            return await _context.Tasks
                .AsNoTracking()
                .Include(t => t.Subtasks)
                .Where(t => t.ProjectId == projectId && t.IsActive)
                .OrderBy(t => t.OrderIndex)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppTask>>> GetAllTasks()
        {
            var subtasks = await _context.Subtasks
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync();

            if (subtasks.Any())
            {
                return subtasks.Select(s => new AppTask
                {
                    TaskId = s.SubtaskId,
                    TaskName = s.Title,
                    Description = s.Description,
                    OrderIndex = s.OrderIndex,
                    IsActive = s.IsActive,
                    DueDate = s.DueDate,
                    Tags = s.Tags,
                    ColumnId = s.ColumnId,
                    ProjectId = s.ProjectId,
                    AssigneeId = s.AssigneeId
                }).ToList();
            }

            return await _context.Tasks
                .AsNoTracking()
                .Include(t => t.Subtasks)
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<AppTask>> CreateTask(AppTask task)
        {
            int currentUserId = GetCurrentUserId();
            var project = await _context.Projects.FindAsync(task.ProjectId);
            bool isAdmin = User.FindFirst("username")?.Value?.ToLower() == "admin";
            if (project == null || (!isAdmin && project.OwnerId != currentUserId)) return Forbid();

            task.IsActive = true;
            if (task.DueDate.HasValue)
            {
                task.DueDate = DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc);
            }
            var maxOrder = await _context.Subtasks.Where(s => s.ColumnId == task.ColumnId).MaxAsync(s => (int?)s.OrderIndex) 
                        ?? await _context.Tasks.Where(t => t.ColumnId == task.ColumnId).MaxAsync(t => (int?)t.OrderIndex) ?? -1;
            task.OrderIndex = maxOrder + 1;

            _context.Tasks.Add(task);

            // Đồng bộ lưu trực tiếp vào bảng Subtasks trên Database
            var subtask = new Subtask
            {
                Title = task.TaskName,
                Description = task.Description,
                OrderIndex = task.OrderIndex,
                IsActive = true,
                DueDate = task.DueDate,
                Tags = task.Tags,
                ColumnId = task.ColumnId,
                ProjectId = task.ProjectId,
                AssigneeId = task.AssigneeId
            };
            _context.Subtasks.Add(subtask);

            await _context.SaveChangesAsync();

            task.TaskId = subtask.SubtaskId;
            return Ok(task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] AppTask updatedTask)
        {
            if (id != updatedTask.TaskId) return BadRequest();

            bool isAdmin = User.FindFirst("username")?.Value?.ToLower() == "admin";

            var task = await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task != null)
            {
                if (!isAdmin)
                {
                    var project = task.Project ?? await _context.Projects.FindAsync(task.ProjectId);
                    if (project == null || project.OwnerId != GetCurrentUserId()) return Forbid();
                }

                task.TaskName = updatedTask.TaskName;
                task.Description = updatedTask.Description;
                task.AssigneeId = updatedTask.AssigneeId;
                task.DueDate = updatedTask.DueDate.HasValue ? DateTime.SpecifyKind(updatedTask.DueDate.Value, DateTimeKind.Utc) : null;
                task.Tags = updatedTask.Tags;
            }

            var subtask = await _context.Subtasks.Include(s => s.Project).FirstOrDefaultAsync(s => s.SubtaskId == id);
            if (subtask != null)
            {
                if (!isAdmin)
                {
                    var project = subtask.Project ?? await _context.Projects.FindAsync(subtask.ProjectId);
                    if (project == null || project.OwnerId != GetCurrentUserId()) return Forbid();
                }

                subtask.Title = updatedTask.TaskName;
                subtask.Description = updatedTask.Description;
                subtask.AssigneeId = updatedTask.AssigneeId;
                subtask.DueDate = updatedTask.DueDate.HasValue ? DateTime.SpecifyKind(updatedTask.DueDate.Value, DateTimeKind.Utc) : null;
                subtask.Tags = updatedTask.Tags;
            }

            if (task == null && subtask == null) return NotFound();

            await _context.SaveChangesAsync();
            return Ok(updatedTask);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            bool isAdmin = User.FindFirst("username")?.Value?.ToLower() == "admin";

            var task = await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task != null)
            {
                if (!isAdmin)
                {
                    var project = task.Project ?? await _context.Projects.FindAsync(task.ProjectId);
                    if (project == null || project.OwnerId != GetCurrentUserId()) return Forbid();
                }
                task.IsActive = false;
            }

            var subtask = await _context.Subtasks.Include(s => s.Project).FirstOrDefaultAsync(s => s.SubtaskId == id);
            if (subtask != null)
            {
                if (!isAdmin)
                {
                    var project = subtask.Project ?? await _context.Projects.FindAsync(subtask.ProjectId);
                    if (project == null || project.OwnerId != GetCurrentUserId()) return Forbid();
                }
                subtask.IsActive = false;
            }

            if (task == null && subtask == null) return NotFound();

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("{id}/column")]
        public async Task<IActionResult> UpdateTaskColumn(int id, [FromBody] int newColumnId)
        {
            bool isAdmin = User.FindFirst("username")?.Value?.ToLower() == "admin";

            var task = await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task != null)
            {
                if (!isAdmin)
                {
                    var project = task.Project ?? await _context.Projects.FindAsync(task.ProjectId);
                    if (project == null || project.OwnerId != GetCurrentUserId()) return Forbid();
                }
                task.ColumnId = newColumnId;
                var maxOrder = await _context.Tasks.Where(t => t.ColumnId == newColumnId).MaxAsync(t => (int?)t.OrderIndex) ?? -1;
                task.OrderIndex = maxOrder + 1;
            }

            var subtask = await _context.Subtasks.Include(s => s.Project).FirstOrDefaultAsync(s => s.SubtaskId == id);
            if (subtask != null)
            {
                if (!isAdmin)
                {
                    var project = subtask.Project ?? await _context.Projects.FindAsync(subtask.ProjectId);
                    if (project == null || project.OwnerId != GetCurrentUserId()) return Forbid();
                }
                subtask.ColumnId = newColumnId;
                var maxOrder = await _context.Subtasks.Where(s => s.ColumnId == newColumnId).MaxAsync(s => (int?)s.OrderIndex) ?? -1;
                subtask.OrderIndex = maxOrder + 1;
            }

            if (task == null && subtask == null) return NotFound();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cap nhat thanh cong" });
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
                if (task != null) task.OrderIndex = req.OrderIndex;

                var subtask = await _context.Subtasks.FindAsync(req.TaskId);
                if (subtask != null) subtask.OrderIndex = req.OrderIndex;
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cap nhat thanh cong" });
        }
    }
}

