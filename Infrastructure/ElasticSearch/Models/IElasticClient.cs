using Elastic.Clients.Elasticsearch;

namespace Core.Models.ElasticSearch;

public interface IElasticClient<T>
{
    Task<IReadOnlyCollection<T>> GetAsync(int page, int pageSize, IndexName indexName);
    
    Task<bool> IndexAsync(T item, IndexName indexName);

    Task<IReadOnlyCollection<T>> SearchAsync(IndexName indexName, string query, string? category = null);
    Task DeleteAsync(int id, IndexName indexName);
    
    Task ReindexAllAsync(List<T> items);
}