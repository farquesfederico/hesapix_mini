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
                opt => opt.MapFrom(src => src.Status == Models.Enums.SubscriptionStatus.Active
                    && src.EndDate > DateTime.UtcNow));

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