using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PartnerApi.Logging
{
    public static class LogEncryptionHelper
    {
        private static readonly byte[] LogKey = Encoding.UTF8.GetBytes("SuperSecretLogEncryptionKey12345"); // 32 Bytes for AES-256

        public static string EncryptPasswordValue(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            try
            {
                byte[] nonce = new byte[12];
                RandomNumberGenerator.Fill(nonce);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = new byte[plainBytes.Length];
                byte[] tag = new byte[16];

                using var aesGcm = new AesGcm(LogKey, tag.Length);
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

                byte[] combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
                Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
                Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
                Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

                return $"[ENCRYPTED_AES256:{Convert.ToBase64String(combined)}]";
            }
            catch
            {
                return "[ENCRYPTION_FAILED]";
            }
        }

        public static string SanitizeJsonPayload(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) return jsonString;

            try
            {
                var node = JsonNode.Parse(jsonString);
                if (node is JsonObject obj)
                {
                    EncryptSensitiveFields(obj);
                    return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
                }
            }
            catch
            {
                // Ignore if non-JSON payload
            }
            return jsonString;
        }

        private static void EncryptSensitiveFields(JsonObject obj)
        {
            foreach (var property in obj)
            {
                if (property.Key.Equals("partnerpassword", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value != null)
                    {
                        obj[property.Key] = EncryptPasswordValue(property.Value.ToString());
                    }
                }
                else if (property.Value is JsonObject childObj)
                {
                    EncryptSensitiveFields(childObj);
                }
                else if (property.Value is JsonArray childArray)
                {
                    foreach (var item in childArray)
                    {
                        if (item is JsonObject arrObj)
                        {
                            EncryptSensitiveFields(arrObj);
                        }
                    }
                }
            }
        }
    }
}
