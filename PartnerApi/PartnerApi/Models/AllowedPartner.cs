namespace PartnerApi.Models
{
    public record AllowedPartner
    (
        string PartnerRefNo, 
        string PartnerKey, 
        string PlainPassword
    );
}
