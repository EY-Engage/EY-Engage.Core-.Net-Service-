using Microsoft.AspNetCore.Http;

namespace EYEngage.Core.Application.InterfacesServices;


public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folderPath);
    Task<Stream> GetFileAsync(string filePath);
}
