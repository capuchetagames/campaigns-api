namespace Core.Dtos;

public record DonationReceivedEvent(
    Guid CampaignId,
    decimal Amount);