using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace CampaignsApi.Service.DynamoLogging;

public static class DynamoDbExtensions
{
    /// <summary>
    /// Garante que a tabela de logs exista (usado com DynamoDB local).
    /// Best-effort: falhas não impedem o startup da aplicação.
    /// </summary>
    public static async Task EnsureLogTableExistsAsync(IAmazonDynamoDB client, string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName)) return;

        try
        {
            var tables = await client.ListTablesAsync();
            if (tables.TableNames.Contains(tableName)) return;

            await client.CreateTableAsync(new CreateTableRequest
            {
                TableName = tableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new("Id", ScalarAttributeType.S)
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new("Id", KeyType.HASH)
                }
            });

            Console.WriteLine($"[DynamoDB] Tabela de logs '{tableName}' criada.");
        }
        catch (ResourceInUseException)
        {
            // tabela criada em paralelo por outra réplica — ok
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DynamoDB] Não foi possível garantir a tabela '{tableName}': {ex.Message}");
        }
    }

    public static IServiceCollection AddDynamoDb(this IServiceCollection services, IConfiguration configuration)
    {
        var useLocal       = configuration.GetValue<bool>("DynamoDb:UseLocal");
        var localUrl = configuration["DynamoDb:LocalUrl"];
        var profile  = configuration["DynamoDb:ProfileName"];
        var region   = configuration["AWS_DEFAULT_REGION"];
        
        services.AddSingleton<IAmazonDynamoDB>(CreateDynamoDbClient(useLocal, localUrl, profile, region));
        
        return services;
    }
    
    private static IAmazonDynamoDB CreateDynamoDbClient(bool useLocal, string? localUrl, string? profile, string? region)
    {
        if (useLocal)
        {
            return new AmazonDynamoDBClient(new BasicAWSCredentials("fake", "fake"), new AmazonDynamoDBConfig { ServiceURL = localUrl });
        }

        //local false - connecting to aws with tokens
        if (string.IsNullOrWhiteSpace(profile)) return new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region));
        
        
        //play rider - connecting to aws with profile
        var credentials = ResolveProfileCredentials(profile);
        return new AmazonDynamoDBClient(credentials, RegionEndpoint.GetBySystemName(region));
    }

    private static AWSCredentials ResolveProfileCredentials(string? profileName)
    {
        var chain = new CredentialProfileStoreChain();
        if (chain.TryGetAWSCredentials(profileName, out var credentials))
        {
            return credentials;
        }

        throw new InvalidOperationException(
            $"Profile '{profileName}' não encontrado em ~/.aws/credentials ou ~/.aws/config.\n" +
            $"Execute: aws configure --profile {profileName}");
    }
}