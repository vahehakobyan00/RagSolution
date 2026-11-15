using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Dtos
{
    public class KnowledgeBaseCategoriesList
    {
        public List<KnowledgeBaseCategoryGridItem> KnowledgeBaseCategories { get; set; } = new List<KnowledgeBaseCategoryGridItem>();
        public int Count { get; set; }
    }
    public class KnowledgeBaseCategoryGridItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
