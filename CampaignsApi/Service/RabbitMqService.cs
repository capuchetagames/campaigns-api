using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CampaignsApi.Config;
using Core.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CatalogApi.Service;

public class RabbitMqService : IRabbitMqService
{
    private readonly ILogger<RabbitMqService>? _logger;
    
    private IConnection? _connection;
    private IChannel? _publishChannel;

    private readonly ConcurrentBag<IChannel> _consumerChannels = new();
    private RabbitMqService() { }
    
    public static async Task<RabbitMqService> CreateAsync(RabbitMqSettings settings, ILogger<RabbitMqService> logger, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? settings.Host,
            UserName = settings.User,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        int[] retryDelays = [5, 10, 20, 30];
        IConnection? connection = null;

        foreach (var delay in retryDelays)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"[RabbitMQ] Falha ao conectar, tentando em {delay}s...({ex.Message})", delay);
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }
        }

        if (connection is null || !connection.IsOpen)
        {
            logger.LogError("[RabbitMQ] Não foi possível conectar após todas as tentativas.");
            throw new InvalidOperationException("[RabbitMQ] Não foi possível conectar após todas as tentativas.");
        }
            
        
        var instance = new RabbitMqService();
        instance._connection = connection;
        instance._publishChannel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        Console.WriteLine($"[RabbitMQ] Conectado com sucesso! ({connection.Endpoint.HostName}:{connection.Endpoint.Port})");
        
        return instance;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken)
    {
        await _publishChannel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _publishChannel.BasicPublishAsync(exchange, routingKey, body, cancellationToken: cancellationToken);
    }

    public async Task ConsumeAsync<T>(string exchange, string queue, string routingKey, Func<T, Task> handler, CancellationToken cancellationToken)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken:cancellationToken);
        _consumerChannels.Add(channel);

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, true, cancellationToken:cancellationToken);
        await channel.QueueDeclareAsync(queue, true, false, false, null, cancellationToken:cancellationToken);
        await channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken:cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = JsonSerializer.Deserialize<T>(ea.Body.Span);
            await handler(message!);

            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
        };

        await channel.BasicConsumeAsync(queue, false, consumer, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var channel in _consumerChannels)
            await channel.CloseAsync();

        await _publishChannel.CloseAsync();
        await _connection.CloseAsync();
    }
}
