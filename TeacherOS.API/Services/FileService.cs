using Microsoft.AspNetCore.Http;

namespace TeacherOS.Services;

public class FileService
{

    
    private readonly IConfiguration _config;

    public FileService(
        IConfiguration config)
    {
        _config = config;
    }

 public async Task<string> UploadAsync(
    IFormFile file)
{
    if(file == null)
        return "";

    var uploadsFolder =
        Path.Combine(
            Directory.GetCurrentDirectory(),
            "Uploads"
        );

    if(!Directory.Exists(
        uploadsFolder))
    {
        Directory.CreateDirectory(
            uploadsFolder
        );
    }

    var fileName =
        $"{Guid.NewGuid()}" +
        Path.GetExtension(
            file.FileName);

    var path =
        Path.Combine(
            uploadsFolder,
            fileName);

    using var stream =
        new FileStream(
            path,
            FileMode.Create);

    await file.CopyToAsync(
        stream);

    return
        $"http://localhost:5130/Uploads/{fileName}";
}
}