namespace Rag.Service.FileServices.Requests
{
    public class ExtractedImageUploadRequest
    {
        public string Image { get; set; }
        public long ChunkId { get; set; }
        public string FullPath { get; set; }
    }
}
