using Microsoft.AspNetCore.Http;
using Rag.Service.Dtos;

namespace Rag.Service.FileServices.Interfaces
{
    public interface IIngestService
    {
        Task<IngestResultDto> IngestFromFileAsync(IFormFile File, string DocId);
        Task<IngestResultDto> IngestFromPathAsync(string filePath, string DocId,string extension);
    }
}
