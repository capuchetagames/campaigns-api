using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace CampaignsApi.Service.DynamoLogging;

public static class DynamoDbExtensions
{
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