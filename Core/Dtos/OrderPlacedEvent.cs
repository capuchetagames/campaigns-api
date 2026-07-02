namespace Core.Dtos;

public record OrderPlacedEvent(
    int UserId,
    string Email,
    string Name,
    int GameId,
    decimal Price
    );