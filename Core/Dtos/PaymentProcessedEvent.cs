namespace Core.Dtos;

public record PaymentProcessedEvent(
    int UserId,
    int GameId,
    string Email,
    string Name,
    string Status
    );