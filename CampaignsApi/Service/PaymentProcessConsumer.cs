using Core.Dtos;
using Core.Entity;
using Core.Models;
using Core.Repository;

namespace CatalogApi.Service;

public class PaymentProcessConsumer : BackgroundService
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentProcessConsumer(IRabbitMqService rabbitMqService, IServiceScopeFactory scopeFactory)
    {
        _rabbitMqService = rabbitMqService;
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _rabbitMqService.ConsumeAsync<PaymentProcessedEvent>(
            exchange: "payments.events",
            queue: "payments.process",
            routingKey: "payment.*",
            handler: Handle,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private async Task Handle(PaymentProcessedEvent paymentProcessed)
    {
        Console.WriteLine($" Compra Aprovada Adiciona o Game na Lib do Jogador  : {paymentProcessed.Name} | {paymentProcessed.UserId} | {paymentProcessed.Status}| {paymentProcessed.GameId} ");
        
        using var scope = _scopeFactory.CreateScope();

        var libraryGames = scope.ServiceProvider
            .GetRequiredService<IPlayerLibraryGames>();
        
        libraryGames.Add(new PlayerLibraryGames()
        {
            UserId = paymentProcessed.UserId,
            GameId = paymentProcessed.GameId
            
        });
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _rabbitMqService.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}