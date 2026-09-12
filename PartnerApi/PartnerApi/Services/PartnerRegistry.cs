using PartnerApi.Models;
using System.Text;

namespace PartnerApi.Services
{
    public interface IPartnerRegistry
    {
        bool IsAllowedPartner(string partnerRefNo, string partnerKey, string base64Password);
    }
    public class PartnerRegistry : IPartnerRegistry
    {
        private static readonly List<AllowedPartner> Partners = new()
    {
        new("FG-00001", "FAKEGOOGLE", "FAKEPASSWORD1234"),
        new("FG-00002", "FAKEPEOPLE", "FAKEPASSWORD4578")
    };

        public bool IsAllowedPartner(string partnerRefNo, string partnerKey, string base64Password)
        {
            string decodedPassword;
            try
            {
                byte[] bytes = Convert.FromBase64String(base64Password);
                decodedPassword = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return false;
            }

            return Partners.Any(p =>
                string.Equals(p.PartnerRefNo, partnerRefNo, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.PartnerKey, partnerKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.PlainPassword, decodedPassword, StringComparison.Ordinal));
        }
    }
}
