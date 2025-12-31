using System.Text.RegularExpressions;

namespace ReverseProxy.Yarp.Middleware;

public partial class ResponseCookieLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCookieLoggingMiddleware> _logger;

    [GeneratedRegex(@";\s*expires=Thu,\s*01\s+Jan\s+1970\s+00:00:00\s+GMT", RegexOptions.IgnoreCase)]
    private static partial Regex ExpiresEpochPattern();

    public ResponseCookieLoggingMiddleware(RequestDelegate next, ILogger<ResponseCookieLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        LogResponseCookies(context);
    }

    private void LogResponseCookies(HttpContext context)
    {
        if (context.Response.Headers.TryGetValue("Set-Cookie", out var setCookieHeaders))
        {
            _logger.LogInformation("Response Cookies for {Path}:", context.Request.Path);

            // Transform cookies to replace expires=Thu, 01 Jan 1970 00:00:00 GMT with max-age=0
            var transformedCookies = new List<string>();
            bool hasTransformed = false;

            foreach (var cookie in setCookieHeaders)
            {
                _logger.LogInformation("  Cookie: {Cookie}", cookie);

                if (ExpiresEpochPattern().IsMatch(cookie))
                {
                    hasTransformed = true;
                    var transformedCookie = TransformCookieExpiration(cookie);
                    transformedCookies.Add(transformedCookie);
                    _logger.LogInformation("  Transformed Cookie: {Cookie}", transformedCookie);
                }
                else
                {
                    transformedCookies.Add(cookie);
                }
            }

            if (hasTransformed)
            {
                context.Response.Headers["Set-Cookie"] = transformedCookies.ToArray();
            }
        }
        else
        {
            _logger.LogDebug("No cookies in response for {Path}", context.Request.Path);
        }
    }

    /// <summary>
    /// Workaround for Azure Static Web Apps not handling "expires" attribute contains an expired date correctly.
    /// https://github.com/Azure/static-web-apps/issues/1214
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    private static string TransformCookieExpiration(string cookie)
    {
        // Remove the expires clause
        var result = ExpiresEpochPattern().Replace(cookie, "");

        // Check if max-age is already present
        if (!result.Contains("max-age=", StringComparison.OrdinalIgnoreCase))
        {
            // Find the right place to insert max-age=0 (after the cookie value, before other attributes)
            var firstSemicolon = result.IndexOf(';');
            if (firstSemicolon > 0)
            {
                result = result.Insert(firstSemicolon + 1, " max-age=0;");
            }
            else
            {
                result += "; max-age=0";
            }
        }

        return result;
    }
}
