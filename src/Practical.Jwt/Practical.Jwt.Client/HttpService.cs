using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Practical.Jwt.Client
{
    public class HttpService
    {
        protected readonly HttpClient _httpClient;

        public HttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, string>> GetToken(string tokenEndpoint, LoginRequest request)
        {
            var tokenResponse = await PostAsync<Dictionary<string, string>>(tokenEndpoint, request);
            return tokenResponse;
        }

        public async Task<Dictionary<string, string>> RefreshToken(string tokenEndpoint, RefreshTokenRequest request)
        {
            var tokenResponse = await PostAsync<Dictionary<string, string>>(tokenEndpoint, request);
            return tokenResponse;
        }

        protected Task SetBearerToken(string accessToken)
        {
            if (accessToken != null)
            {
                _httpClient.UseBearerToken(accessToken);
            }

            return Task.CompletedTask;
        }

        public async Task<T> GetAsync<T>(string url, string accessToken = null)
        {
            await SetBearerToken(accessToken);

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAs<T>();
            return data;
        }

        public async Task<T> PostAsync<T>(string url, object data = null, string accessToken = null)
        {
            await SetBearerToken(accessToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (data != null)
            {
                request.Content = data.AsJsonContent();
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var createdObject = await response.Content.ReadAs<T>();
            return createdObject;
        }

        public async Task<T> PutAsync<T>(string url, object data, string accessToken = null)
        {
            await SetBearerToken(accessToken);

            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = data.AsJsonContent();

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var updatedObject = await response.Content.ReadAs<T>();
            return updatedObject;
        }

        public async Task DeleteAsync(string url, string accessToken = null)
        {
            await SetBearerToken(accessToken);

            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        }
    }
}
