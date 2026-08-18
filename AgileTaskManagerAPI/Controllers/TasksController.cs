using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy tất cả Task của một Project cụ thể
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<AppTask>>> GetTasksByProject(int projectId)
        {
            return await _context.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        }

        // 2. Tạo Task mới
        [HttpPost]
        public async Task<ActionResult<AppTask>> CreateTask(AppTask task)
        {
            // Cho phép cột Kanban tùy chỉnh (vd: "day 1"); mặc định ToDo nếu client không gửi
            if (string.IsNullOrWhiteSpace(task.Status))
                task.Status = "ToDo";

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        // 3. Cập nhật trạng thái Task (Dùng khi kéo thả thẻ Kanban)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] string newStatus)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound("Không tìm thấy Task");

            if (string.IsNullOrWhiteSpace(newStatus))
                return BadRequest("Trạng thái không được để trống!");

            task.Status = newStatus.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công", task });
        }
    }
}
