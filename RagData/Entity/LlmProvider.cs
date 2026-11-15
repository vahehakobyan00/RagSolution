using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Entity
{
    public class LlmProvider
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
