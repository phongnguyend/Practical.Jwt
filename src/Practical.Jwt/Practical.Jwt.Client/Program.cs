using Practical.Jwt.Client.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Practical.Jwt.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var httpService = new HttpService(new HttpClient());

            var tokenResponse = await httpService.GetToken("https://localhost:44352/api/users/login", new LoginRequest
            {
                UserName = "test@abc.com"
            });

            var users = await httpService.GetAsync<List<UserModel>>(url: "https://localhost:44352/api/users", accessToken: tokenResponse["accessToken"]);

            tokenResponse = await httpService.RefreshToken("https://localhost:44352/api/users/refreshToken", new RefreshTokenRequest
            {
                RefreshToken = tokenResponse["refreshToken"]
            });

            var user = await httpService.PostAsync<UserModel>(url: "https://localhost:44352/api/users",
                data: new UserModel { Id = "3" },
                accessToken: tokenResponse["accessToken"]);

            user = await httpService.PutAsync<UserModel>(url: $"https://localhost:44352/api/users/{user.Id}",
                data: new UserModel { },
                accessToken: tokenResponse["accessToken"]);

            await httpService.DeleteAsync(url: $"https://localhost:44352/api/users/{user.Id}", accessToken: tokenResponse["accessToken"]);
        }
    }
}
