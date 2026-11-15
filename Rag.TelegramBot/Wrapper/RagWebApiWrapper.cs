using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rag.TelegramBot.Wrapper.Interface;
using Rag.TelegramBot.Wrapper.Models.Models;
using Rag.TelegramBot.Wrapper.Models.Requests;
using Rag.TelegramBot.Wrapper.Models.Response;
using RestSharp;
using Serilog;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Rag.TelegramBot.Wrapper
{
    public class RagWebApiWrapper : IRagWebApiWrapper
    {
        private readonly RestClient _client;
        public RagWebApiWrapper(IOptions<RagWebApiSettings> options, IConfiguration configuration)
        {
            _client = new RestClient(options.Value.BaseUrl);
        }

        public async Task<TelegramPromptResponse> GetAnswerAsync(TelegramQueryRequest queryRequest)
        {
            var request = new RestRequest("Query/TelegramPrompt", Method.Post);
            request.AddJsonBody(queryRequest);
            request.AddHeader("X-User-Id", queryRequest.UserId);
            try
            {
                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    Log.Error("Failed to get answer. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, response.Content);

                    return new TelegramPromptResponse
                    {
                        Answer = $"⚠️ API Error: {response.StatusDescription}",
                        Matches = Array.Empty<string>(),
                        Turns = 0
                    };
                }

                if (string.IsNullOrWhiteSpace(response.Content))
                {
                    Log.Warning("Empty response content from API for TelegramQueryRequest: {@QueryRequest}", queryRequest);
                    return new TelegramPromptResponse { Answer = "⚠️ Empty response from API." };
                }

                var result = JsonConvert.DeserializeObject<TelegramPromptResponse>(response.Content);

                if (result == null)
                {
                    Log.Warning("Deserialized result is null for TelegramQueryRequest: {@QueryRequest}", queryRequest);
                    return new TelegramPromptResponse { Answer = "⚠️ Invalid response from API." };
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while getting answer from ragWebApi");
                return new TelegramPromptResponse { Answer = "⚠️ Exception occurred while processing your request." };
            }
        }


    }
}
