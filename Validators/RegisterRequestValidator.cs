using FluentValidation;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Models.DTOs.Sale;

namespace Hesapix.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email adresi gereklidir")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
                .MaximumLength(255).WithMessage("Email adresi en fazla 255 karakter olabilir");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre gereklidir")
                .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır")
                .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir")
                .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir")
                .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermelidir");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Ad Soyad gereklidir")
                .MaximumLength(200).WithMessage("Ad Soyad en fazla 200 karakter olabilir")
                .MinimumLength(3).WithMessage("Ad Soyad en az 3 karakter olmalıdır");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası gereklidir")
                .Matches(@"^[0-9]{10,11}$").WithMessage("Geçerli bir telefon numarası giriniz (10-11 rakam)");

            RuleFor(x => x.CompanyName)
                .MaximumLength(200).WithMessage("Şirket adı en fazla 200 karakter olabilir")
                .When(x => !string.IsNullOrWhiteSpace(x.CompanyName));

            RuleFor(x => x.TaxNumber)
                .Matches(@"^[0-9]{10,11}$").WithMessage("Vergi numarası 10 veya 11 rakam olmalıdır")
                .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));
        }
    }

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email adresi gereklidir")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre gereklidir");
        }
    }

    public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
    {
        public CreateSaleRequestValidator()
        {
            RuleFor(x => x.SaleDate)
                .NotEmpty().WithMessage("Satış tarihi gereklidir")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Satış tarihi gelecek tarih olamaz");

            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Müşteri adı gereklidir")
                .MaximumLength(200).WithMessage("Müşteri adı en fazla 200 karakter olabilir");

            RuleFor(x => x.CustomerPhone)
                .Matches(@"^[0-9]{10,11}$").WithMessage("Geçerli bir telefon numarası giriniz")
                .When(x => !string.IsNullOrWhiteSpace(x.CustomerPhone));

            RuleFor(x => x.CustomerEmail)
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
                .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("En az bir ürün eklenmelidir")
                .Must(x => x.Count > 0).WithMessage("En az bir ürün eklenmelidir");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.StockId)
                    .GreaterThan(0).WithMessage("Geçerli bir ürün seçiniz");

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır")
                    .LessThan(10000).WithMessage("Miktar 10000'den küçük olmalıdır");

                item.RuleFor(x => x.UnitPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Fiyat negatif olamaz");

                item.RuleFor(x => x.TaxRate)
                    .InclusiveBetween(0, 100).WithMessage("KDV oranı 0-100 arasında olmalıdır");

                item.RuleFor(x => x.DiscountRate)
                    .InclusiveBetween(0, 100).WithMessage("İndirim oranı 0-100 arasında olmalıdır");
            });

            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("İndirim tutarı negatif olamaz");
        }
    }
}