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
public class OrdersController : ControllerBase
{
    private readonly PR4_APIContext _context;
    public OrdersController(PR4_APIContext context) => _context = context;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        if (request.UseBasket == (request.ProductId.HasValue))
            return BadRequest(new 
            { message = "Выберите либо оформление корзины (UseBasket=true), либо один товар (ProductId)" }
            );
        await using var transaction = await _context.Database.BeginTransactionAsync();
        List<(Product product, int quantity)> orderItems;
        Basket? basket = null;

        if (request.UseBasket)
        {
            basket = await _context.Set<Basket>().FirstOrDefaultAsync(b => b.UserID == UserId);
            if (basket == null) 
                return BadRequest(new {
                message = "Корзина пуста" }
            );
            var lines = await _context.Set<BasketProduct>().Where(x => x.BasketID == basket.ID).ToListAsync();
            if (lines.Count == 0)
                return BadRequest(new 
            { message = "Корзина пуста" }
            );
            orderItems = new List<(Product, int)>();
            foreach (var line in lines)
            {
                var product = await _context.Set<Product>().FirstOrDefaultAsync(p => p.ID == line.ProductID);
                if (product == null || !product.Available || product.Count < line.CountProduct)
                    return BadRequest(new {
                        message = $"Недостаточно товара на складе: ID {line.ProductID}" }
                    );
                orderItems.Add((product, line.CountProduct));
            }
        }
        else
        {
            if (request.Quantity < 1) 
                return BadRequest(new 
                { message = "Количество должно быть больше нуля" });
            var product = await _context.Set<Product>().FirstOrDefaultAsync(p => p.ID == request.ProductId!.Value);
            if (product == null) 
                return NotFound(new
                { message = "Товар не найден" });
            if (!product.Available || product.Count < request.Quantity) 
                return BadRequest(new 
                { message = "Недостаточно товара на складе" });
            orderItems = new List<(Product, int)> { 
                (product, request.Quantity) 
            };
        }

        var total = orderItems.Sum(x => x.product.Price * x.quantity);
        var hasPvz = request.PvzId.HasValue;
        var hasAddress = request.AddressId.HasValue;
        if (hasPvz == hasAddress)
            return BadRequest(new
            { message = "Выберите один способ получения: ПВЗ или адрес" });
        if (total < 600 && !hasPvz)
            return BadRequest(new
            { message = "При сумме заказа менее 600 рублей доступна только доставка в ПВЗ" });
        if (hasPvz && !await _context.Set<AddressPVZ>().AnyAsync(x => x.ID == request.PvzId)) 
            return BadRequest(new
            { message = "ПВЗ не найден" });
        if (hasAddress && !await _context.Set<Address>().AnyAsync(x => x.ID == request.AddressId)) 
            return BadRequest(new
            { message = "Адрес не найден" });

        var order = new Order
        {
            DateOrder = DateTime.Now,
            UserID = UserId,
            StatusID = 1,
            AddressPVZID = request.PvzId,
            AddressID = request.AddressId,
            TotalPrice = total
        };
        _context.Set<Order>().Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in orderItems)
        {
            _context.Set<Product_Order>().Add(new Product_Order
            {
                ProductID = item.product.ID,
                OrderID = order.ID,
                Count = item.quantity,
                Price = item.product.Price
            });
            item.product.Count -= item.quantity;
            item.product.Available = item.product.Count > 0;
        }

        if (basket != null)
        {
            var lines = await _context.Set<BasketProduct>().Where(x => x.BasketID == basket.ID).ToListAsync();
            _context.Set<BasketProduct>().RemoveRange(lines);
            basket.Price = 0;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = "Заказ оформлен", orderId = order.ID, totalPrice = total, status = "Оформлен" });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders()
    {
        var orders = await (from o in _context.Set<Order>()
                            join s in _context.Set<Status>() on o.StatusID equals s.ID
                            where o.UserID == UserId
                            orderby o.DateOrder descending
                            select new { o.ID, o.DateOrder, o.TotalPrice, o.StatusID, Status = s.Name, o.AddressID, o.AddressPVZID })
                           .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _context.Set<Order>().AsNoTracking().FirstOrDefaultAsync(o => o.ID == id && o.UserID == UserId);
        if (order == null) 
            return NotFound(new 
            { message = "Заказ не найден" });
        var status = await _context.Set<Status>().Where(s => s.ID == order.StatusID).Select(s => s.Name).FirstOrDefaultAsync();
        var address = order.AddressID.HasValue ? await _context.Set<Address>().Where(a => a.ID == order.AddressID).Select(a => a.Name).FirstOrDefaultAsync() : null;
        var pvz = order.AddressPVZID.HasValue ? await _context.Set<AddressPVZ>().Where(a => a.ID == order.AddressPVZID).Select(a => a.Name).FirstOrDefaultAsync() : null;
        var products = await (from po in _context.Set<Product_Order>()
                              join p in _context.Set<Product>() on po.ProductID equals p.ID
                              where po.OrderID == id
                              select new { p.ID, p.Name, po.Count, UnitPrice = po.Price, Sum = po.Price * po.Count }).ToListAsync();
        return Ok(new { order.ID, order.DateOrder, order.TotalPrice, order.StatusID, status, deliveryAddress = address, pvz, products });
    }
}
