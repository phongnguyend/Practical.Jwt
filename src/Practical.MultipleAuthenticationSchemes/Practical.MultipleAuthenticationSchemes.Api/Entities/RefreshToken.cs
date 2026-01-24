using System;

namespace Practical.MultipleAuthenticationSchemes.Api.Entities;

public class RefreshToken
{
    public string UserName { get; set; }

    public DateTimeOffset Expiration { get; set; }

    public DateTimeOffset? ConsumedTime { get; set; }
}
