namespace CampaignsApi.Service.DynamoLogging;

public class DynamoLogModel
{
    public string Id          { get; set; } = Guid.NewGuid().ToString();
    public string Timestamp   { get; set; } = DateTime.UtcNow.ToString("o");
    public string Level       { get; set; } = string.Empty;
    public string Category    { get; set; } = string.Empty;
    public string Message     { get; set; } = string.Empty;
    public string? Exception  { get; set; }
    public string? CorrelationId { get; set; }
    public string Environment { get; set; } = 
        System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
}