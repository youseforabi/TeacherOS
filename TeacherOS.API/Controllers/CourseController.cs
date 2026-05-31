    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;
    using TeacherOS.Database.Context;
    using TeacherOS.Domain.Entities;
    using TeacherOS.Features.Courses.DTOs;
    using TeacherOS.Services;

    using Microsoft.AspNetCore.SignalR;
    using TeacherOS.Hubs;
    namespace TeacherOS.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {

        private readonly IHubContext<NotificationHub>_hub;


        private readonly AppDbContext _context;

        public CourseController(AppDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub=hub;

        }


    [Authorize(Roles="Teacher")]
    [HttpPost]
    public async Task<IActionResult>
    Create(
    [FromForm] CreateCourseDto dto,
    [FromServices] FileService fileService
    )
    {
        var teacherId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        var image =
            await fileService.UploadAsync(
                dto.Thumbnail
            );
    var exists =
        await _context.Courses
        .AnyAsync(x=>

            x.Title.ToLower()
            == dto.Title.ToLower()

            &&

            x.TeacherId ==
            Guid.Parse(teacherId!)
        );

    if(exists)
    {
        return BadRequest(
            "Course already exists"
        );
    }
       var course = new Course
{
    Title=dto.Title,

    Description=dto.Description,

    Price=dto.Price,

    CategoryId=dto.CategoryId,

    ThumbnailUrl=image,

    TeacherId=
        Guid.Parse(
            teacherId!
        ),

    IsApproved=false
};
        _context.Courses.Add(course);

        await _context.SaveChangesAsync();

        return Ok(course);
    }
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 6, [FromQuery] string search = "", [FromQuery] string categoryId = "")
{
    var query = _context.Courses
        .Include(x => x.Category)  // 👈 أضف ده عشان تجيب الـ Category
        .Include(x => x.Teacher)   // 👈 وأضف ده عشان تجيب الـ Teacher
        .Where(x => !x.IsDeleted && x.IsApproved)
        .AsQueryable();

    if (!string.IsNullOrEmpty(search))
        query = query.Where(x => x.Title.Contains(search));

    if (!string.IsNullOrEmpty(categoryId))
        query = query.Where(x => x.CategoryId.ToString() == categoryId);

    var total = await query.CountAsync();
    
    var courses = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new  // 👈 Projection مخصص عشان تضمن الشكل النهائي
        {
            x.Id,
            x.Title,
            x.Description,
            x.Price,
            x.ThumbnailUrl,
            x.IsApproved,
            x.IsDeleted,
            x.CreatedAt,
            x.TeacherId,
            CategoryName = x.Category != null ? x.Category.Name : null,  // 👈最关键
            TeacherName = x.Teacher != null ? x.Teacher.Name : null       // 👈最关键
        })
        .ToListAsync();

    return Ok(new { total, courses });
}
 [Authorize(Roles = "Teacher")]
[HttpGet("teacher-courses")]
public async Task<IActionResult> TeacherCourses(int page = 1, int pageSize = 6, string search = "")
{
    // 1. جلب الـ ID بتاع المدرس الحالي من الـ Token
    var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 2. بناء الكويري الأساسية (بنجيب كورسات المدرس ده، واللي معمولها Approve، ومش ممسوحة)
    var query = _context.Courses
        .Where(x => x.TeacherId == teacherId && x.IsApproved && !x.IsDeleted);

    // 3. تطبيق فلتر البحث لو المدرس كتب حاجة في خانة الـ Search
    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
    }

    // 4. حساب إجمالي عدد الكورسات بعد الفلترة (مهم جداً للـ Angular عشان يحسب عدد الصفحات)
    var totalCount = await query.CountAsync();

    // 5. تطبيق الباجينيشن (Skip لعديد العناصر السابقة، و Take لعدد عناصر الصفحة الحالية فقط)
    var courses = await query
        .OrderByDescending(x => x.Id) // ترتيب اختياري لجعل الأحدث يظهر أولاً
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new
        {
            x.Id,
            x.Title,
            x.Description,
            x.Price,
            x.ThumbnailUrl,
            x.IsApproved
        })
        .ToListAsync();

    // 6. إرجاع الأوبجكت المدمج اللي الـ Angular مستنيه بالظبط
    return Ok(new 
    { 
        total = totalCount, 
        courses = courses 
    });
}
   
   [Authorize(Roles = "Teacher")]
