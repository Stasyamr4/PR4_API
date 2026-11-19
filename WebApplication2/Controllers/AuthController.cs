using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PR4_API.Data;
using PR4_API.DTOs;
using PR4_API.Models;

namespace PR4_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PR4_APIContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(PR4_APIContext context, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Заполните имя, логин и пароль" });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Пароль должен содержать не менее 6 символов" });

        var login = request.Login.Trim();
        if (await _context.Set<User>().AnyAsync(u => u.Login == login))
            return Conflict(new { message = "Пользователь с таким логином уже существует" });

        var user = new User
        {
            Name = request.Name.Trim(),
            Login = login,
            RoleID = 2,
            Password = ""
        };
        user.Password = _passwordHasher.HashPassword(user, request.Password);
        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Регистрация прошла успешно", userId = user.ID });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Login == request.Login);
        if (user == null)
            return Unauthorized(new { message = "Неверный логин или пароль" });

        PasswordVerificationResult verification = PasswordVerificationResult.Failed;
        bool valid;

        // Совместимость с тестовыми записями из SQL-скрипта: старый пароль сравнивается,
        // затем сразу заменяется хешем, чтобы не передавать его дальше в PasswordHasher.
        if (user.Password == request.Password)
        {
            user.Password = _passwordHasher.HashPassword(user, request.Password);
            await _context.SaveChangesAsync();
            valid = true;
        }
        else
        {
            try
            {
                verification = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
                valid = verification != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                valid = false;
            }
        }

        if (!valid)
            return Unauthorized(new { message = "Неверный логин или пароль" });

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Password = _passwordHasher.HashPassword(user, request.Password);
            await _context.SaveChangesAsync();
        }

        var role = user.RoleID == 1 ? "Admin" : "User";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(double.TryParse(_configuration["Jwt:ExpiresMinutes"], out var minutes) ? minutes : 120);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expires,
            user = new { user.ID, user.Name, user.Login, user.RoleID, role }
        });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Set<User>().Where(u => u.ID == id)
            .Select(u => new { u.ID, u.Name, u.Login, u.RoleID })
            .FirstOrDefaultAsync();
        return user == null ? NotFound() : Ok(user);
    }
}
