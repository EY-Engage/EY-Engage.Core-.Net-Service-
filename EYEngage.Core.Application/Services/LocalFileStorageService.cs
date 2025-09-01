using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;


    namespace EYEngage.Core.Application.Services;

    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderPath)
        {
            ValidateFile(file);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullFolderPath = Path.Combine(_env.WebRootPath, folderPath);
            Directory.CreateDirectory(fullFolderPath);

            var filePath = Path.Combine(fullFolderPath, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/{folderPath}/{fileName}";
        }

        public Task<Stream> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
                throw new FileNotFoundException("File not found", fullPath);

            return Task.FromResult((Stream)new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null)
                throw new ValidationException("Aucun fichier reçu");

            if (file.Length > MaxFileSize)
                throw new ValidationException($"Taille maximale autorisée : {MaxFileSize / 1024 / 1024} Mo");

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                throw new ValidationException($"Extensions autorisées : {string.Join(", ", AllowedExtensions)}");
        }
    }

