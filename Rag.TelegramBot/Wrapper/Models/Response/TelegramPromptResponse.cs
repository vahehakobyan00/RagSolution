using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.TelegramBot.Wrapper.Models.Response
{
    public class TelegramPromptResponse
    {
        public string Answer { get; set; }
        public object Matches { get; set; }  
        public int Turns { get; set; }
    }
}
