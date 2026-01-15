using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetPayments(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<List<PaymentDto>>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _paymentService.GetPaymentsByUserIdAsync(userId, startDate, endDate, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödemeler listelenirken hata");
                return StatusCode(500, ApiResponse<List<PaymentDto>>.FailResult("Ödemeler listelenemedi"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> GetPayment(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<PaymentDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _paymentService.GetPaymentByIdAsync(id, userId);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme bilgisi alınırken hata: PaymentId={PaymentId}", id);
                return StatusCode(500, ApiResponse<PaymentDto>.FailResult("Ödeme bilgisi alınamadı"));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<PaymentDto>.FailResult("Geçersiz veri"));
                }

                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<PaymentDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _paymentService.CreatePaymentAsync(request, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Yeni ödeme oluşturuldu: UserId={UserId}, PaymentId={PaymentId}", userId, result.Data?.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme oluşturulurken hata");
                return StatusCode(500, ApiResponse<PaymentDto>.FailResult("Ödeme oluşturulamadı"));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> UpdatePayment(int id, [FromBody] CreatePaymentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<PaymentDto>.FailResult("Geçersiz veri"));
                }

                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<PaymentDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _paymentService.UpdatePaymentAsync(id, request, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Ödeme güncellendi: UserId={UserId}, PaymentId={PaymentId}", userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme güncellenirken hata: PaymentId={PaymentId}", id);
                return StatusCode(500, ApiResponse<PaymentDto>.FailResult("Ödeme güncellenemedi"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeletePayment(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<bool>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _paymentService.DeletePaymentAsync(id, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Ödeme silindi: UserId={UserId}, PaymentId={PaymentId}", userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme silinirken hata: PaymentId={PaymentId}", id);
                return StatusCode(500, ApiResponse<bool>.FailResult("Ödeme silinemedi"));
            }
        }

        [HttpGet("sale/{saleId}")]
        public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetPaymentsBySaleId(int saleId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<List<PaymentDto>>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _paymentService.GetPaymentsBySaleIdAsync(saleId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış ödemeleri listelenirken hata: SaleId={SaleId}", saleId);
                return StatusCode(500, ApiResponse<List<PaymentDto>>.FailResult("Satış ödemeleri listelenemedi"));
            }
        }
    }
}