[HttpPost("{courseId}/lesson")]
public async Task<IActionResult> AddLesson(Guid courseId, [FromBody] CreateLessonDto dto)
{
    // 1. حماية أولية للـ Model
    if (dto == null || string.IsNullOrEmpty(dto.Title) || string.IsNullOrEmpty(dto.Content))
    {
        return BadRequest(new { message = "Title and Content are strictly required fields." });
    }

    if (!ModelState.IsValid) return BadRequest(ModelState);

    try
    {
        // 2. حساب الـ Order بأمان
        var nextOrder = await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .Select(l => (int?)l.Order)
            .MaxAsync() ?? 0;

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Order = nextOrder + 1, 
            ContentType = dto.ContentType?.ToLower()?.Trim() ?? "youtube",
            Content = dto.Content?.Trim() ?? string.Empty, // حماية هنا بالـ ? لمنع الـ 500 Error
            CourseId = courseId
        };

        _context.Lessons.Add(lesson);

        // 3. تسجيل الـ System Activity Log بأمان
        var teacherName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Instructor";
        _context.SystemActivityLogs.Add(new SystemActivityLog
        {
            ActionType = "LESSON_UPLOAD",
            ActorName = teacherName,
            ActorRole = "Teacher",
            TargetName = lesson.Title, 
            Message = $"Instructor '{teacherName}' added a new lesson titled '{lesson.Title}'."
        });

        await _context.SaveChangesAsync();

        // 4. جلب الطلاب المشتركين لإرسال الإشعارات
        var studentIds = await _context.Enrollments
            .Where(x => x.CourseId == courseId)
            .Select(x => x.StudentId.ToString())
            .ToListAsync();

        if (studentIds.Any())
        {
            var notifications = studentIds.Select(studentId => new Notification
            {
                UserId = Guid.Parse(studentId),
                Message = $"New lesson published: {lesson.Title}"
            });
            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            // بث SignalR بالخلفية بدون تعطيل الـ Request الرئيسي
            _ = _hub.Clients.Users(studentIds).SendAsync("ReceiveNotification", $"New lesson: {lesson.Title}");
        }

        // 5. الإرجاع المضمون: نرجع الأوبجكت مباشرة بدل الـ CreatedAtAction لو كان مسبب تعارض في الـ Routing
        return Ok(lesson);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Fatal Error AddLesson]: {ex.Message}");
        return StatusCode(500, new { message = "An internal server error occurred while processing the payload.", details = ex.Message });
    }
}
        [HttpGet("lesson/{lessonId}/details")]   // اسم الـ Route مختلف عشان ما يتعارضش مع GetLesson
    public async Task<IActionResult> GetLessonById(Guid lessonId)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound("Lesson not found");

        return Ok(new
        {
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Order,
            lesson.ContentType,
            lesson.Content,
            lesson.CourseId
        });
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(Guid id)
    {
        var course =
            await _context.Courses
            .Where(x => x.Id == id)

            .Select(x => new
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Price = x.Price,
                ThumbnailUrl = x.ThumbnailUrl,

                TeacherName =
                    x.Teacher.Name,

                CategoryName =
                    x.Category.Name,

                Lessons =
                    x.Lessons.Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.Description,
                        l.Order
                    }),

                Reviews =
                    x.Reviews.Select(r => new
                    {
                        r.Rating,
                        r.Comment
                    })
            })

            .FirstOrDefaultAsync();

        if(course == null)
            return NotFound();

        return Ok(course);
    }
        [Authorize(Roles="Student")]
    [HttpPost("{courseId}/buy")]
    public async Task<IActionResult> BuyCourse(
        Guid courseId
    )
    {
        var studentId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        var course =
            await _context.Courses
            .FindAsync(courseId);

        if(course == null)
            return NotFound();

        var alreadyPurchased =
            await _context.Enrollments.AnyAsync(
                x =>
                x.CourseId == courseId &&
                x.StudentId ==
                Guid.Parse(studentId!)
            );

        if(alreadyPurchased)
            return BadRequest(
                "Already purchased"
            );

        var enrollment =
            new Enrollment
            {
                StudentId =
                    Guid.Parse(studentId!),

                CourseId = courseId,

                PurchasedAt =
                    DateTime.UtcNow
            };

        _context.Enrollments.Add(
            enrollment
        );

        await _context.SaveChangesAsync();
        _context.Notifications.Add(

    new Notification
    {
    UserId=
    course.TeacherId,

    Message=

    $"New purchase on {

    course.Title

    }"

    }
    );

    await _context
    .SaveChangesAsync();

    await _hub
    .Clients
    .User(
    course.TeacherId
    .ToString()
    )
    .SendAsync(

    "ReceiveNotification",

    $"New purchase on {

    course.Title

    }"

    );

        return Ok(
            "Course Purchased Successfully"
        );
    }

    [Authorize(Roles="Student")]
    [HttpGet("my-courses")]
    public async Task<IActionResult> MyCourses()
    {
        var studentId =
            Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )!
            );

        var courses =
            await _context.Enrollments

            .Where(x =>
                x.StudentId == studentId)

            .Select(x => new
            {
                x.Course.Id,
                x.Course.Title,
                x.Course.Description,
                x.Course.Price,
                x.Course.ThumbnailUrl
            })

            .ToListAsync();

        return Ok(courses);
    }
