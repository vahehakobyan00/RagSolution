using Rag.Service.Dtos;
using Rag.Service.Enums;
using Rag.Service.Factory.Interfaces;

namespace Rag.Service.Factory
{
    public class LlmProviderFactory : ILlmProviderFactory
    {
        public ILlmProvider Create(ServiceAgentConfigDto agent)
        {
            return agent.LlmProviderId switch
            {
                (int)LlmProviderEnum.OpenAI => new OpenAiLlmProvider(agent.APIToken, agent.ModelName),
                // (int)LlmProviderEnum.OpenAI => new AzureOpenAiLlmClient(...),
                // (int)LlmProviderEnum.Deepseek => new DeepSeekLlmClient(...),
                _ => throw new NotSupportedException($"LLM Provider ID {agent.LlmProviderId} not supported")
            };
        }
    }
}
