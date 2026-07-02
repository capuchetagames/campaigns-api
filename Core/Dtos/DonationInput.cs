namespace Core.Dtos;

public class DonationInput
{
    public Guid UserId { get; set; }
    public Guid CampaignId { get; set; }
    public decimal Amount { get; set; }
}