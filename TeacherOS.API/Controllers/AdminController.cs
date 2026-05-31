using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherOS.Database.Context;
using TeacherOS.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using TeacherOS.Hubs;

namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hub;

    public AdminController(AppDbContext context, IHubContext<NotificationHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var users = await _context.Users.CountAsync();
        var teachers = await _context.Users.CountAsync(x => x.Role == "Teacher");
        var students = await _context.Users.CountAsync(x => x.Role == "Student");
        var pendingCourses = await _context.Courses.CountAsync(x => !x.IsApproved);
        var totalCourses = await _context.Courses.CountAsync();

        var totalPlatformRevenue = await _context.Enrollments
            .Join(_context.Courses,
                e => e.CourseId,
                c => c.Id,
                (e, c) => c.Price)
            .SumAsync();

        return Ok(new
        {
            Users = users,
            Teachers = teachers,
            Students = students,
            PendingCourses = pendingCourses,
            TotalCourses = totalCourses,
            TotalRevenue = totalPlatformRevenue
        });
    }

    // ✨ تم الاحتفاظ بمسار الإشعارات للأدمن ليقرأ من جدول السجلات الجديد الغني بالبيانات الموزعة
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications()
    {
        var logs = await _context.SystemActivityLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(20) 
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("pending-courses")]
    public async Task<IActionResult> PendingCourses()
    {
        var courses = await _context.Courses
            .Where(x => !x.IsApproved && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Price,
                x.ThumbnailUrl,
                x.CreatedAt,
                x.IsApproved,

                Teacher = x.Teacher != null ? new
                {
                    Id = (Guid?)x.Teacher.Id,
                    Name = x.Teacher.Name,
                    Email = x.Teacher.Email
                } : null,

                Category = x.Category != null ? new
                {
                    Id = (Guid?)x.Category.Id,
                    Name = x.Category.Name
                } : null
            })
            .ToListAsync();

        return Ok(courses);
    }

    [HttpPut("{courseId}/approve")]
    public async Task<IActionResult> ApproveCourse(Guid courseId)
    {
        var course = await _context.Courses.Include(c => c.Teacher).FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        course.IsApproved = true;

        // إشعار جرس المستخدم الشخصي للمدرس
        _context.Notifications.Add(new Notification
        {
            UserId = course.TeacherId,
            Message = $"Your course {course.Title} was approved"
        });

        // سجل النشاط الأفقي الاحترافي الموجه للوحة الأدمن
        _context.SystemActivityLogs.Add(new SystemActivityLog
        {
            ActionType = "COURSE_APPROVAL",
            ActorName = course.Teacher?.Name ?? "Platform System",
            ActorRole = "Teacher",
            TargetName = course.Title,
            Message = $"Course '{course.Title}' has been approved by the Administrator."
        });

        await _context.SaveChangesAsync();

        await _hub.Clients.User(course.TeacherId.ToString())
            .SendAsync("CourseApproved", course);

        return Ok("Course Approved");
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsersDirectory()
    {
        var users = await _context.Users
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Email,
                x.Role
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("earnings-report")]
    public async Task<IActionResult> GetFinancialReport()
    {
        var salesReport = await _context.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Student)
            .OrderByDescending(e => e.PurchasedAt) 
            .Select(e => new
            {
                EnrollmentId = e.Id,
                CourseTitle = e.Course.Title,
                PricePaid = e.Course.Price,
                StudentName = e.Student != null ? e.Student.Name : "Unknown Student",
                PurchasedAt = e.PurchasedAt
            })
            .ToListAsync();

        return Ok(salesReport);
    }

    [HttpGet("all-reviews")]
    public async Task<IActionResult> GetAllReviews()
    {
        var reviews = await _context.Reviews
            .Include(r => r.Course)
            .Select(r => new {
                r.Id,
                r.Rating,
                r.Comment,
                CourseTitle = r.Course.Title
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("detailed-management-stats")]
    public async Task<IActionResult> GetDetailedManagementStats()
    {
        // 1. الأدمن: أسمائهم وإجمالي عدد عمليات الـ Approve التي قاموا بها من خلال الـ SystemActivityLogs
        var admins = await _context.Users
            .Where(x => x.Role == "Admin")
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Email,
                // بنحسب الأدمن عمل كام Approve بناءً على اسمه المسجل في لوج النشاطات
                TotalApproves = _context.SystemActivityLogs
                    .Count(log => log.ActionType == "COURSE_APPROVAL" && log.Message.Contains("by the Administrator")) 
                    // ملحوظة: بما أن حقل الـ ApprovedByUserId غير موجود بالـ Entity، اعتمدنا على السجلات بدقة هنا
            })
            .ToListAsync();

        // 2. المدرسين: أسمائهم، عدد كورساتهم، إجمالي الدروس، وتفاصيل كل كورس بالـ Lessons اللي جواه
        var teachers = await _context.Users
            .Where(x => x.Role == "Teacher")
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Email,
                TotalCourses = t.Courses.Count(c => !c.IsDeleted),
                TotalLessons = _context.Lessons.Count(l => l.Course!.TeacherId == t.Id),
                Courses = t.Courses
                    .Where(c => !c.IsDeleted)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        LessonsCount = c.Lessons.Count,
                        LessonsList = c.Lessons.OrderBy(l => l.Order).Select(l => new
                        {
                            l.Id,
                            l.Title,
                            l.Order,
                            l.ContentType
                        }).ToList()
                    }).ToList()
            })
            .ToListAsync();

        // 3. الكورسات والطلاب: كل كورس، اسم المدرس بتاعه، عدد الطلاب، وأسماء وإيميلات الطلاب المشتركين فيه فعلياً
        var coursesWithStudents = await _context.Courses
            .Where(c => !c.IsDeleted)
            .Select(c => new
            {
                CourseId = c.Id,
                CourseTitle = c.Title,
                TeacherName = c.Teacher.Name,
                StudentsCount = c.Enrollments.Count,
                EnrolledStudents = c.Enrollments.Select(e => new
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student.Name,
                    StudentEmail = e.Student.Email,
                    EnrolledAt = e.PurchasedAt
                }).ToList()
            })
            .ToListAsync();

        // تجميع البيانات في كائن مدمج ومنظم جداً للـ Angular
        return Ok(new
        {
            Admins = admins,
            Teachers = teachers,
            CoursesWithStudents = coursesWithStudents
        });
    }

    [HttpGet("financial-summary")]
public async Task<IActionResult> GetFinancialSummary()
{
    var summary = await _context.Courses
        .Where(c => !c.IsDeleted)
        .Select(c => new {
            c.Title,
            TotalSalesCount = c.Enrollments.Count,
            TotalRevenue = c.Enrollments.Count * c.Price,
            // نسبة مساهمة الكورس في إجمالي دخل المنصة
            RevenueShare = _context.Enrollments.Count() > 0 
                           ? (c.Enrollments.Count * c.Price) / _context.Enrollments.Sum(e => e.Course.Price) 
                           : 0
        })
        .OrderByDescending(x => x.TotalRevenue)
        .ToListAsync();

    return Ok(summary);
}



}