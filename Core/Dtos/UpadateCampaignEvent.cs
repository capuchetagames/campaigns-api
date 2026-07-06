using Core.Enums;

namespace Core.Dtos;

public record UpdateCampaignEvent(
    Guid CampaignId,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal FinancialGoal,
    Status Status);
    