using Microsoft.AspNetCore.Mvc;

namespace TeacherOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file)
    {
        if (file == null)
            return BadRequest();

        var folder = "Uploads";

        Directory.CreateDirectory(folder);

        var fileName =
            Guid.NewGuid()
            + Path.GetExtension(file.FileName);

        var path =
            Path.Combine(folder, fileName);

        using var stream =
            new FileStream(path, FileMode.Create);

        await file.CopyToAsync(stream);

        return Ok(new
        {
            url = $"/Uploads/{fileName}"
        });
    }
}