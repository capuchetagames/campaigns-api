using Core.Enums;

namespace Core.Dtos;

public record NewCampaignEvent(string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal FinancialGoal,
    Status Status);
    