[Authorize]
    [HttpGet("lesson/{lessonId}")]
    public async Task<IActionResult> GetLesson(Guid lessonId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var lesson = await _context.Lessons
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == lessonId);

        if (lesson == null)
            return NotFound();

        // ADMIN
        if (role == "Admin")
        {
            return Ok(new
            {
                lesson.Id,
                lesson.Title,
                lesson.Description,
                lesson.Order,
                lesson.ContentType,
                lesson.Content,
                lesson.CourseId
            });
        }

        // TEACHER OWNER
        if (lesson.Course.TeacherId.ToString() == userId)
        {
            return Ok(new
            {
                lesson.Id,
                lesson.Title,
                lesson.Description,
                lesson.Order,
                lesson.ContentType,
                lesson.Content,
                lesson.CourseId
            });
        }

        bool enrolled = await _context.Enrollments
            .AnyAsync(x => x.StudentId.ToString() == userId && x.CourseId == lesson.CourseId);

        if (!enrolled)
        {
            return Forbid("Buy course first");
        }

        return Ok(new
        {
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Order,
            lesson.ContentType,
            lesson.Content,
            lesson.CourseId
        });
    }    [Authorize(Roles="Teacher")]
    [HttpPut("lesson/{lessonId}")]

    public async Task<IActionResult>
    UpdateLesson(

    Guid lessonId,

    CreateLessonDto dto

    )
    {

    var lesson =

    await _context.Lessons
    .FindAsync(lessonId);

    if(lesson==null)
    return NotFound();

    lesson.Title=
    dto.Title;

    lesson.Description=
    dto.Description;

    lesson.Order=
    dto.Order;

    lesson.Content=
    dto.Content;

    lesson.ContentType=
    dto.ContentType;

    await _context
    .SaveChangesAsync();

    return Ok(new
    {
        lesson.Id,
        lesson.Title,
        lesson.Description,
        lesson.Order,
        lesson.ContentType,
        lesson.Content,
        lesson.CourseId
    });
    }


