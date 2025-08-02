using Practical.Jwt.Client;
using Practical.Jwt.Client.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using UriHelper;

var httpService = new HttpService(new HttpClient());

var baseUrl = "https://localhost:44352/";

var tokenResponse = await httpService.GetTokenAsync(UriPath.Combine(baseUrl, "/connect/token"), new TokenRequestModel
{
    GrantType = "password",
    UserName = "test@abc.com",
    Password = "xxx",
});

var users = await httpService.GetAsync<List<UserModel>>(url: UriPath.Combine(baseUrl, "/api/users"), accessToken: tokenResponse["access_token"]);

tokenResponse = await httpService.RefreshTokenAsync(UriPath.Combine(baseUrl, "/connect/token"), new TokenRequestModel
{
    GrantType = "refresh_token",
    RefreshToken = tokenResponse["refresh_token"]
});

tokenResponse = await httpService.RefreshTokenAsync(UriPath.Combine(baseUrl, "/connect/token"), new TokenRequestModel
{
    GrantType = "refresh_token",
    RefreshToken = tokenResponse["refresh_token"]
});

var user = await httpService.PostAsync<UserModel>(url: UriPath.Combine(baseUrl, "/api/users"),
    data: new UserModel { Id = "3" },
    accessToken: tokenResponse["access_token"]);

user = await httpService.PutAsync<UserModel>(url: UriPath.Combine(baseUrl, $"/api/users/{user.Id}"),
    data: new UserModel { },
    accessToken: tokenResponse["access_token"]);

await httpService.DeleteAsync(url: UriPath.Combine(baseUrl, $"/api/users/{user.Id}"), accessToken: tokenResponse["access_token"]);



tokenResponse = await httpService.GetTokenAsync(UriPath.Combine(baseUrl, "/connect/token"), new TokenRequestModel
{
    GrantType = "client_credentials",
    ClientId = "myclient",
    ClientSecret = "myclientsecret"
});

users = await httpService.GetAsync<List<UserModel>>(url: UriPath.Combine(baseUrl, "/api/users"), accessToken: tokenResponse["access_token"]);

Console.ReadLine();
