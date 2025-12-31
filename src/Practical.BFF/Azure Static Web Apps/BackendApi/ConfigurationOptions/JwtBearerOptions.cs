namespace BackendApi.ConfigurationOptions;

public class JwtBearerOptions
{
    public string Authority { get; set; }

    public string Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; }
}
