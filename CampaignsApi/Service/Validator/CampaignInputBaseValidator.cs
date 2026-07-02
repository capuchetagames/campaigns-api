using Core.Models;
using FluentValidation;

namespace CampaignsApi.Service.Validator;

public abstract class CampaignInputBaseValidator<T> : AbstractValidator<T> where T : ICampaignInputBase
{
    protected CampaignInputBaseValidator()
    {
        RuleFor(c => c.Title).NotEmpty().WithMessage("Campaign Title is required");
        RuleFor(c => c.Description).NotEmpty().WithMessage("Campaign Description is required");
        RuleFor(c => c.FinancialGoal).NotEmpty().WithMessage("Campaign Financial Goal is required");
        RuleFor(c => c.FinancialGoal).GreaterThan(0).WithMessage("Campaign Financial Goal must be greater than 0");
        RuleFor(c => c.StartDate).NotEmpty().WithMessage("Campaign Start Date is required");
        RuleFor(c => c.EndDate).NotEmpty().WithMessage("Campaign End Date is required");
        RuleFor(c => c.StartDate).LessThan(c => c.EndDate).WithMessage("Campaign Start Date must be before End Date");
        RuleFor(c => c.EndDate).GreaterThan(c => c.StartDate).WithMessage("Campaign End Date must be after Start Date");
        RuleFor(c => c.EndDate).GreaterThan(DateTime.UtcNow).WithMessage("Campaign End Date must be in the future");
    }
}