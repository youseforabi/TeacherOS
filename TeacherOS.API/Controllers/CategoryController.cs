using Microsoft.AspNetCore.Mvc;
using TeacherOS.Database.Context;
using TeacherOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoryController(
        AppDbContext context
    )
    {
        _context=context;
    }

    [HttpPost]
    public async Task<IActionResult>
    Create(string name)
    {
        var category =
            new Category
            {
                Name=name
            };

        _context.Categories
            .Add(category);

        await _context
            .SaveChangesAsync();

        return Ok(category);
    }

 [HttpGet]
public async Task<IActionResult> Get()
{
    var categories =
        await _context.Categories

        .Select(c => new
        {
            c.Id,
            c.Name,

            Courses =
                c.Courses.Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Price,
                    x.ThumbnailUrl
                })
        })

        .ToListAsync();

    return Ok(categories);
}
}