using FluentValidation;
using Hesapix.Models.DTOs.Auth;

namespace Hesapix.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email gereklidir")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
            .MaximumLength(255).WithMessage("Email en fazla 255 karakter olabilir");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır")
            .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir")
            .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir")
            .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermelidir")
            .Matches(@"[\!\@\#\$\%\^\&\*\(\)\_\+\=\-\[\]\{\}\;\:\'\,\.\<\>\?\/]")
            .WithMessage("Şifre en az bir özel karakter içermelidir");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Şifreler eşleşmiyor");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Firma adı gereklidir")
            .MaximumLength(255).WithMessage("Firma adı en fazla 255 karakter olabilir");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).WithMessage("Vergi numarası en fazla 50 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.TaxNumber));
    }
}