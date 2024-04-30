using Microsoft.AspNetCore.Http;

namespace Bislerium.Application.DTOs.Upload;

public class UploadDto
{
    public string FilePath { get; set; }
    
    public List<IFormFile> Files { get; set; }
}