[Authorize]
[HttpGet("{courseId}/lessons")]
public async Task<IActionResult> GetCourseLessons(Guid courseId)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var role = User.FindFirstValue(ClaimTypes.Role);

    // اختياري: التحقق إن الكورس موجود أصلاً
    var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
    if (!courseExists)
        return NotFound("Course not found");

    // جلب الدروس
    var lessons = await _context.Lessons
        .Where(l => l.CourseId == courseId)
        .OrderBy(l => l.Order)           // مهم جداً عشان الترتيب
        .Select(l => new
        {
            l.Id,
            l.Title,
            l.Order,
            l.ContentType,
            l.Content,          // لو عايز تجيب المحتوى (فيديو، نص، etc.)
            // أضف أي حقل تاني محتاجه
        })
        .ToListAsync();

    return Ok(lessons);
}
[Authorize(Roles = "Teacher")]
[HttpDelete("lesson/{lessonId}")]
public async Task<IActionResult> DeleteLesson(Guid lessonId)
{
    // 1. البحث عن الدرس المراد حذفه
    var lesson = await _context.Lessons.FindAsync(lessonId);

    if (lesson == null)
        return NotFound();

    // حفظ الـ CourseId في متغير قبل حذف الدرس عشان نستخدمه في إعادة الترتيب
    var courseId = lesson.CourseId;

    // 2. حذف الدرس من قاعدة البيانات
    _context.Lessons.Remove(lesson);
    await _context.SaveChangesAsync();

    // 3. إعادة ترتيب الدروس المتبقية تلقائياً لسد الفجوة
    var remainingLessons = await _context.Lessons
        .Where(x => x.CourseId == courseId)
        .OrderBy(x => x.Order) // ترتيبهم الحالي
        .ToListAsync();

    // حلقة تكرارية لتحديث الـ Order يبدأ من 1 بالتسلسل
    int newOrder = 1;
    foreach (var remainingLesson in remainingLessons)
    {
        remainingLesson.Order = newOrder;
        newOrder++;
    }

    // حفظ الترتيب الجديد في قاعدة البيانات
    await _context.SaveChangesAsync();

    // ارجاع أوبجكت JSON صريح عشان الـ Angular ميتلخبطش زي ما حلينا المشكلة اللي فاتت
    return Ok(new { message = "Deleted" });
}
    [Authorize(Roles="Student")]
    [HttpPost("{courseId}/review")]
    public async Task<IActionResult>
    Review(
    Guid courseId,
    int rating,
    string comment
    )
    {
        var studentId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        var review =
            new Review
            {
                Rating=rating,
                Comment=comment,
                StudentId=
                    Guid.Parse(studentId!),

                CourseId=courseId
            };

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync();

        var course=

    await _context.Courses
    .FindAsync(courseId);

    _context.Notifications.Add(

    new Notification
    {
    UserId=
    course!.TeacherId,

    Message=

    $"New review on {

    course.Title

    }"

    }
    );

    await _context
    .SaveChangesAsync();

    await _hub
    .Clients
    .User(
    course.TeacherId
    .ToString()
    )
    .SendAsync(

    "ReceiveNotification",

    $"New review on {

    course.Title

    }"

    );

return Ok(new { message = "Deleted" });
        }

    [HttpGet("{courseId}/reviews")]
    public async Task<IActionResult>
    GetReviews(Guid courseId)
    {
        var reviews =
            await _context.Reviews
            .Where(x =>
                x.CourseId==courseId
            )
            .ToListAsync();

        return Ok(reviews);
    }

