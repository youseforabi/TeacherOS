using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeacherOS.Database.Context;
using TeacherOS.Domain.Entities; 

namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonController : ControllerBase
{
    private readonly AppDbContext _context;

    public LessonController(AppDbContext context)
    {
        _context = context;
    }
    
    [Authorize]
    [HttpGet("{lessonId}")]
    public async Task<IActionResult> GetLesson(Guid lessonId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var lesson = await _context.Lessons
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == lessonId);

        if (lesson == null)
            return NotFound();

        return Ok(new
        {
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Order,
            lesson.ContentType,
            lesson.Content,
            Course = new
            {
                lesson.Course.Id,
                lesson.Course.Title
            }
        });
    }

    // 🛠️ حل التضارب: قمنا بتغيير مسار الدالة الثانية بإضافة كلمة "details" لكي لا تتضارب مع الأولى
    [Authorize]
    [HttpGet("details/{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var lesson = await _context.Lessons
            .Where(x => x.Id == id)
            .Select(x => new {
                x.Id,
                x.Title,
                x.Description,
                x.Order,
                x.ContentType,
                x.Content
            })
            .FirstOrDefaultAsync();

        if (lesson == null)
            return NotFound();

        return Ok(lesson);
    }

    [Authorize(Roles = "Student")]
    [HttpPost("lesson/{lessonId}/complete")]
    public async Task<IActionResult> CompleteLesson(Guid lessonId)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var progress = await _context.LessonProgresses
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.LessonId == lessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                StudentId = studentId,
                LessonId = lessonId
            };
            _context.LessonProgresses.Add(progress);
        }

        progress.IsCompleted = true;
        progress.CompletedAt = DateTime.UtcNow;
        progress.LastViewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Roles = "Student")]
    [HttpGet("{courseId}/progress")]
    public async Task<IActionResult> CourseProgress(Guid courseId)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var totalLessons = await _context.Lessons
            .CountAsync(x => x.CourseId == courseId);

        var completed = await _context.LessonProgresses
            .CountAsync(x => x.StudentId == studentId && x.Lesson.CourseId == courseId);

        var percent = totalLessons == 0 ? 0 : (completed * 100) / totalLessons;

        return Ok(new
        {
            completed,
            totalLessons,
            percent
        });
    }   

    [Authorize(Roles = "Student")]
    [HttpPost("{lessonId}/track")]
    public async Task<IActionResult> TrackLesson(Guid lessonId)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var progress = await _context.LessonProgresses
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.LessonId == lessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                StudentId = studentId,
                LessonId = lessonId,
                LastViewedAt = DateTime.UtcNow
            };
            _context.LessonProgresses.Add(progress);
        }
        else
        {
            progress.LastViewedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}