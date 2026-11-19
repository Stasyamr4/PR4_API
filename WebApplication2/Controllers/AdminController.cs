using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.DTOs;
using PR4_API.Models;

namespace PR4_API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly PR4_APIContext _context;
    public AdminController(PR4_APIContext context) => _context = context;

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Price < 0 || request.Count < 0 || request.CountInProduct < 1)
            return BadRequest(new { message = "Проверьте название, цену и количество товара" });
        if (!await _context.Set<Category>().AnyAsync(c => c.ID == request.CategoryID)) return BadRequest(new { message = "Категория не найдена" });
        var product = new Product
        {
            Name = request.Name.Trim(), Price = request.Price, Count = request.Count,
            CountInProduct = request.CountInProduct, Description = request.Description ?? "",
            Image = request.Image ?? "", Rating = 0, CategoryID = request.CategoryID,
            Available = request.Count > 0 && request.Available
        };
        _context.Set<Product>().Add(product);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар создан", productId = product.ID });
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, ProductRequest request)
    {
        var product = await _context.Set<Product>().FindAsync(id);
        if (product == null) 
            return NotFound(new { message = "Товар не найден" });
        if (request.Price < 0 || request.Count < 0 || request.CountInProduct < 1) 
            return BadRequest(new { message = "Некорректные значения цены или количества" });
        if (!await _context.Set<Category>().AnyAsync(c => c.ID == request.CategoryID)) 
            return BadRequest(new { message = "Категория не найдена" });
        product.Name = request.Name.Trim(); product.Price = request.Price; product.Count = request.Count;
        product.CountInProduct = request.CountInProduct; product.Description = request.Description ?? "";
        product.Image = request.Image ?? ""; product.CategoryID = request.CategoryID;
        product.Available = request.Count > 0 && request.Available;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар обновлён" });
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Set<Product>().FindAsync(id);
        if (product == null) 
            return NotFound();
        var usedInOrders = await _context.Set<Product_Order>().AnyAsync(x => x.ProductID == id);
        if (usedInOrders) 
            return BadRequest(new { message = "Нельзя удалить товар, который уже присутствует в заказах. Сделайте его недоступным." });
        var basketLines = await _context.Set<BasketProduct>().Where(x => x.ProductID == id).ToListAsync();
        var likes = await _context.Set<Like>().Where(x => x.ProductID == id).ToListAsync();
        var comments = await _context.Set<Comment>().Where(x => x.ProductID == id).ToListAsync();
        _context.Set<BasketProduct>().RemoveRange(basketLines);
        _context.Set<Like>().RemoveRange(likes);
        _context.Set<Comment>().RemoveRange(comments);
        _context.Set<Product>().Remove(product);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Товар удалён" });
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(NameRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Укажите название категории" });
        var category = new Category { Name = request.Name.Trim() };
        _context.Set<Category>().Add(category);
        await _context.SaveChangesAsync();
        return Ok(new { category.ID, category.Name });
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, NameRequest request)
    {
        var category = await _context.Set<Category>().FindAsync(id);
        if (category == null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Укажите название категории" });
        category.Name = request.Name.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { message = "Категория обновлена" });
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Set<Category>().FindAsync(id);
        if (category == null) return NotFound();
        if (await _context.Set<Product>().AnyAsync(p => p.CategoryID == id)) return BadRequest(new 
        { message = "Нельзя удалить категорию, в которой есть товары" });
        _context.Set<Category>().Remove(category);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Категория удалена" });
    }

    [HttpPost("pvz")]
    public async Task<IActionResult> CreatePvz(NameRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Укажите адрес ПВЗ" });
        var pvz = new AddressPVZ { Name = request.Name.Trim() };
        _context.Set<AddressPVZ>().Add(pvz);
        await _context.SaveChangesAsync();
        return Ok(new { pvz.ID, pvz.Name });
    }

    [HttpPut("pvz/{id:int}")]
    public async Task<IActionResult> UpdatePvz(int id, NameRequest request)
    {
        var pvz = await _context.Set<AddressPVZ>().FindAsync(id);
        if (pvz == null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Укажите адрес ПВЗ" });
        pvz.Name = request.Name.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { message = "ПВЗ обновлён" });
    }

    [HttpDelete("pvz/{id:int}")]
    public async Task<IActionResult> DeletePvz(int id)
    {
        var pvz = await _context.Set<AddressPVZ>().FindAsync(id);
        if (pvz == null) return NotFound();
        if (await _context.Set<Order>().AnyAsync(o => o.AddressPVZID == id)) return BadRequest(new {
            message = "Нельзя удалить ПВЗ, который используется в заказах" });
        _context.Set<AddressPVZ>().Remove(pvz);
        await _context.SaveChangesAsync();
        return Ok(new { message = "ПВЗ удалён" });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await (from o in _context.Set<Order>()
                            join u in _context.Set<User>() on o.UserID equals u.ID
                            join s in _context.Set<Status>() on o.StatusID equals s.ID
                            orderby o.DateOrder descending
                            select new { o.ID, o.DateOrder,
                                o.UserID,
                                UserName = u.Name,
                                o.TotalPrice,
                                o.StatusID,
                                Status = s.Name,
                                o.AddressID,
                                o.AddressPVZID }).ToListAsync();
        return Ok(orders);
    }

    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, StatusRequest request)
    {
        var order = await _context.Set<Order>().FindAsync(id);
        if (order == null) return NotFound(new { message = "Заказ не найден" });
        if (!await _context.Set<Status>().AnyAsync(s => s.ID == request.StatusId)) 
            return BadRequest(new { message = "Статус не найден" });
        order.StatusID = request.StatusId;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Статус заказа обновлён" });
    }

    [HttpGet("statuses")]
    public async Task<IActionResult> GetStatuses()
    {
        var items = await _context.Set<Status>().AsNoTracking().OrderBy(s => s.ID).Select(s => new { s.ID, s.Name }).ToListAsync();
        return Ok(items);
    }

    [HttpDelete("comments/{id:int}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _context.Set<Comment>().FindAsync(id);
        if (comment == null)
            return NotFound();
        var productId = comment.ProductID;
        _context.Set<Comment>().Remove(comment);
        await _context.SaveChangesAsync();
        var ratings = await _context.Set<Comment>().Where(c => c.ProductID == productId).Select(c => c.Stars).ToListAsync();
        var parsed = ratings.Select(x => int.TryParse(x, out var value) ? value : 0).Where(x => x >= 1 && x <= 5).ToList();
        var product = await _context.Set<Product>().FindAsync(productId);
        if (product != null) product.Rating = parsed.Count == 0 ? 0 : parsed.Average();
        await _context.SaveChangesAsync();
        return Ok(new { message = "Отзыв удалён" });
    }
}
