using Microsoft.AspNetCore.Http;
using Rag.Service.FileServices.Responses;

namespace Rag.Service.FileServices.Interfaces
{
    public interface IUploaderService
    {
        Task<UploadedResponse> UploadFile(IFormFile? file);
        string UploadBase64Image(string base64String, string fileNameWithoutExt);
        string GetDirectoryPath();
        string UploadBase64ImageToPath(string base64String, string fullPath);
        string GetFullPath(string base64String, string fileNameWithoutExt);
    }
}
