using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TutorLinkBe.Infrastructure.Config;
using TutorLinkBe.Domain.Entities;

namespace TutorLinkBe.Application.Services;

// Provides access to MongoDB and exposes a connectivity check
public sealed class MongoDbService
{
    private readonly ILogger<MongoDbService> _logger;
    private readonly MongoClient _client;
    private readonly IMongoDatabase? _database;
    private readonly IConfiguration _configuration;
    
    // Initializes a new instance of the <see cref="MongoDbService"/> class.
    public MongoDbService( ILogger<MongoDbService> logger,IConfiguration configuration )
    {
        _logger = logger;

        var settings = configuration.GetSection( "ConnectionStrings" );
        
        var connectionString = settings["MongoDbConnection"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("MongoDb.ConnectionString is not configured in AppSettings.");
            connectionString= string.Empty;
        }
        
        _client = new MongoClient(connectionString);

        try
        {
            var mongoUrl = MongoUrl.Create(connectionString);
            var databaseName = string.IsNullOrWhiteSpace(mongoUrl.DatabaseName) ? "tutorlink" : mongoUrl.DatabaseName;
            _database = _client.GetDatabase(databaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse MongoDB connection string for database selection.");
        }
    }
    
    //check the MongoDb connection by attempting to list database
    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var session = await _client.ListDatabaseNamesAsync(cancellationToken);
            _ = await session.ToListAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Failed to connect to MongoDB or list databases.");
            return false;
        }
    }
    
}