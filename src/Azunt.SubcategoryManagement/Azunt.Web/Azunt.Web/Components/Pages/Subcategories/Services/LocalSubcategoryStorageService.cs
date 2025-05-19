using Azunt.SubcategoryManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Azunt.Web.Components.Pages.Subcategories.Services
{
    public class LocalSubcategoryStorageService : ISubcategoryStorageService
    {
        private readonly string _container = "files";
        private readonly string _subFolder;
        private readonly string _rootPath;
        private readonly ILogger<LocalSubcategoryStorageService> _logger;

        public LocalSubcategoryStorageService(IWebHostEnvironment env, ILogger<LocalSubcategoryStorageService> logger, string subFolder = "subcategories")
        {
            _logger = logger;
            _subFolder = subFolder;

            _rootPath = Path.Combine(env.WebRootPath, _container, _subFolder);

            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        public async Task<string> UploadAsync(Stream stream, string fileName)
        {
            string safeFileName = GetUniqueFileName(fileName);
            string fullPath = Path.Combine(_rootPath, safeFileName);

            using (var fileStream = File.Create(fullPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            // 웹 접근 가능한 상대 경로 반환
            return $"/{_container}/{_subFolder}/{safeFileName}";
        }

        public Task<Stream> DownloadAsync(string fileName)
        {
            string fullPath = Path.Combine(_rootPath, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Subcategory not found: {fileName}");

            var stream = File.OpenRead(fullPath);
            return Task.FromResult<Stream>(stream);
        }

        public Task DeleteAsync(string fileName)
        {
            string fullPath = Path.Combine(_rootPath, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private string GetUniqueFileName(string originalName)
        {
            string baseName = Path.GetFileNameWithoutExtension(originalName);
            string extension = Path.GetExtension(originalName);
            string newFileName = originalName;
            int count = 1;

            while (File.Exists(Path.Combine(_rootPath, newFileName)))
            {
                newFileName = $"{baseName}({count}){extension}";
                count++;
            }

            return newFileName;
        }
    }
}