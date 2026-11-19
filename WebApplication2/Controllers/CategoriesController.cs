using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.Models;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly PR4_APIContext _context;
        public CategoriesController(PR4_APIContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var items = await _context.Set<Category>().AsNoTracking().OrderBy(c => c.Name)
                .Select(c => new { c.ID, c.Name }).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var item = await _context.Set<Category>().AsNoTracking().Where(c => c.ID == id)
                .Select(c => new { c.ID, c.Name }).FirstOrDefaultAsync();
            return item == null ? NotFound() : Ok(item);
        }
    }
}