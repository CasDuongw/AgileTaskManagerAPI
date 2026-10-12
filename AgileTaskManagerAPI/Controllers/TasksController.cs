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
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            var subtasks = await _context.Subtasks
                .Where(s => s.ProjectId == projectId && s.IsActive)
                .OrderBy(s => s.OrderIndex)
                .ThenBy(s => s.SubtaskId)
                .ToListAsync();

            // Auto-heal nếu có duplicate OrderIndex trong cùng 1 cột
            var byCol = subtasks.GroupBy(s => s.ColumnId ?? s.TaskId);
            bool needHeal = false;
            foreach (var grp in byCol)
            {
                if (grp.GroupBy(s => s.OrderIndex).Any(g => g.Count() > 1))
                {
                    needHeal = true;
                    var ordered = grp.OrderBy(s => s.SubtaskId).ToList();
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        ordered[i].OrderIndex = i;
                    }
                }
            }
            if (needHeal)
            {
                await _context.SaveChangesAsync();
            }

            return subtasks.OrderBy(s => s.OrderIndex).ThenBy(s => s.SubtaskId).Select(s => new AppTask
            {
                TaskId = s.SubtaskId,
                TaskName = s.Title,
                Description = s.Description,
                OrderIndex = s.OrderIndex,
                IsActive = s.IsActive,
                DueDate = s.DueDate.HasValue ? DateTime.SpecifyKind(s.DueDate.Value, DateTimeKind.Utc) : null,
                Tags = s.Tags,
                ColumnId = s.ColumnId ?? s.TaskId,
                ProjectId = s.ProjectId ?? projectId,
                AssigneeId = s.AssigneeId
            }).ToList();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppTask>>> GetAllTasks()
        {
            var subtasks = await _context.Subtasks
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync();

            return subtasks.Select(s => new AppTask
            {
                TaskId = s.SubtaskId,
                TaskName = s.Title,
                Description = s.Description,
                OrderIndex = s.OrderIndex,
                IsActive = s.IsActive,
                DueDate = s.DueDate.HasValue ? DateTime.SpecifyKind(s.DueDate.Value, DateTimeKind.Utc) : null,
                Tags = s.Tags,
                ColumnId = s.ColumnId ?? s.TaskId,
                ProjectId = s.ProjectId ?? 0,
                AssigneeId = s.AssigneeId
            }).ToList();
        }

        [HttpPost]
        public async Task<ActionResult<AppTask>> CreateTask(AppTask task)
        {
            var project = await _context.Projects.FindAsync(task.ProjectId);
            if (project == null) return NotFound();

            task.IsActive = true;
            if (task.DueDate.HasValue)
            {
                task.DueDate = DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc);
            }

            if (task.ColumnId <= 0)
            {
                var firstCol = await _context.KanbanColumns
                    .Where(c => c.ProjectId == task.ProjectId)
                    .OrderBy(c => c.OrderIndex)
                    .FirstOrDefaultAsync();
                if (firstCol != null)
                {
                    task.ColumnId = firstCol.ColumnId;
                }
            }

            // Đảm bảo Task lớn tương ứng với ColumnId tồn tại trong bảng Tasks
            AppTask? mainTask = null;
            if (task.ColumnId > 0)
            {
                mainTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ColumnId == task.ColumnId);
                if (mainTask == null)
                {
                    var col = await _context.KanbanColumns.FindAsync(task.ColumnId);
                    if (col != null)
                    {
                        mainTask = new AppTask
                        {
                            TaskName = col.ColumnName,
                            ProjectId = col.ProjectId,
                            ColumnId = col.ColumnId,
                            OrderIndex = col.OrderIndex,
                            IsActive = true,
                            DueDate = col.DueDate
                        };
                        _context.Tasks.Add(mainTask);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            int targetTaskId = mainTask != null ? mainTask.TaskId : (task.ColumnId > 0 ? task.ColumnId : 1);

            var maxOrder = await _context.Subtasks
                .Where(s => (s.ColumnId == task.ColumnId || s.TaskId == targetTaskId) && s.IsActive)
                .MaxAsync(s => (int?)s.OrderIndex) ?? -1;
            
            var subtask = new Subtask
            {
                Title = task.TaskName,
                Description = task.Description,
                OrderIndex = maxOrder + 1,
                IsActive = true,
                DueDate = task.DueDate,
                Tags = task.Tags,
                ColumnId = task.ColumnId,
                ProjectId = task.ProjectId,
                AssigneeId = task.AssigneeId,
                TaskId = targetTaskId,
                IsCompleted = false
            };

            _context.Subtasks.Add(subtask);
            await _context.SaveChangesAsync();

            task.TaskId = subtask.SubtaskId;
            task.OrderIndex = subtask.OrderIndex;
            return Ok(task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] AppTask updatedTask)
        {
            if (id != updatedTask.TaskId) return BadRequest();

            var subtask = await _context.Subtasks.FindAsync(id);
            if (subtask != null)
            {
                subtask.Title = updatedTask.TaskName;
                subtask.Description = updatedTask.Description;
                subtask.AssigneeId = updatedTask.AssigneeId;
                subtask.DueDate = updatedTask.DueDate.HasValue ? DateTime.SpecifyKind(updatedTask.DueDate.Value, DateTimeKind.Utc) : null;
                subtask.Tags = updatedTask.Tags;
                await _context.SaveChangesAsync();
                return Ok(updatedTask);
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.TaskName = updatedTask.TaskName;
                task.Description = updatedTask.Description;
                task.AssigneeId = updatedTask.AssigneeId;
                task.DueDate = updatedTask.DueDate.HasValue ? DateTime.SpecifyKind(updatedTask.DueDate.Value, DateTimeKind.Utc) : null;
                task.Tags = updatedTask.Tags;
                await _context.SaveChangesAsync();
                return Ok(updatedTask);
            }

            return NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var subtask = await _context.Subtasks.FindAsync(id);
            if (subtask != null)
            {
                subtask.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok();
            }

            return NotFound();
        }

        [HttpPatch("{id}/column")]
        public async Task<IActionResult> UpdateTaskColumn(int id, [FromBody] int newColumnId)
        {
            var subtask = await _context.Subtasks.FindAsync(id);
            if (subtask != null)
            {
                subtask.ColumnId = newColumnId;
                var mainTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ColumnId == newColumnId);
                if (mainTask != null)
                {
                    subtask.TaskId = mainTask.TaskId;
                }
                else
                {
                    var col = await _context.KanbanColumns.FindAsync(newColumnId);
                    if (col != null)
                    {
                        mainTask = new AppTask
                        {
                            TaskName = col.ColumnName,
                            ProjectId = col.ProjectId,
                            ColumnId = col.ColumnId,
                            OrderIndex = col.OrderIndex,
                            IsActive = true,
                            DueDate = col.DueDate
                        };
                        _context.Tasks.Add(mainTask);
                        await _context.SaveChangesAsync();
                        subtask.TaskId = mainTask.TaskId;
                    }
                }

                var maxOrder = await _context.Subtasks.Where(s => s.ColumnId == newColumnId || s.TaskId == subtask.TaskId).MaxAsync(s => (int?)s.OrderIndex) ?? -1;
                subtask.OrderIndex = maxOrder + 1;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cap nhat thanh cong" });
            }

            return NotFound();
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
                var subtask = await _context.Subtasks.FindAsync(req.TaskId);
                if (subtask != null) subtask.OrderIndex = req.OrderIndex;
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cap nhat thanh cong" });
        }
    }
}

