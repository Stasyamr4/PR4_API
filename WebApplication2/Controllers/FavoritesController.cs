using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.Models;

namespace PR4_API.Controllers;

[ApiController]
[Authorize(Roles = "User,Admin")]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    private readonly PR4_APIContext _context;
    public FavoritesController(PR4_APIContext context) => _context = context;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetFavorites()
    {
        var items = await (from f in _context.Set<Like>()
                           join p in _context.Set<Product>() on f.ProductID equals p.ID
                           where f.UserID == UserId
                           select new { p.ID, p.Name, p.Price, p.Image, p.Available, p.Count }).ToListAsync();
        return Ok(items);
    }

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> AddFavorite(int productId)
    {
        if (!await _context.Set<Product>().AnyAsync(p => p.ID == productId)) return NotFound(new { message = "Товар не найден" });
        if (await _context.Set<Like>().AnyAsync(x => x.UserID == UserId && x.ProductID == productId))
            return Conflict(new { message = "Товар уже в избранном" });
        _context.Set<Like>().Add(new Like { UserID = UserId, ProductID = productId });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар добавлен в избранное" });
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> RemoveFavorite(int productId)
    {
        var item = await _context.Set<Like>().FirstOrDefaultAsync(x => x.UserID == UserId && x.ProductID == productId);
        if (item == null) return NotFound(new { message = "Товар не найден в избранном" });
        _context.Set<Like>().Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар удалён из избранного" });
    }

    [HttpPost("{productId:int}/to-basket")]
    public async Task<IActionResult> MoveToBasket(int productId)
    {
        var favorite = await _context.Set<Like>().FirstOrDefaultAsync(x => x.UserID == UserId && x.ProductID == productId);
        if (favorite == null)
            return NotFound(new 
            { message = "Товар не найден в избранном" });
        var product = await _context.Set<Product>().FirstOrDefaultAsync(p => p.ID == productId);
        if (product == null) 
            return NotFound();
        if (!product.Available || product.Count < 1) 
            return BadRequest(new 
            { message = "Товар сейчас недоступен" });
        var basket = await _context.Set<Basket>().FirstOrDefaultAsync(b => b.UserID == UserId);
        if (basket == null)
        {
            basket = new Basket { UserID = UserId, Price = 0 };
            _context.Set<Basket>().Add(basket);
            await _context.SaveChangesAsync();
        }
        var line = await _context.Set<BasketProduct>().FirstOrDefaultAsync(x => x.BasketID == basket.ID && x.ProductID == productId);
        if (line == null) _context.Set<BasketProduct>().Add(new BasketProduct { BasketID = basket.ID, ProductID = productId, CountProduct = 1 });
        else if (line.CountProduct < product.Count) line.CountProduct++;
        else return BadRequest(new
        { message = "На складе недостаточно товара" });
        _context.Set<Like>().Remove(favorite);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар перенесён в корзину" });
    }
}
