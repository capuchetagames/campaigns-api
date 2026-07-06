namespace Core.Dtos;

public class DonationInput
{
    public Guid CampaignId { get; set; }
    public decimal Amount { get; set; }
}