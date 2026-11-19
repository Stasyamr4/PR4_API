using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.DTOs;
using PR4_API.Models;

namespace PR4_API.Controllers;

[ApiController]
[Authorize(Roles = "User,Admin")]
[Route("api/[controller]")]
public class BasketController : ControllerBase
{
    private readonly PR4_APIContext _context;
    public BasketController(PR4_APIContext context) => _context = context;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Basket> GetOrCreateBasket()
    {
        var basket = await _context.Set<Basket>().FirstOrDefaultAsync(b => b.UserID == UserId);
        if (basket == null)
        {
            basket = new Basket { UserID = UserId, Price = 0 };
            _context.Set<Basket>().Add(basket);
            await _context.SaveChangesAsync();
        }
        return basket;
    }

    [HttpGet]
    public async Task<IActionResult> GetBasket()
    {
        var basket = await GetOrCreateBasket();
        var items = await (from bp in _context.Set<BasketProduct>()
                           join p in _context.Set<Product>() on bp.ProductID equals p.ID
                           where bp.BasketID == basket.ID
                           select new { p.ID,
                               p.Name,
                               p.Price,
                               p.Image,
                               p.Count,
                               Quantity = bp.CountProduct,
                               Sum = p.Price * bp.CountProduct
                           }).ToListAsync();
        var total = items.Sum(x => x.Sum);
        basket.Price = total;
        await _context.SaveChangesAsync();
        return Ok(new { basketId = basket.ID, items, total });
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(BasketItemRequest request)
    {
        if (request.Quantity <= 0) 
            return BadRequest(new { message = "Количество должно быть больше нуля" });
        var product = await _context.Set<Product>().FirstOrDefaultAsync(p => p.ID == request.ProductId);
        if (product == null)
            return NotFound(new { message = "Товар не найден" });
        if (!product.Available || product.Count < request.Quantity) 
            return BadRequest(new { message = "Недостаточно товара на складе" });
        var basket = await GetOrCreateBasket();
        var line = await _context.Set<BasketProduct>().FirstOrDefaultAsync(x => x.BasketID == basket.ID && x.ProductID == product.ID);
        if (line == null) _context.Set<BasketProduct>().Add(new BasketProduct 
        { BasketID = basket.ID,
            ProductID = product.ID,
            CountProduct = request.Quantity });
        else
        {
            if (line.CountProduct + request.Quantity > product.Count)
                return BadRequest(new { message = "На складе недостаточно товара для такого количества" });
            line.CountProduct += request.Quantity;
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар добавлен в корзину" });
    }

    [HttpPut("items/{productId:int}")]
    public async Task<IActionResult> UpdateItem(int productId, BasketItemRequest request)
    {
        if (request.Quantity <= 0) 
            return BadRequest(new { message = "Количество должно быть больше нуля" });
        var basket = await GetOrCreateBasket();
        var line = await _context.Set<BasketProduct>().FirstOrDefaultAsync(x => x.BasketID == basket.ID && x.ProductID == productId);
        if (line == null)
            return NotFound(new { message = "Товара нет в корзине" });
        var product = await _context.Set<Product>().FindAsync(productId);
        if (product == null)
            return NotFound();
        if (request.Quantity > product.Count) 
            return BadRequest(new { message = "Недостаточно товара на складе" });
        line.CountProduct = request.Quantity;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Количество обновлено" });
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var basket = await GetOrCreateBasket();
        var line = await _context.Set<BasketProduct>().FirstOrDefaultAsync(x => x.BasketID == basket.ID && x.ProductID == productId);
        if (line == null) 
            return NotFound(new { message = "Товара нет в корзине" });
        _context.Set<BasketProduct>().Remove(line);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар удалён из корзины" });
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        var basket = await GetOrCreateBasket();
        var lines = await _context.Set<BasketProduct>().Where(x => x.BasketID == basket.ID).ToListAsync();
        _context.Set<BasketProduct>().RemoveRange(lines);
        basket.Price = 0;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Корзина очищена" });
    }
}
