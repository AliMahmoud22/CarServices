using System.Text.RegularExpressions;
using FluentValidation;

namespace CarServices.Application.Bookings;

public partial class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    // Egyptian mobile number: 010, 011, 012, 015 followed by 8 digits (optionally prefixed with +20 or 0020)
    [GeneratedRegex(@"^(?:\+?20|0020)?0?1[0125]\d{8}$")]
    private static partial Regex EgyptianMobileRegex();

    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.ModelId)
            .GreaterThan(0)
            .WithMessage("يجب اختيار موديل السيارة.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty()
            .WithMessage("يجب اختيار خدمة واحدة على الأقل.");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("يجب اختيار تاريخ الحجز.");

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("يجب إدخال اسم العميل.")
            .MinimumLength(2)
            .WithMessage("اسم العميل يجب أن يكون حرفين على الأقل.")
            .MaximumLength(100)
            .WithMessage("اسم العميل لا يجب أن يتجاوز 100 حرف.");

        RuleFor(x => x.CustomerPhone)
            .NotEmpty()
            .WithMessage("يجب إدخال رقم الهاتف.")
            .Must(phone => !string.IsNullOrWhiteSpace(phone) && EgyptianMobileRegex().IsMatch(phone.Trim()))
            .WithMessage("رقم الهاتف غير صحيح. يرجى إدخال رقم موبايل مصري صحيح (مثال: 01012345678).");

        RuleFor(x => x.CustomerEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail))
            .WithMessage("البريد الإلكتروني غير صحيح.");

        RuleFor(x => x.ModelYear)
            .InclusiveBetween(1980, 2030)
            .When(x => x.ModelYear.HasValue)
            .WithMessage("سنة الصنع غير صحيحة.");
    }
}

