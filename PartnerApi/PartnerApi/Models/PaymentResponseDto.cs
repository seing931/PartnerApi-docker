using System.Text.Json.Serialization;

namespace PartnerApi.Models
{
    public record PaymentResponseDto(
        [property: JsonPropertyName("result")] int Result,
        [property: JsonPropertyName("totalamount")] long? TotalAmount = null,
        [property: JsonPropertyName("totaldiscount")] long? TotalDiscount = null,
        [property: JsonPropertyName("finalamount")] long? FinalAmount = null,
        [property: JsonPropertyName("resultmessage")] string? ResultMessage = null
    )
    {
        public static PaymentResponseDto Success(long totalAmount, long totalDiscount, long finalAmount)
            => new(1, totalAmount, totalDiscount, finalAmount);

        public static PaymentResponseDto Failure(string message)
            => new(0, ResultMessage: message);
    }
}
