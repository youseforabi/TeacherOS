using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using TeacherOS.Database.Context;

namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentController(
        AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles="Student")]
    [HttpPost("{courseId}")]
    public async Task<IActionResult>
    Checkout(Guid courseId)
    {
        var course =
            await _context.Courses
            .FindAsync(courseId);

        if(course==null)
            return NotFound();

        var options =
            new SessionCreateOptions
            {
                PaymentMethodTypes =
                    new List<string>
                    {
                        "card"
                    },

                LineItems =
                    new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            PriceData=
                                new()
                                {
                                    Currency="usd",

                                    UnitAmount=
                                        (long)
                                        (course.Price*100),

                                    ProductData=
                                        new()
                                        {
                                            Name=
                                            course.Title
                                        }
                                },

                            Quantity=1
                        }
                    },

                Mode="payment",

                SuccessUrl=
                    "http://localhost:4200/success",

                CancelUrl=
                    "http://localhost:4200/cancel"
            };

        var service =
            new SessionService();

        var session =
            await service
            .CreateAsync(options);

        return Ok(new
        {
            session.Url
        });
    }
}