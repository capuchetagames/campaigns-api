namespace Core.Models;

public interface IRabbitMqService: IAsyncDisposable
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken);
    
    Task ConsumeAsync<T>(string exchange, string queue, string routingKey, Func<T, Task> handler, CancellationToken cancellationToken);
}