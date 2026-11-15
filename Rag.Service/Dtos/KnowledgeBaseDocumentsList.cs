using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class KnowledgeBaseDocumentsList
    {
        public List<KnowledgeBaseDocumentGridItem> KnowledgeBaseDocuments { get; set; } = new List<KnowledgeBaseDocumentGridItem>();
        public int Count { get; set; }
    }
    public class KnowledgeBaseDocumentGridItem
    {
        public long Id { get; set; }    
        public string FileName { get; set; }
        public string FileDescription { get; set; }
        public long FileSize {  get; set; }
        public string KbName {  get; set; }
        public string KbCategoryName {  get; set; }
        public DateTime UploadDate {  get; set; }
        public DateTime? VectorStoreSyncDate {  get; set; }
    }
}
