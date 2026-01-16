using AutoMapper;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Models.Entities;

namespace Hesapix.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User Mappings
        CreateMap<User, UserDto>();

        // Subscription Mappings
        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(dest => dest.DaysRemaining,
                opt => opt.MapFrom(src => (src.EndDate - DateTime.UtcNow).Days))
            .ForMember(dest => dest.IsActive,
                opt => opt.MapFrom(src => (src.Status == Models.Enums.SubscriptionStatus.Active
                    || src.Status == Models.Enums.SubscriptionStatus.Trial)
                    && src.EndDate > DateTime.UtcNow))
            .ForMember(dest => dest.StatusMessage,
                opt => opt.MapFrom(src =>
                    src.WillCancelAtPeriodEnd
                        ? $"Aboneliğiniz {src.EndDate:dd.MM.yyyy} tarihinde sona erecek ve yenilenmeyecek"
                        : src.Status == Models.Enums.SubscriptionStatus.Active
                            ? $"Aktif - {src.EndDate:dd.MM.yyyy} tarihinde yenilenecek"
                            : src.Status == Models.Enums.SubscriptionStatus.Trial
                                ? $"Deneme sürümü - {src.EndDate:dd.MM.yyyy} tarihinde sona erecek"
                                : "Aboneliğiniz sona ermiş"));

        // Sale Mappings
        CreateMap<Sale, SaleDto>();
        CreateMap<SaleItem, SaleItemDto>();

        // Payment Mappings
        CreateMap<Payment, PaymentDto>();
        CreateMap<CreatePaymentRequest, Payment>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        // Stock Mappings
        CreateMap<Stok, StockDto>();
        CreateMap<CreateStockRequest, Stok>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        CreateMap<UpdateStockRequest, Stok>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}