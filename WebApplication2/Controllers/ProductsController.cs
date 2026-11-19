using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.Models;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly PR4_APIContext _context;
        public ProductsController(PR4_APIContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetProducts(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, bool onlyAvailable = false)
        {
            var query = _context.Set<Product>().AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));
            if (categoryId.HasValue) query = query.Where(p => p.CategoryID == categoryId.Value);
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
            if (onlyAvailable) query = query.Where(p => p.Available && p.Count > 0);
            var result = await query.OrderBy(p => p.Name).Select(p => new
            {
                p.ID,
                p.Name,
                p.Price,
                p.Count,
                p.CountInProduct,
                p.Description,
                p.Image,
                p.Rating,
                p.CategoryID,
                p.Available
            }).ToListAsync();
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Set<Product>().AsNoTracking()
                .Where(p => p.ID == id)
                .Select(p => new { p.ID, p.Name, p.Price, p.Count, p.CountInProduct, p.Description, p.Image, p.Rating, p.CategoryID, p.Available })
                .FirstOrDefaultAsync();
            return product == null ? NotFound(new { message = "Товар не найден" }) : Ok(product);
        }
    }
}