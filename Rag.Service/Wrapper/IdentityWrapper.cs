using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rag.Service.Wrapper.Interfaces;
using Rag.Service.Wrapper.Models.Models;
using RestSharp;
using Serilog;

namespace Rag.Service.Wrapper
{
    public class IdentityWrapper : IIdentityWrapper
    {
        private readonly RestClient _client;

        public IdentityWrapper(IOptions<IdentityApiSettings> options)
        {
            _client = new RestClient(options.Value.BaseUrl);

        }
        public async Task<Dictionary<string, string>> GetUserNamesByUserIdsAsync(List<string> userIds)
        {
            var request = new RestRequest("Users/GetUserNamesByIds", Method.Post);
            request.AddJsonBody(userIds);

            try
            {
                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    Log.Error("Failed to fetch usernames. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, response.Content);
                    return new Dictionary<string, string>();
                }

                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                return result ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while fetching usernames for user IDs {@UserIds}", userIds);
                throw; 
            }
        }
    }
}
