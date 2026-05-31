using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeacherOS.Database.Context;
using TeacherOS.Domain.Entities;

namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _context;

    public EnrollmentController(
        AppDbContext context)
    {
        _context = context;
    }

[Authorize(Roles = "Student")]
[HttpPost("{courseId}")]
public async Task<IActionResult> Enroll(Guid courseId)
{
    var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // جلب اسم الطالب الحالي من سياق الـ Token أو الـ DB لتوثيقه في السجل
    var studentName = User.FindFirst(ClaimTypes.Name)?.Value ?? "A Student";

    var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == courseId);
    if (course == null) return NotFound("Course not found");

    var exists = await _context.Enrollments.AnyAsync(x => x.CourseId == courseId && x.StudentId == Guid.Parse(studentId!));
    if (exists) return BadRequest("Already enrolled");

    var enrollment = new Enrollment
    {
        CourseId = courseId,
        StudentId = Guid.Parse(studentId!)
    };

    _context.Enrollments.Add(enrollment);

    // ✨ زرع الـ Log الخاص بعملية الشراء للأدمن فوراً:
    _context.SystemActivityLogs.Add(new SystemActivityLog
    {
        ActionType = "STUDENT_ENROLLMENT",
        ActorName = studentName,
        ActorRole = "Student",
        TargetName = course.Title,
        Message = $"Student '{studentName}' successfully purchased '{course.Title}'."
    });

    await _context.SaveChangesAsync();
    return Ok(enrollment);
}
}