using Core.Models;

namespace CampaignsApi.Service;

public class BaseLogger<T> :  IBaseLogger<T>
{
    private readonly ILogger<T> _logger;
    private ICorrelationIdService _correlationIdService;
    
    public BaseLogger(ILogger<T> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    public virtual void LogInformation(string message)
    {
        _logger.LogInformation($"[CorrelationId: {_correlationIdService.Get()}] {message}");
    }

    public virtual void LogError(string message)
    {
        _logger.LogError($"[CorrelationId: {_correlationIdService.Get()}] {message}");
    }

    public virtual void LogWarning(string message)
    {
        _logger.LogWarning($"[CorrelationId: {_correlationIdService.Get()}] {message}");
    }
}