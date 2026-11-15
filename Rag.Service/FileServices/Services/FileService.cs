using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.FileServices.Interfaces;
using Rag.Service.FileServices.Requests;
using Rag.Service.FileServices.Responses;
using Serilog;

namespace Rag.Service.FileServices.Services
{
    public class FileService : IFileService
    {
        private readonly IFileItemRepository _fileItemRepository;
        private readonly IUploaderService _uploaderService;

        public FileService(
            IFileItemRepository fileItemRepository,
            IUploaderService uploaderService)
        {
            _fileItemRepository = fileItemRepository;
            _uploaderService = uploaderService;
        }

        public async Task<Response> UpdateFileDescriptionById(long fileItemId, string fileDescription)
        {
            Log.Information("Starting UpdateFileDescriptionById for FileItemId={FileItemId}", fileItemId);

            try
            {
                var fileItem = await _fileItemRepository.FindById(fileItemId);

                if (fileItem == null)
                {
                    Log.Warning("FileItem not found for Id {FileItemId}", fileItemId);
                    return new Response
                    {
                        HasError = true,
                        Message = "File not found."
                    };
                }

                fileItem.Description = fileDescription;

                _fileItemRepository.Update(fileItem);
                await _fileItemRepository.SaveChangesAsync();

                Log.Information("File description updated successfully for FileItemId={FileItemId}", fileItemId);

                return new Response
                {
                    HasError = false,
                    Message = "File description updated successfully."
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while updating file description for FileItemId {FileItemId}", fileItemId);
                throw;
            }
        }


        public async Task<FileInfoResponse> UploadFileAsync(FileUploadRequest fileUploadRequest)
        {
            Log.Information("Starting UploadFileAsync for file: {FileName}, CreatorId: {CreatorId}",
             fileUploadRequest.file?.FileName, fileUploadRequest.CreatorId);
            try
            {
                var file = await _uploaderService.UploadFile(fileUploadRequest.file);
                Log.Information("File upload result: Uploaded={Uploaded}, FileName={UploadedFileName}, FileSize={FileSize}",file.Uploaded, file.UploadedFilename, file.FileSize);
                if (file.Uploaded)
                {
                    var fileItem = new FileItem
                    {
                        CreatedByUserId = fileUploadRequest.CreatorId,
                        Description = fileUploadRequest.Description,
                        Name = file.UploadedFilename,
                        FileSize = file.FileSize,
                        FilePath = file.UploadedFilePath,
                        CreationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                    };
                    await _fileItemRepository.InsertAsync(fileItem);
                    await _fileItemRepository.SaveChangesAsync();
                    Log.Information("FileItem saved successfully with Id: {FileId}", fileItem.Id);
                    return new FileInfoResponse { Id = fileItem.Id };
                }
                else
                {
                    Log.Warning("File upload failed for {FileName}.", fileUploadRequest.file?.FileName);
                    throw new Exception("File not uploaded successfully.");
                }            
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in UploadFileAsync for file: {FileName}, CreatorId: {CreatorId}",
                    fileUploadRequest.file?.FileName, fileUploadRequest.CreatorId);
                throw;
            }
        }

        public async Task<FileInfoResponse> UploadExtractedImageAsync(ExtractedImageUploadRequest request)
        {
            Log.Information("Starting UploadExtractedImageAsync"); 
            try
            {
                var path = _uploaderService.UploadBase64ImageToPath(request.Image, request.FullPath);
                if (!string.IsNullOrEmpty(path))
                {
                    var fileItem = new FileItem
                    {
                        CreatedByUserId = Guid.NewGuid().ToString().Replace("-",""),
                        ChunkId = request.ChunkId,
                        FilePath = path,
                        CreationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                    };
                    await _fileItemRepository.InsertAsync(fileItem);
                    await _fileItemRepository.SaveChangesAsync();
                    Log.Information("FileItem saved successfully with Id: {FileId}", fileItem.Id);
                    return new FileInfoResponse { Id = fileItem.Id };
                }
                else
                {
                    throw new Exception("File not uploaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in UploadFileAsync for file");
                throw;
            }
        }
    }
}
