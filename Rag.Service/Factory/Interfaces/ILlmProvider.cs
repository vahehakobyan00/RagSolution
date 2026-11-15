using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Factory.Interfaces
{
    public interface ILlmProvider
    {
        IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);
        Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);
    }
}
