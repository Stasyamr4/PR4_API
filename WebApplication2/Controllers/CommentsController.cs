using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.DTOs;
using PR4_API.Models;

namespace PR4_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly PR4_APIContext _context;
    public CommentsController(PR4_APIContext context) => _context = context;

    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetProductComments(int productId)
    {
        var comments = await (from c in _context.Set<Comment>()
                              join u in _context.Set<User>() on c.UserID equals u.ID
                              where c.ProductID == productId
                              orderby c.ID descending
                              select new { c.ID,
                                  c.ProductID,
                                  c.UserID,
                                  UserName = u.Name,
                                  c.Stars,
                                  c.Response
                              }).ToListAsync();
        var average = comments.Count == 0 ? 0 : comments.Average(c => int.TryParse(c.Stars, out var value) ? value : 0);
        return Ok(new { averageRating = Math.Round(average, 2), comments });
    }

    [Authorize(Roles = "User,Admin")]
    [HttpPost("product/{productId:int}")]
    public async Task<IActionResult> AddComment(int productId, CommentRequest request)
    {
        if (request.Stars < 1 || request.Stars > 5)
            return BadRequest(new { message = "Оценка должна быть от 1 до 5" });
        if (string.IsNullOrWhiteSpace(request.Response)) 
            return BadRequest(new { message = "Напишите текст отзыва" });
        if (!await _context.Set<Product>().AnyAsync(p => p.ID == productId)) 
            return NotFound(new { message = "Товар не найден" });
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _context.Set<Comment>().Add(new Comment { 
            ProductID = productId,
            UserID = userId,
            Stars = request.Stars.ToString(),
            Response = request.Response.Trim()
        });
        await _context.SaveChangesAsync();
        await UpdateRating(productId);
        return Ok(new { message = "Отзыв добавлен" });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _context.Set<Comment>().FirstOrDefaultAsync(c => c.ID == id);
        if (comment == null) 
            return NotFound(new { message = "Отзыв не найден" });
        var productId = comment.ProductID;
        _context.Set<Comment>().Remove(comment);
        await _context.SaveChangesAsync();
        await UpdateRating(productId);
        return Ok(new { message = "Отзыв удалён" });
    }

    private async Task UpdateRating(int productId)
    {
        var values = await _context.Set<Comment>().Where(c => c.ProductID == productId)
            .Select(c => c.Stars).ToListAsync();
        var parsed = values.Select(x => int.TryParse(x, out var value) ? value : 0).Where(x => x >= 1 && x <= 5).ToList();
        var product = await _context.Set<Product>().FirstAsync(p => p.ID == productId);
        product.Rating = parsed.Count == 0 ? 0 : parsed.Average();
        await _context.SaveChangesAsync();
    }
}
