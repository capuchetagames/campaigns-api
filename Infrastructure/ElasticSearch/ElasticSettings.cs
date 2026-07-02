using Core.Models.ElasticSearch;

namespace Infrastructure.ElasticSearch;

public class ElasticSettings : IElasticSettings
{
    public bool UseCloud { get; set; }
    public string ApiKey { get; set; }
    public string CloudId { get; set; }
    public string LocalUrl { get; set; }
}