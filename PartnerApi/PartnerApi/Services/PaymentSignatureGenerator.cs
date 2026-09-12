using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PartnerApi.Services
{
    public interface IPaymentSignatureGenerator
    {
        string GenerateSignature(string timestampIso, string partnerKey, string partnerRefNo, long totalAmount, string partnerPasswordEncoded);
    }
    public class PaymentSignatureGenerator : IPaymentSignatureGenerator
    {
        public string GenerateSignature(string timestampIso, string partnerKey, string partnerRefNo, long totalAmount, string partnerPasswordEncoded)
        {
            if (!DateTimeOffset.TryParse(timestampIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
                return string.Empty;

            // Format: yyyyMMddHHmmss + partnerkey + partnerrefno + totalamount + partnerpassword(encoded)
            string formattedTimestamp = parsedDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string rawString = $"{formattedTimestamp}{partnerKey}{partnerRefNo}{totalAmount}{partnerPasswordEncoded}";

            // SHA-256 Hash
            byte[] rawBytes = Encoding.UTF8.GetBytes(rawString);
            byte[] hashBytes = SHA256.HashData(rawBytes);

            // Lowercase Hexadecimal Hash Output
            string hexHash = Convert.ToHexStringLower(hashBytes);

            // Convert Hex string to Base64 (UTF-8)
            byte[] hexBytes = Encoding.UTF8.GetBytes(hexHash);
            return Convert.ToBase64String(hexBytes);
        }
    }
}
