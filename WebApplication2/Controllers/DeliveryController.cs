using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PR4_API.Data;
using PR4_API.Models;

namespace PR4_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly PR4_APIContext _context;
    public DeliveryController(PR4_APIContext context) => _context = context;

    [HttpGet("pvz")]
    public async Task<IActionResult> GetPvz()
    {
        var items = await _context.Set<AddressPVZ>().AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.ID, x.Name }).ToListAsync();
        return Ok(items);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses()
    {
        var items = await _context.Set<Address>().AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.ID, x.Name }).ToListAsync();
        return Ok(items);
    }
}
