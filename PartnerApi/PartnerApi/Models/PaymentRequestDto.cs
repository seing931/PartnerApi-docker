using System.Text.Json.Serialization;

namespace PartnerApi.Models
{
    public record PaymentRequestDto(
      [property: JsonPropertyName("partnerkey")] string? PartnerKey,
      [property: JsonPropertyName("partnerrefno")] string? PartnerRefNo,
      [property: JsonPropertyName("partnerpassword")] string? PartnerPassword,
      [property: JsonPropertyName("totalamount")] long? TotalAmount,
      [property: JsonPropertyName("items")] List<ItemDto>? Items,
      [property: JsonPropertyName("timestamp")] string? Timestamp,
      [property: JsonPropertyName("sig")] string? Sig
  );
}
