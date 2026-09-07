using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Bom (Inject) AppDbContext vào d? Controller có quy?n truy c?p Database
        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. L?y danh sách t?t c? Project (dành cho màn hình Dashboard)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _context.Projects.ToListAsync();
        }

        // 2. T?o m?t Project m?i
        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject(Project project)
        {
            // T? d?ng gán th?i gian t?o là lúc này
            project.CreatedAt = DateTime.Now;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Tr? v? d? li?u Project v?a t?o thành công
            return CreatedAtAction(nameof(GetProjects), new { id = project.ProjectId }, project);
        }
    }
}
