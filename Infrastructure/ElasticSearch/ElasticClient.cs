using Core.Models.ElasticSearch;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;

namespace Infrastructure.ElasticSearch;

public class ElasticClient<T> : IElasticClient<T>
{
    private readonly ElasticsearchClient _client;

    public ElasticClient(IElasticSettings settings)
    {
        _client = settings.UseCloud ?
            new ElasticsearchClient(settings.CloudId, new ApiKey(settings.ApiKey)) :
            new ElasticsearchClient(new Uri(settings.LocalUrl));
    }
    
    public async Task<IReadOnlyCollection<T>> GetAsync(int page, int pageSize, IndexName indexName)
    {
        var response = await _client.SearchAsync<T>(s => s
            .Index(indexName)
            .From(page)
            .Size(pageSize)); 
        
        return response.Documents;
    }

    public async Task<bool> IndexAsync(T item, IndexName indexName)
    {
        var response = await _client.IndexAsync<T>(item, i => i.Index(indexName));
        
        return response.IsValidResponse;
    }

    public async Task<IReadOnlyCollection<T>> SearchAsync(IndexName indexName, string query, string? category = null)
    {
        var response = await _client.SearchAsync<T>(s => s
            .Index(indexName.ToString())
            .Query(q => q
                .Bool(b =>
                {
                    b.Must(m => m
                        .MultiMatch(mm => mm
                            .Query(query)
                            .Fields(new[] { "name^3", "category"})
                            .Fuzziness(new Fuzziness("AUTO"))
                            .FuzzyTranspositions(true)
                            .MinimumShouldMatch("70%")
                        )
                    );

                    if (category is not null)
                        b.Filter(f => f.Term(t => t.Field("category.keyword").Value(category)));
                })
            )
            .Sort(s => s.Score(sc => sc.Order(SortOrder.Desc)))
            .Size(20)
        );
        
        return response.Documents;
    }

    public async Task DeleteAsync(int id, IndexName indexName)
    {
        await _client.DeleteAsync(indexName,id.ToString());
    }

    public async Task ReindexAllAsync(List<T> items)
    {
        // Limpa o índice atual
        await _client.Indices.DeleteAsync(typeof(IndexName));
        await _client.Indices.CreateAsync(typeof(IndexName), c => c
            .Mappings(new TypeMapping
            {
                Properties = new Properties
                {
                    { "name",        new TextProperty() },
                    { "category",    new KeywordProperty() },
                    { "active",      new BooleanProperty() },
                    { "price",       new ScaledFloatNumberProperty { ScalingFactor = 100 } },
                    { "createdAt",   new DateProperty() }
                }
            })
        );

        // Bulk indexing
        var bulk = await _client.BulkAsync(b => b
            .Index(typeof(IndexName))
            .IndexMany(items)
        );

        if (bulk.Errors)
            throw new Exception($"Reindex falhou: {bulk.ItemsWithErrors.Count()} erros");
    }
}