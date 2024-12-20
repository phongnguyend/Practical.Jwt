using Practical.Jwt.Client.Extensions;
using Practical.Jwt.Client.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Practical.Jwt.Client;

public class HttpService
{
    protected readonly HttpClient _httpClient;

    public HttpService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Dictionary<string, string>> GetTokenAsync(string tokenEndpoint, TokenRequestModel request)
    {
        var tokenResponse = await RequestTokenAsync<Dictionary<string, string>>(tokenEndpoint, request);
        return tokenResponse;
    }

    public async Task<Dictionary<string, string>> RefreshTokenAsync(string tokenEndpoint, TokenRequestModel request)
    {
        var tokenResponse = await RequestTokenAsync<Dictionary<string, string>>(tokenEndpoint, request);
        return tokenResponse;
    }

    public async Task<T> RequestTokenAsync<T>(string tokenEndpoint, TokenRequestModel tokenRequest)
    {
        if (tokenRequest.GrantType == "client_credentials")
        {
            _httpClient.UseBasicAuthentication(tokenRequest.ClientId, tokenRequest.ClientSecret);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", tokenRequest.GrantType),
            new KeyValuePair<string, string>("username", tokenRequest.UserName),
            new KeyValuePair<string, string>("password", tokenRequest.Password),
            new KeyValuePair<string, string>("client_id", tokenRequest.ClientId),
            new KeyValuePair<string, string>("client_secret", tokenRequest.ClientSecret),
            new KeyValuePair<string, string>("refresh_token", tokenRequest.RefreshToken),
            new KeyValuePair<string, string>("scope", tokenRequest.Scope)
        });

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var createdObject = await response.Content.ReadAs<T>();
        return createdObject;
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
