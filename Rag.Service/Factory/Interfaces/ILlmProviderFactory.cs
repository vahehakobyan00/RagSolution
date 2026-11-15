using Rag.Service.Dtos;

namespace Rag.Service.Factory.Interfaces
{
    public interface ILlmProviderFactory
    {
        ILlmProvider Create(ServiceAgentConfigDto agent);
    }
}
