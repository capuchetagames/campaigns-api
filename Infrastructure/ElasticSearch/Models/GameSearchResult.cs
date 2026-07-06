using Core.Entity;

namespace Infrastructure.ElasticSearch.Models;

public class GameSearchResult
{
    public Campaign Game { get; set; } = null!;
    public double Score { get; set; }
}