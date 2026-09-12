using System;

namespace PartnerApi.Helper
{
    public static class MathUtils
    {
        /// <summary>
        /// Extension method to check if a long integer is a prime number.
        /// </summary>
        public static bool IsPrime(this long number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            var boundary = (long)Math.Floor(Math.Sqrt(number));
            for (long i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0) return false;
            }
            return true;
        }
    }
}
