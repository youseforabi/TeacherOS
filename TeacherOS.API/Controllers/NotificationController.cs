using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeacherOS.Database.Context;

namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class NotificationController
:ControllerBase
{
private readonly AppDbContext _context;

public NotificationController(
AppDbContext context
)
{
_context=context;
}

[HttpGet]

public async Task<IActionResult>
Get()
{
var userId=

Guid.Parse(

User.FindFirstValue(
ClaimTypes.NameIdentifier
)!
);

var notifications=

await _context.Notifications

.Where(x=>

x.UserId==userId
)

.OrderByDescending(
x=>x.CreatedAt
)

.ToListAsync();

return Ok(
notifications
);
}

[HttpGet("unread-count")]

public async Task<IActionResult>
Unread()
{
var userId=

Guid.Parse(

User.FindFirstValue(
ClaimTypes.NameIdentifier
)!
);

var count=

await _context.Notifications

.CountAsync(x=>

x.UserId==userId

&&

!x.IsRead
);

return Ok(count);
}

[HttpPut("{id}/read")]

public async Task<IActionResult>
MarkRead(Guid id)
{
var notification=

await _context.Notifications
.FindAsync(id);

if(notification==null)
return NotFound();

notification.IsRead=true;

await _context
.SaveChangesAsync();

return Ok();
}

[HttpPut("mark-all")]

public async Task<IActionResult>
MarkAll()
{
var userId=

Guid.Parse(

User.FindFirstValue(
ClaimTypes.NameIdentifier
)!
);

var notifications=

await _context.Notifications

.Where(x=>

x.UserId==userId

&&

!x.IsRead
)

.ToListAsync();

foreach(
var item
in notifications
)
{
item.IsRead=true;
}

await _context
.SaveChangesAsync();

return Ok();
}
}