using Core.Enums;
using Core.Models;

namespace Core.Entity;

/// <summary>
/// Representa uma Campanha.
/// </summary>
public class CampaignUpdateInput :ICampaignInputBase
{
    public Guid Id { get; set; }
    
    public required string Title { get; set; }
    
    public required string Description { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public decimal FinancialGoal { get; set; }
    
    public Status Status { get; set; }
}