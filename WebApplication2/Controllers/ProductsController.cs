using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.Models;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly PR4_APIContext _context;

        public ProductsController(PR4_APIContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice)
        {
            var products = _context.Set<Product>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    EF.Functions.Like(p.Name, $"%{search}%"));
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryID == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p =>
                    p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p =>
                    p.Price <= maxPrice.Value);
            }

            var result = await products
                .OrderBy(p => p.Name)
                .Select(p => new
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
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Set<Product>()
                .AsNoTracking()
                .Where(p => p.ID == id)
                .Select(p => new
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
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Товар не найден"
                });
            }

            return Ok(product);
        }
    }
}

