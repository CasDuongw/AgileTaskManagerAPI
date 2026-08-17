using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Bơm (Inject) AppDbContext vào để Controller có quyền truy cập Database
        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách tất cả Project (dành cho màn hình Dashboard)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _context.Projects.ToListAsync();
        }

        // 2. Tạo một Project mới
        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject(Project project)
        {
            // Tự động gán thời gian tạo là lúc này
            project.CreatedAt = DateTime.Now;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Trả về dữ liệu Project vừa tạo thành công
            return CreatedAtAction(nameof(GetProjects), new { id = project.ProjectId }, project);
        }
    }
}
