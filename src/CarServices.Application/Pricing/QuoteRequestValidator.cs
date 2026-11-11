using FluentValidation;

namespace CarServices.Application.Pricing;

public class QuoteRequestValidator : AbstractValidator<QuoteRequest>
{
    public QuoteRequestValidator()
    {
        RuleFor(x => x.ModelId)
            .GreaterThan(0)
            .WithMessage("يجب اختيار موديل السيارة.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty()
            .WithMessage("يجب اختيار خدمة واحدة على الأقل.");
    }
}