[Authorize(Roles = "Teacher")]
[HttpGet("teacher-dashboard")]
public async Task<IActionResult> TeacherDashboard()
{
    var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 1. جلب كورسات المدرس
    var teacherCourses = await _context.Courses
        .Where(x => x.TeacherId == teacherId && !x.IsDeleted)
        .ToListAsync();

    var courseIds = teacherCourses.Select(x => x.Id).ToList();

    // 2. حساب عدد الطلاب
    var studentsCount = await _context.Enrollments
        .Where(x => courseIds.Contains(x.CourseId))
        .Select(x => x.StudentId)
        .Distinct()
        .CountAsync();

    // 3. [البديل الجديد] حساب إجمالي التقييمات للمدرس
    var totalReviews = await _context.Reviews
        .CountAsync(x => courseIds.Contains(x.CourseId));

    // 4. جلب آخر 5 طلاب سجلوا
    var recentEnrollments = await _context.Enrollments
        .Where(x => courseIds.Contains(x.CourseId))
        .Include(x => x.Student)
        .Include(x => x.Course)
        .OrderByDescending(x => x.PurchasedAt)
        .Take(5)
        .Select(x => new {
            StudentName = x.Student.Name,
            CourseTitle = x.Course.Title,
            PurchasedAt = x.PurchasedAt
        })
        .ToListAsync();

    // 5. جلب الكورسات الأعلى مبيعاً (شيلنا منها الـ Revenue وخليناها ترجع عدد الطلاب بس)
    var topCourses = await _context.Enrollments
        .Where(x => courseIds.Contains(x.CourseId))
        .GroupBy(x => x.CourseId)
        .Select(g => new {
            CourseTitle = _context.Courses.Where(c => c.Id == g.Key).Select(c => c.Title).FirstOrDefault(),
            StudentsCount = g.Count()
        })
        .OrderByDescending(x => x.StudentsCount)
        .Take(3)
        .ToListAsync();

    // 6. إرجاع النتيجة بالبيانات الجديدة
    return Ok(new
    {
        TotalCourses = teacherCourses.Count,
        TotalStudents = studentsCount,
        TotalReviews = totalReviews, // الكارت الجديد مكان الـ Revenue
        RecentEnrollments = recentEnrollments,
        TopCourses = topCourses
    });
}
    [Authorize(Roles="Teacher")]
    [HttpGet("top-course")]
    public async Task<IActionResult>
    TopCourse()
    {
        var teacherId =
            Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )!
            );

        var topCourse =
            await _context.Enrollments

            .Join(
                _context.Courses,
                e=>e.CourseId,
                c=>c.Id,
                (e,c)=>new{e,c}
            )

            .Where(x =>
                x.c.TeacherId==
                teacherId
            )

            .GroupBy(x=>x.c.Title)

            .Select(g=>new
            {
                Course=g.Key,
                Sales=g.Count()
            })

            .OrderByDescending(
                x=>x.Sales
            )

            .FirstOrDefaultAsync();

        return Ok(topCourse);
    }

  
    [Authorize(Roles="Teacher")]
    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics()
    {
        var teacherId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var revenue =
            await _context.Enrollments
                .Where(x =>
                    x.Course.TeacherId
                    .ToString()==teacherId)
                .SumAsync(x =>
                    x.Course.Price);

        var avgRating =
            await _context.Reviews
                .Where(x =>
                    x.Course.TeacherId
                    .ToString()==teacherId)
                .AverageAsync(
                    x=>(double?)x.Rating)
                ?? 0;

        return Ok(new
        {
            Revenue = revenue,
            AverageRating = avgRating
        });
    }
    [Authorize(Roles="Teacher")]
    [HttpPut("{courseId}")]
    public async Task<IActionResult>
    UpdateCourse(
    Guid courseId,
    UpdateCourseDto dto
    )
    {
        var teacherId =
            Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )!
            );

        var course =
            await _context.Courses
            .FindAsync(courseId);

        if(course==null)
            return NotFound();

        if(course.TeacherId!=teacherId)
            return Forbid();

        course.Title =
            dto.Title;

        course.Description =
            dto.Description;

        course.Price =
            dto.Price;

        await _context
            .SaveChangesAsync();

        return Ok(course);
    }
    [Authorize(Roles="Teacher")]
    [HttpDelete("{courseId}")]
    public async Task<IActionResult>
    Delete(Guid courseId)
    {
        var teacherId =
            Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )!
            );

        var course =
            await _context.Courses
            .FindAsync(courseId);

        if(course==null)
            return NotFound();

        if(course.TeacherId!=teacherId)
            return Forbid();

    course.IsDeleted=true;
        await _context.SaveChangesAsync();

        return Ok(
            "Deleted Successfully"
        );
    }

    [Authorize(Roles="Teacher")]
    [HttpGet("my-teaching-courses")]
    public async Task<IActionResult>
    MyTeachingCourses()
    {
        var teacherId =
            Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )!
            );

        var courses =
            await _context.Courses

           .Where(x => x.TeacherId == teacherId && !x.IsDeleted) 
            .ToListAsync();

        return Ok(courses);
    }
