using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace CampaignsApi.Service.DynamoLogging;

public class DynamoDbLogger : ILogger
{
    private readonly string _category;
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;
    private readonly LogLevel _minLevel;
    private IExternalScopeProvider? _scopeProvider;

    public DynamoDbLogger(string category, IAmazonDynamoDB client, string tableName, LogLevel minLevel, IExternalScopeProvider? scopeProvider = null)
    {
        _category  = category;
        _client    = client;
        _tableName = tableName;
        _minLevel  = minLevel;
        _scopeProvider = scopeProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel && logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        
        var correlationId = GetCorrelationId();
        
        var entry = new DynamoLogModel
        {
            Level     = logLevel.ToString(),
            Category  = _category,
            Message   = formatter(state, exception),
            Exception = exception?.ToString(),
            CorrelationId = correlationId
        };
        
        _ = Task.Run(() => PersistAsync(entry));
    }
    private string? GetCorrelationId()
    {
        string? correlationId = null;

        _scopeProvider?.ForEachScope((scope, _) =>
        {
            if (scope is not IEnumerable<KeyValuePair<string, object>> props) return;
            
            foreach (var prop in props)
            {
                if (prop.Key == "CorrelationId") correlationId = prop.Value?.ToString();
            }

        }, (object?)null);

        return correlationId;
    }
    
    
    private void AddIfNotEmpty(Dictionary<string, AttributeValue> item, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            item[key] = new AttributeValue { S = value };
        }
    }

    private async Task PersistAsync(DynamoLogModel entry)
    {
        try
        {
            var item = new Dictionary<string, AttributeValue>();

            AddIfNotEmpty(item,"Id",            entry.Id);
            AddIfNotEmpty(item,"CorrelationId", entry.CorrelationId);
            AddIfNotEmpty(item,"Environment",   entry.Environment);
            AddIfNotEmpty(item,"Level",         entry.Level);
            AddIfNotEmpty(item,"Category",      entry.Category);
            AddIfNotEmpty(item,"Message",       entry.Message);
            AddIfNotEmpty(item,"Exception",     entry.Exception);
            AddIfNotEmpty(item,"Timestamp",     entry.Timestamp);
            

            await _client.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item      = item
            });
        }
        catch (Exception ex)
        {
            // Silencia erros do logger para não causar loop infinito
            Console.WriteLine($">>> ERRO ao gravar log no DynamoDB: {ex.Message}");
        }
    }

    // Scope vazio
    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}