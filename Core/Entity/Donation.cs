namespace Core.Entity;

public class Donation :EntityBase
{
    public Guid UserId { get; set; }
    public Guid CampaignId { get; set; }
    public decimal Amount { get; set; }
}