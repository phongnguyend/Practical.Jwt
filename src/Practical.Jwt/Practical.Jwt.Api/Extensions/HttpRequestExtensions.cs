using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace Practical.Jwt.Api.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool TryGetBearerToken(this HttpRequest httpRequest, out string token)
        {
            string authorization = httpRequest.Headers.ContainsKey("Authorization") ? httpRequest.Headers["Authorization"] : string.Empty;

            if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("Bearer ".Length).Trim();
                return true;
            }

            token = null;
            return false;
        }

        public static bool TryGetBasicCredentials(this HttpRequest httpRequest, out string userName, out string password)
        {
            string authorization = httpRequest.Headers.ContainsKey("Authorization") ? httpRequest.Headers["Authorization"] : string.Empty;

            if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    byte[] data = Convert.FromBase64String(authorization.Substring("Basic ".Length).Trim());
                    string text = Encoding.UTF8.GetString(data);
                    int delimiterIndex = text.IndexOf(':');
                    if (delimiterIndex >= 0)
                    {
                        userName = text.Substring(0, delimiterIndex);
                        password = text.Substring(delimiterIndex + 1);
                        return true;
                    }
                }
                catch (FormatException)
                {
                }
                catch (ArgumentException)
                {
                }
            }

            userName = null;
            password = null;
            return false;
        }
    }
}
