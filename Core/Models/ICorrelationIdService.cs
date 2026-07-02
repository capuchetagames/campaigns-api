namespace Core.Models;

public interface ICorrelationIdService
{
    string Get();
    void Set(string correlationId);
}