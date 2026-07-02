using Core.Models;

namespace Core.Dtos;

public class CampaignInput : ICampaignInputBase
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal FinancialGoal { get; set; }
}