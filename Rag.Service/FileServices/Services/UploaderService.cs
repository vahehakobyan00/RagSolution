using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Rag.Service.FileServices.Interfaces;
using Rag.Service.FileServices.Responses;
using System.Net.Http.Headers;

namespace Rag.Service.FileServices.Services
{
    public class UploaderService : IUploaderService
    {
        private readonly IConfiguration _configuration;
        public UploaderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<UploadedResponse> UploadFile(IFormFile? file)
        {
            PathResponse path = GetVirtualPath(file);
            bool uploaded = await Upload(file, path.FullPath);

            if (uploaded)
            {
                return new UploadedResponse
                {
                    Uploaded = true,
                    UploadedFilename = path.Filename,
                    FileSize = GetFileSize(file),
                    UploadedFilePath = path.FilePath
                };

            }
            return new UploadedResponse();
        }

        private async Task<bool> Upload(IFormFile? file, string? filename)
        {
            if (!File.Exists(filename))
            {
                using (FileStream fs = File.Create(filename))
                {
                    await file.CopyToAsync(fs);
                    await fs.FlushAsync();
                }
            }
            return true;
        }

        private long GetFileSize(IFormFile? file)
        {
            long size = 0;
            if (file != null)
            {
                size = file.Length;
            }

            return size;
        }
        public PathResponse GetVirtualPath(IFormFile? file)
        {
            string fileStorage = _configuration.GetSection("FileStorage:SavePath").Value;
            var date = DateTime.Now;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var folderName = firstDayOfMonth.ToString("ddMMyy");
            var _path = fileStorage + $@"//{folderName}";

            string generatedName = Guid.NewGuid().ToString().Replace("-", "");
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
            var filename = ContentDispositionHeaderValue
                                    .Parse(file.ContentDisposition)
                                    .FileName?
                                    .Trim('"');
            var uploadedFilename = filename;
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var filenamenew = filename?.Replace(filenameWithoutExtension ?? "", generatedName);

            var title = filename;
            var fullPath = _path + $@"//{filenamenew}";
            var filePath = folderName + $@"//{filenamenew}";
            return new PathResponse
            {
                Filename = filename,
                Title = title,
                FullPath = fullPath,
                FilePath = filePath,
            };
        }

        public string UploadBase64Image(string base64String, string fileNameWithoutExt)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                throw new ArgumentNullException(nameof(base64String));

            // Extract mime/type if included (e.g., "data:image/png;base64,...")
            string mimeType = "image/png"; // default fallback
            if (base64String.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int start = base64String.IndexOf(':') + 1;
                int end = base64String.IndexOf(';');
                mimeType = base64String[start..end];

                // Remove the header
                base64String = base64String[(base64String.IndexOf(",") + 1)..];
            }

            // Convert from Base64 → bytes
            byte[] imageBytes = Convert.FromBase64String(base64String);

            // Determine file extension
            string extension = mimeType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".bin" // unknown type fallback
            };

            // Ensure directory exists
            string outputDirectory = GetDirectoryPath();
            Directory.CreateDirectory(outputDirectory);
            string fullPath = Path.Combine(outputDirectory, fileNameWithoutExt + extension);

            // Save file
            File.WriteAllBytes(fullPath, imageBytes);

            return fullPath; // return the actual saved path
        }

        public string UploadBase64ImageToPath(string base64String, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                throw new ArgumentNullException(nameof(base64String));

            // Extract mime/type if included (e.g., "data:image/png;base64,...")
            string mimeType = "image/png"; // default fallback
            if (base64String.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int start = base64String.IndexOf(':') + 1;
                int end = base64String.IndexOf(';');
                mimeType = base64String[start..end];

                // Remove the header
                base64String = base64String[(base64String.IndexOf(",") + 1)..];
            }

            // Convert from Base64 → bytes
            byte[] imageBytes = Convert.FromBase64String(base64String);

            // Ensure directory exists
            string outputDirectory = GetDirectoryPath();
            Directory.CreateDirectory(outputDirectory);
            
            // Save file
            File.WriteAllBytes(fullPath, imageBytes);

            return fullPath; // return the actual saved path
        }

        public string GetDirectoryPath()
        {
            string fileStorage = _configuration.GetSection("FileStorage:SavePath").Value;
            var date = DateTime.Now;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var folderName = firstDayOfMonth.ToString("ddMMyy");
            var _path = fileStorage + $@"/{folderName}";
            return _path;
        }

        public string GetFullPath(string base64String, string fileNameWithoutExt)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                throw new ArgumentNullException(nameof(base64String));

            // Extract mime/type if included (e.g., "data:image/png;base64,...")
            string mimeType = "image/png"; // default fallback
            if (base64String.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int start = base64String.IndexOf(':') + 1;
                int end = base64String.IndexOf(';');
                mimeType = base64String[start..end];

                // Remove the header
                base64String = base64String[(base64String.IndexOf(",") + 1)..];
            }

            // Determine file extension
            string extension = mimeType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".bin" // unknown type fallback
            };

            string outputDirectory = GetDirectoryPath();
            string fullPath = Path.Combine(outputDirectory, fileNameWithoutExt + extension);

            return fullPath; // return the actual saved path
        }
    }
}
