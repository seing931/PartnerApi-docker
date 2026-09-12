using Microsoft.AspNetCore.Mvc;
using PartnerApi.Models;
using PartnerApi.Services;

namespace PartnerApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class TransactionController : ControllerBase
    {
        private readonly IPaymentProcessingService _paymentService;

        public TransactionController(IPaymentProcessingService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("submittrxmessage")]
        public IActionResult SubmitTransaction([FromBody] PaymentRequestDto request)
        {
            // 1. Validate & Authorize Request
            var (isValid, errorMessage) = _paymentService.ValidateAndAuthorize(request);

            if (!isValid)
            {
                return Ok(PaymentResponseDto.Failure(errorMessage ?? "Validation failed."));
            }

            // 2. Calculate Final Amount & Discounts
            var (totalDiscount, finalAmount) = _paymentService.CalculateDiscount(request.TotalAmount!.Value);

            // 3. Return Success Result
            return Ok(PaymentResponseDto.Success(
                request.TotalAmount.Value,
                totalDiscount,
                finalAmount
            ));
        }
    }
}
