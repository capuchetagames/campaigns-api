using Amazon.DynamoDBv2;

namespace CampaignsApi.Service.DynamoLogging;

[ProviderAlias("DynamoDb")]
public sealed class DynamoDbLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;
    private readonly LogLevel _minLevel;
    
    private IExternalScopeProvider _scopeProvider;

    public DynamoDbLoggerProvider(IAmazonDynamoDB client, string tableName, LogLevel minLevel = LogLevel.Warning)
    {
        _client    = client;
        _tableName = tableName;
        _minLevel  = minLevel;
    }
    
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public ILogger CreateLogger(string categoryName) => new DynamoDbLogger(categoryName, _client, _tableName, _minLevel, _scopeProvider);

    public void Dispose() { }
    
    
}