using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.Models;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly PR4_APIContext _context;

        public CategoriesController(PR4_APIContext context)
        {
            _context = context;
        }

        // Получение списка всех категорий
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Set<Category>()
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.ID,
                    c.Name
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Set<Category>()
                .AsNoTracking()
                .Where(c => c.ID == id)
                .Select(c => new
                {
                    c.ID,
                    c.Name
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Категория не найдена"
                });
            }

            return Ok(category);
        }
    }
}
