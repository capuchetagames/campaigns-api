using Core.Entity;
using FluentValidation;

namespace CampaignsApi.Service.Validator;

public class CampaignUpdateValidator : CampaignInputBaseValidator<CampaignUpdateInput>
{
    public CampaignUpdateValidator():base()
    {
        RuleFor(c=>c.Status).IsInEnum().WithMessage("Status Invalid");
    }
    
}