using AutoMapper;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Models.DTOs.Subs;
using Hesapix.Models.Entities;

namespace Hesapix.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User Mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Subscription, opt => opt.MapFrom(src => src.Subscription));

            // Subscription Mappings
            CreateMap<Subscription, SubscriptionDTO>()
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive()))
                .ForMember(dest => dest.DaysRemaining, opt => opt.MapFrom(src =>
                    src.EndDate.HasValue ? (int?)(src.EndDate.Value - DateTime.UtcNow).TotalDays : null));

            // Sale Mappings
            CreateMap<Sale, SaleDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.SaleItems));

            CreateMap<SaleItem, SaleItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Stock.ProductName))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Stock.ProductCode));

            // Stock Mappings
            CreateMap<Stok, StockDto>();

            // Payment Mappings
            CreateMap<Payment, PaymentDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Sale != null ? src.Sale.CustomerName : null));
        }
    }
}