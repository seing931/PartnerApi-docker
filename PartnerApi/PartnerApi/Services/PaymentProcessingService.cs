using PartnerApi.Helper;
using PartnerApi.Models;
using System.Globalization;

namespace PartnerApi.Services
{
    public interface IPaymentProcessingService
    {
        (bool IsValid, string? ErrorMessage) ValidateAndAuthorize(PaymentRequestDto request);
        (long TotalDiscount, long FinalAmount) CalculateDiscount(long totalAmountCents);
    }

    public class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly IPartnerRegistry _partnerRegistry;
        private readonly IPaymentSignatureGenerator _signatureGenerator;
        private readonly TimeProvider _timeProvider;

        public PaymentProcessingService(
            IPartnerRegistry partnerRegistry,
            IPaymentSignatureGenerator signatureGenerator,
            TimeProvider? timeProvider = null)
        {
            _partnerRegistry = partnerRegistry;
            _signatureGenerator = signatureGenerator;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public (bool IsValid, string? ErrorMessage) ValidateAndAuthorize(PaymentRequestDto request)
        {
            // -----------------------------------------------------------------
            // Mandatory Parameters Validation
            // -----------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(request.PartnerKey))
                return (false, "partnerkey is required.");

            if (string.IsNullOrWhiteSpace(request.PartnerRefNo))
                return (false, "partnerrefno is required.");

            if (string.IsNullOrWhiteSpace(request.PartnerPassword))
                return (false, "partnerpassword is required.");

            if (request.TotalAmount is null)
                return (false, "totalamount is required.");

            if (request.TotalAmount <= 0)
                return (false, "totalamount must be positive.");

            if (request.Items is null || request.Items.Count == 0)
                return (false, "items is required.");

            if (string.IsNullOrWhiteSpace(request.Timestamp))
                return (false, "timestamp is required.");

            if (string.IsNullOrWhiteSpace(request.Sig))
                return (false, "sig is required.");

            // -----------------------------------------------------------------
            // Partner Authorization Check
            // -----------------------------------------------------------------
            if (!_partnerRegistry.IsAllowedPartner(request.PartnerRefNo, request.PartnerKey, request.PartnerPassword))
            {
                return (false, "Access Denied!");
            }

            // -----------------------------------------------------------------
            // Item-level Field & Business Rules Validations
            // -----------------------------------------------------------------
            long calculatedItemsTotal = 0;
            for (int i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];

                if (string.IsNullOrWhiteSpace(item.PartnerItemRef))
                    return (false, $"items[{i}].partneritemref is required.");

                if (item.PartnerItemRef.Length > 50)
                    return (false, $"items[{i}].partneritemref exceeds maximum length of 50.");

                if (string.IsNullOrWhiteSpace(item.Name))
                    return (false, $"items[{i}].name is required.");

                if (item.Name.Length > 100)
                    return (false, $"items[{i}].name exceeds maximum length of 100.");

                if (item.Qty is null)
                    return (false, $"items[{i}].qty is required.");

                if (item.Qty <= 0)
                    return (false, $"items[{i}].qty must be positive.");

                if (item.Qty > 5)
                    return (false, "Quantity must not exceed 5.");

                if (item.UnitPrice is null)
                    return (false, $"items[{i}].unitprice is required.");

                if (item.UnitPrice <= 0)
                    return (false, $"items[{i}].unitprice must be positive.");

                calculatedItemsTotal += item.Qty.Value * item.UnitPrice.Value;
            }

            if (request.TotalAmount.Value != calculatedItemsTotal)
            {
                return (false, "Invalid Total Amount.");
            }

            // -----------------------------------------------------------------
            // Timestamp Expiry Check (+- 5 Minutes)
            // -----------------------------------------------------------------
            if (!DateTimeOffset.TryParse(request.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var requestTime))
            {
                return (false, "Invalid timestamp format.");
            }

            var serverTime = _timeProvider.GetUtcNow();
            if ((requestTime - serverTime).Duration() > TimeSpan.FromMinutes(5))
            {
                return (false, "Expired.");
            }

            // -----------------------------------------------------------------
            // Signature Verification Logic
            // -----------------------------------------------------------------
            string expectedSig = _signatureGenerator.GenerateSignature(
                request.Timestamp,
                request.PartnerKey,
                request.PartnerRefNo,
                request.TotalAmount.Value,
                request.PartnerPassword
            );

            if (!string.Equals(request.Sig.Trim(), expectedSig, StringComparison.Ordinal))
            {
                return (false, "Access Denied!");
            }

            return (true, null);
        }

        public (long TotalDiscount, long FinalAmount) CalculateDiscount(long totalAmountCents)
        {
            decimal totalAmountMyr = totalAmountCents / 100m;

            // 1. Base Discount
            int baseDiscountPct = totalAmountMyr switch
            {
                < 200m => 0,
                <= 500m => 5,
                <= 800m => 7,
                <= 1200m => 10,
                _ => 15
            };

            // 2. Conditional Discounts
            int conditionalDiscountPct = 0;
            long integerMyr = totalAmountCents / 100;

            // Condition 1: Prime number above MYR 500
            if (totalAmountMyr > 500m && totalAmountCents % 100 == 0 && integerMyr.IsPrime())
            {
                conditionalDiscountPct += 8;
            }

            // Condition 2: Ends in digit 5 and above MYR 900
            if (totalAmountMyr > 900m && totalAmountCents % 100 == 0 && integerMyr % 10 == 5)
            {
                conditionalDiscountPct += 10;
            }

            // 3. Cap on Maximum Discount (20%)
            int finalDiscountPct = Math.Min(baseDiscountPct + conditionalDiscountPct, 20);

            long totalDiscountCents = (totalAmountCents * finalDiscountPct) / 100;
            long finalAmountCents = totalAmountCents - totalDiscountCents;

            return (totalDiscountCents, finalAmountCents);
        }
    }
}