[Authorize(Roles="Student")]
[HttpPost("{courseId}/favorite")]
public async Task<IActionResult> Favorite(Guid courseId)
{
    var studentId =
        Guid.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            )!
        );

    var exists =
        await _context.Wishlists
        .FirstOrDefaultAsync(x=>

            x.StudentId==studentId
            &&

            x.CourseId==courseId
        );

    if(exists != null)
    {
        _context.Wishlists.Remove(exists);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            added=false
        });
    }

    _context.Wishlists.Add(
        new Wishlist
        {
            StudentId=studentId,
            CourseId=courseId
        });

    await _context.SaveChangesAsync();

    return Ok(new
    {
        added=true
    });
}


 [Authorize(Roles="Student")]
[HttpGet("wishlist")]
public async Task<IActionResult> Wishlist()
{
    var studentId =
        Guid.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            )!
        );

    var courses =

        await _context.Wishlists

        .Where(x=>
            x.StudentId==studentId
        )

        .Include(x=>x.Course)
        .ThenInclude(x=>x.Teacher)

        .Include(x=>x.Course)
        .ThenInclude(x=>x.Category)

        .Select(x=>new
        {
            x.Course.Id,
            x.Course.Title,
            x.Course.Description,
            x.Course.Price,
            x.Course.ThumbnailUrl,

            TeacherName=
                x.Course.Teacher.Name,

            CategoryName=
                x.Course.Category.Name
        })

        .ToListAsync();

    return Ok(courses);
}

    [Authorize(Roles="Student")]
    [HttpGet("purchased-ids")]
    public async Task<IActionResult>
    PurchasedIds()
    {
        var studentId =
            Guid.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )!
            );

        var ids =
            await _context.Enrollments
            .Where(x=>
                x.StudentId==studentId
            )
            .Select(x=>x.CourseId)
            .ToListAsync();

        return Ok(ids);
    }

[Authorize(Roles = "Student")]
[HttpGet("student-dashboard")]
public async Task<IActionResult> StudentDashboard()
{
    var studentId = Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!
    );

    var purchasedCourses = await _context.Enrollments
        .Where(x => x.StudentId == studentId)
        .Select(x => x.Course)
        .ToListAsync();

    var purchasedCount = purchasedCourses.Count;
    var purchasedCourseIds = purchasedCourses.Select(c => c.Id).ToList();

    // 1. حساب إجمالي الدروس المكتملة لكل الكروت
    var completedCount = await _context.LessonProgresses
        .CountAsync(x => x.StudentId == studentId 
                      && purchasedCourseIds.Contains(x.Lesson.CourseId) 
                      && x.IsCompleted);

    // 2. حساب إجمالي عدد دروس كل الكورسات المشترك بها
    var totalLessons = await _context.Lessons
        .CountAsync(x => purchasedCourseIds.Contains(x.CourseId));

    double overallProgress = 0;
    if (totalLessons > 0)
    {
        overallProgress = (double)completedCount / totalLessons * 100;
    }

    // جلب آخر درس وقّف عنده الطالب فعلياً
    var lastViewedProgress = await _context.LessonProgresses
        .Where(x => x.StudentId == studentId && purchasedCourseIds.Contains(x.Lesson.CourseId))
        .OrderByDescending(x => x.LastViewedAt)
        .Include(x => x.Lesson)
        .ThenInclude(l => l.Course)
        .FirstOrDefaultAsync();

    object? continueWatching = null;

    if (lastViewedProgress != null)
    {
        var targetCourseId = lastViewedProgress.Lesson.CourseId;

        // 🎯 حساب نسبة تقدم الطالب في هذا الكورس المحدد بالتحديد
        var courseTotalLessons = await _context.Lessons.CountAsync(l => l.CourseId == targetCourseId);
        var courseCompletedLessons = await _context.LessonProgresses
            .CountAsync(lp => lp.StudentId == studentId && lp.Lesson.CourseId == targetCourseId && lp.IsCompleted);

        double courseProgress = 0;
        if (courseTotalLessons > 0)
        {
            courseProgress = (double)courseCompletedLessons / courseTotalLessons * 100;
        }

        continueWatching = new
        {
            Id = targetCourseId, 
            LessonId = lastViewedProgress.LessonId,
            Title = lastViewedProgress.Lesson.Course.Title, 
            ThumbnailUrl = lastViewedProgress.Lesson.Course.ThumbnailUrl,
            Progress = Math.Round(courseProgress) // 👈 النسبة المئوية للكورس الحالي اللي الـ Angular مستنيها
        };
    }
    else
    {
        // لو لسه متفرجش على حاجة خالص، نرجع أول كورس اشتراه كـ Default بنسبة تقدم 0
        var firstEnrollment = await _context.Enrollments
            .Where(x => x.StudentId == studentId)
            .Include(x => x.Course)
            .FirstOrDefaultAsync();

        if (firstEnrollment != null)
        {
            continueWatching = new
            {
                Id = firstEnrollment.CourseId,
                LessonId = Guid.Empty,
                Title = firstEnrollment.Course.Title,
                ThumbnailUrl = firstEnrollment.Course.ThumbnailUrl,
                Progress = 0 // 👈 كورس جديد تماماً لسه مبدأش فيه
            };
        }
    }

    return Ok(new
    {
        PurchasedCourses = purchasedCount, 
        Progress = Math.Round(overallProgress), // الـ Overall progress للدائرة الكبيرة
        Certificates = 0,
        ContinueWatching = continueWatching
    });
}
    [Authorize(Roles="Student")]
