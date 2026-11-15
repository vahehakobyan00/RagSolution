using System.ComponentModel.DataAnnotations;

namespace Rag.Data.Entity
{
    public class MenuItem
    {
        [Key]
        public long Id { get; set; }
        [MaxLength(200)]
        public string Name { get; set; } = default!;
        [MaxLength(100)]
        public string Category { get; set; } = default!;
        public string Description { get; set; } = string.Empty;
        /// <summary>Price in AMD.</summary>
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
