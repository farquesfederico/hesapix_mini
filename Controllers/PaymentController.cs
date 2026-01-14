using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using System.Security.Claims;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        /// <summary>
        /// Yeni ödeme/tahsilat ekle
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var payment = await _paymentService.CreatePayment(request, GetUserId());
                _logger.LogInformation("Yeni ödeme kaydı oluşturuldu - ID: {Id}, Type: {Type}",
                    payment.Id, payment.PaymentType);
                return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme oluşturma hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tüm ödemeleri getir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var payments = await _paymentService.GetPayments(
                    userId: GetUserId(),
                    page: 1,
                    pageSize:20,
                    startDate: startDate,
                    endDate: endDate);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme listeleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// ID'ye göre ödeme getir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentById(id, GetUserId());
                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme getirme hatası - ID: {Id}", id);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Satışa göre ödemeleri getir
        /// </summary>
        [HttpGet("by-sale/{saleId}")]
        public async Task<IActionResult> GetBySale(int saleId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsBySaleId(saleId, GetUserId());
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış ödemeleri getirme hatası - SaleID: {SaleId}", saleId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Ödeme tipine göre kayıtları getir
        /// </summary>
        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetByType(PaymentType type)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByType(type, GetUserId());
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme tipi listeleme hatası - Type: {Type}", type);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tahsilatları getir (Income)
        /// </summary>
        [HttpGet("incomes")]
        public async Task<IActionResult> GetIncomes([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var payments = await _paymentService.GetPayments(
                    userId: GetUserId(),
                    page:1,
                    pageSize: 20,
                    startDate: startDate,
                    endDate: endDate);
                var incomes = payments.Where(p => p.PaymentType == PaymentType.Income).ToList();
                return Ok(incomes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tahsilat listeleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Giderleri getir (Expense)
        /// </summary>
        [HttpGet("expenses")]
        public async Task<IActionResult> GetExpenses([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var payments = await _paymentService.GetPayments(userId: GetUserId(), page: 1, pageSize: 20, startDate: startDate, endDate: endDate);
                var expenses = payments.Where(p => p.PaymentType == PaymentType.Expense).ToList();
                return Ok(expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gider listeleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Ödeme sil
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _paymentService.DeletePayment(id, GetUserId());

                if (!result)
                {
                    return NotFound(new { message = "Ödeme kaydı bulunamadı" });
                }

                _logger.LogInformation("Ödeme silindi - ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme silme hatası - ID: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Ödeme istatistikleri
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var payments = await _paymentService.GetPayments(userId: GetUserId(), page: 1, pageSize: 20, startDate: startDate, endDate: endDate);

                var statistics = new
                {
                    totalPayments = payments.Count,
                    totalIncome = payments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount),
                    totalExpense = payments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount),
                    netCashFlow = payments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount) -
                                  payments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount),
                    incomeCount = payments.Count(p => p.PaymentType == PaymentType.Income),
                    expenseCount = payments.Count(p => p.PaymentType == PaymentType.Expense),
                    paymentMethods = payments.GroupBy(p => p.PaymentMethod)
                        .Select(g => new
                        {
                            method = g.Key.ToString(),
                            count = g.Count(),
                            total = g.Sum(p => p.Amount)
                        }).ToList()
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme istatistikleri hatası");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}