using Core.Enums;

namespace Core.Entity;

/// <summary>
/// Representa uma Campanha.
/// </summary>
public class Campaign : EntityBase
{
    public required string Title { get; set; }
    
    public required string Description { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public decimal FinancialGoal { get; set; }

    public decimal AmountRaised { get; set; }

    public Status Status { get; set; }
}