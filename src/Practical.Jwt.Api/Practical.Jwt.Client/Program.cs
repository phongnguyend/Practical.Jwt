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

            var users = await httpService.GetAsync<List<string>>("https://localhost:44352/api/users", tokenResponse["accessToken"]);

            tokenResponse = await httpService.RefreshToken("https://localhost:44352/api/users/refreshToken", new RefreshTokenRequest
            {
                UserName = "test@abc.com",
                RefreshToken = tokenResponse["refreshToken"]
            });
        }
    }
}
