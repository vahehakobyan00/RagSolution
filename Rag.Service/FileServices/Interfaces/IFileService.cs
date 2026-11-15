using Rag.Service.Dtos;
using Rag.Service.FileServices.Requests;
using Rag.Service.FileServices.Responses;

namespace Rag.Service.FileServices.Interfaces
{
    public interface IFileService
    {
        Task<FileInfoResponse> UploadFileAsync(FileUploadRequest fileUploadRequest);
        Task<Response> UpdateFileDescriptionById(long fileItemId, string fileDescription);
        Task<FileInfoResponse> UploadExtractedImageAsync(ExtractedImageUploadRequest request);
    }
}
