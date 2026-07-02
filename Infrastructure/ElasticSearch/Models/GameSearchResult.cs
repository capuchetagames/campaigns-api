using Core.Entity;

namespace Core.Models.ElasticSearch;

public class GameSearchResult
{
    public Game Game { get; set; } = null!;
    public double Score { get; set; }
}