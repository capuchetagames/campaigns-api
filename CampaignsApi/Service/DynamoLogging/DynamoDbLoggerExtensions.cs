using Amazon.DynamoDBv2;

namespace CampaignsApi.Service.DynamoLogging;

public static class DynamoDbLoggerExtensions
{
    public static ILoggingBuilder AddDynamoDbLogger(this ILoggingBuilder builder, string tableName, LogLevel minLevel = LogLevel.Warning)
    {
        builder.Services.AddSingleton<ILoggerProvider>(sp =>
        {
            var client = sp.GetRequiredService<IAmazonDynamoDB>();
            return new DynamoDbLoggerProvider(client, tableName, minLevel);
        });
        
        return builder;
    }
}