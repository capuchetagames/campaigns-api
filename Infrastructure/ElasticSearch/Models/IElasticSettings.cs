namespace Core.Models.ElasticSearch;

public interface IElasticSettings
{
    bool UseCloud { get; set; }
    string ApiKey { get; set; }
    string CloudId { get; set; }
    
    string LocalUrl { get; set; }
}