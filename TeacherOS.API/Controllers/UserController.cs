using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherOS.Database.Context;
using TeacherOS.Domain.Entities;
using TeacherOS.Features.Users.DTOs;
using TeacherOS.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public UserController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

[HttpPost("register")]
public async Task<IActionResult>
Register(RegisterDto dto)
{
    bool exists =

        await _context.Users
        .AnyAsync(x=>

            x.Email==
            dto.Email
        );

    if(exists)
    {
        return BadRequest(
            "Email already exists"
        );
    }

    string role="Student";

    if(dto.Role=="Teacher")
    {
        role="Teacher";
    }

    if(dto.Role=="Admin")
    {
        var requesterRole =
            User.FindFirstValue(
                ClaimTypes.Role
            );

        if(requesterRole!="Admin")
        {
            return BadRequest(
                "Only admin can create admin accounts"
            );
        }

        role="Admin";
    }

    var user = new User
    {
        Name=dto.Name,

        Email=dto.Email,

        Password=
            BCrypt.Net.BCrypt
            .HashPassword(
                dto.Password
            ),

        Role=role
    };

    _context.Users.Add(user);

    await _context.SaveChangesAsync();

    return Ok(
        "Registered Successfully"
    );
}
    [HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    var user = await _context.Users
        .FirstOrDefaultAsync(x => x.Email == dto.Email);

    if (user == null)
        return Unauthorized("Invalid credentials");

    bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

    if (!isValid)
        return Unauthorized("Invalid credentials");

    var token = _tokenService.CreateToken(user);

    return Ok(new
    {
        token,
        user = new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role
        }
    });
}

    // GET ALL
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

[Authorize(Roles = "Admin")]
[HttpGet("admin-dashboard")]
public IActionResult AdminDashboard()
{
    return Ok("Welcome Admin");
}

[Authorize(Roles = "Student")]
[HttpGet("student-dashboard")]
public IActionResult StudentDashboard()
{
    return Ok("Welcome Student");
}
[Authorize]
[HttpGet("profile")]
public IActionResult Profile()
{
    return Ok("Authenticated User");
}



[Authorize]
[HttpGet("me")]
public async Task<IActionResult> Me()
{
    var id =
        Guid.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            )!
        );

    var user =
        await _context.Users
        .FindAsync(id);

    if(user==null)
        return NotFound();

    return Ok(new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Role
    });
}
}