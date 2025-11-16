namespace TutorLinkBe.Infrastructure.Config;

public class AppSettings
{
    public MongoDbSettings MongoDb { get; init; } = new();
    public string[] CorsOrigins { get; init; } = [];
    public JwtSettings Jwt { get; init; } = new();
}

// MongoDB configuration settings
public sealed class MongoDbSettings
{
    public string ConnectionString { get; init; }=string.Empty;
}
// JWT configuration settings
public sealed class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
}