using System.Text.Json.Serialization;

namespace PartnerApi.Models
{
    public record ItemDto(
        [property: JsonPropertyName("partneritemref")] string? PartnerItemRef,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("qty")] int? Qty,
        [property: JsonPropertyName("unitprice")] long? UnitPrice
    );
}
