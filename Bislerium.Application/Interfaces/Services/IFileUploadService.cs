using Microsoft.AspNetCore.Http;

namespace Bislerium.Application.Interfaces.Services;

public interface IFileUploadService
{
    string UploadDocument(string uploadedFilePath, IFormFile file);
}