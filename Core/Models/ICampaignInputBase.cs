namespace Core.Models;

public interface ICampaignInputBase
{
    string Title { get; set; }
    string Description { get; set; }
    DateTime StartDate { get; set; }
    DateTime EndDate { get; set; }
    decimal FinancialGoal { get; set; }
}