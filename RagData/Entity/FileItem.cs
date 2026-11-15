using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Entity
{
    public class FileItem
    {
        [Key]
        public long Id { get; set; }
        public string? Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public string? MetaType { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public long FileSize { get; set; }
        public string? FilePath { get; set; } = null!;
        public long? ChunkId { get; set; }
    }
}
