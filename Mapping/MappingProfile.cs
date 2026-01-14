using AutoMapper;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Auth;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Models.Entities;

namespace Hesapix.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User Mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => src.EmailVerified));

            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => false));

            // Sale Mappings
            CreateMap<Sale, SaleDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.SaleItems));

            CreateMap<SaleItem, SaleItemDto>();

            CreateMap<CreateSaleRequest, Sale>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.SaleItems, opt => opt.Ignore());

            // Stock Mappings
            CreateMap<Stock, StockDto>();

            CreateMap<StockDto, Stock>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            // Payment Mappings
            CreateMap<Payment, PaymentDto>();

            CreateMap<CreatePaymentRequest, Payment>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}