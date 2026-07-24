using Core.Dtos;
using Core.Enums;
using Core.Repository;
using FluentValidation;

namespace CampaignsApi.Service.Validator;

public class DonationInputValidator : AbstractValidator<DonationInput>
{
    public DonationInputValidator(ICampaignRepository campaignRepository)
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("O valor da doação deve ser maior que zero.");

        RuleFor(x => x.CampaignId)
            .Must(campaignId =>
            {
                var campaign = campaignRepository.GetById(campaignId);
                return campaign is {Status: Status.Active, Ended: false};
            })
            .WithMessage("Doações são permitidas apenas para campanhas ativas.");
    }
}