[HttpGet("continue-learning")]

public async Task<IActionResult>
ContinueLearning()
{
    var studentId =
        Guid.Parse(

User.FindFirstValue(
ClaimTypes.NameIdentifier
)!

);

    var lesson =
        await _context
        .LessonProgresses

        .Where(x=>
            x.StudentId==
            studentId
        )

        .OrderByDescending(
            x=>x.LastViewedAt
        )

        .Select(x=>new
        {
            CourseId =
                x.Lesson.CourseId,

            LessonId =
                x.LessonId,

            CourseTitle =
                x.Lesson.Course.Title,

            Thumbnail =
                x.Lesson.Course
                .ThumbnailUrl
        })

        .FirstOrDefaultAsync();

    return Ok(lesson);
}

[Authorize(Roles="Student")]
[HttpGet("recent-lessons")]

public async Task<IActionResult>
RecentLessons()
{
    var studentId =
        Guid.Parse(

User.FindFirstValue(
ClaimTypes.NameIdentifier
)!

);

    var lessons =
        await _context
        .LessonProgresses

        .Where(x=>
            x.StudentId==
            studentId
        )

        .OrderByDescending(
            x=>x.LastViewedAt
        )

        .Take(5)

        .Select(x=>new
        {
            LessonId=
                x.Lesson.Id,

            LessonTitle=
                x.Lesson.Title,

            CourseTitle=
            x.Lesson.Course
                .Title,

            LastViewedAt=
                x.LastViewedAt
        })

        .ToListAsync();

    return Ok(lessons);
}

[Authorize(Roles="Student")]
[HttpPost("lesson/{lessonId}/view")]
public async Task<IActionResult> ViewLesson(Guid lessonId)
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
            LastViewedAt = DateTime.UtcNow,
            IsCompleted = false
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
[Authorize(Roles = "Student")]
[HttpDelete("{courseId}/unfavorite")] // 👈 مخصص فقط للحذف باستخدام HttpDelete
public async Task<IActionResult> Unfavorite(Guid courseId)
{
    var studentId = Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!
    );

    var wishlistitem = await _context.Wishlists
        .FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId);

    if (wishlistitem == null)
    {
        return NotFound("Course not found in wishlist");
    }

    _context.Wishlists.Remove(wishlistitem);
    await _context.SaveChangesAsync();

    return Ok("Course removed from wishlist");
}

}