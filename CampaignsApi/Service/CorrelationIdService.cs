using Core.Models;

namespace CampaignsApi.Service;

public class CorrelationIdService : ICorrelationIdService
{
    private static string _correlationId;
    
    public string Get() => _correlationId;

    public void Set(string correlationId) => _correlationId = correlationId;
}