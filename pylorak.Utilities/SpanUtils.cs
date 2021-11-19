using System;

namespace pylorak.Utilities
{
    public static class ReadOnlySpanCharExtensions
    {
        public static bool Equals(this ReadOnlySpan<char> span, string other, StringComparison opts)
        {
            return span.Equals(other.AsSpan(), opts);
        }

        private static (ulong, bool) DecimalToNumeric(this ReadOnlySpan<char> span, int maxDecimals, bool negativeAllowed)
        {
            ulong ret = 0;
            bool negative = false;

            // Skip leading and trailing whitespace
            span = span.Trim();

            // String may begin with a sign
            if (span[0] == '+')
            {
                span = span.Slice(1);
            }
            else if (negativeAllowed && (span[0] == '-'))
            {
                negative = true;
                span = span.Slice(1);
            }

            // String must not be empty after the sign
            if (span.Length == 0)
                throw new FormatException();

            for (int i = 0; i < span.Length; ++i)
            {
                if (i == maxDecimals)
                    throw new OverflowException();

                char c = span[i];
                if (char.IsDigit(c))
                    ret = ret * 10UL + (ulong)(c - 48);
                else
                    throw new FormatException();
            }

            return (ret, negative);
        }

        public static int DecimalToInt32(this ReadOnlySpan<char> span)
        {
            (var unsignedVal, var negative) = DecimalToNumeric(span, 10, true);
            return checked((negative ? (int)-(uint)unsignedVal : (int)unsignedVal));
        }
        public static ushort DecimalToUInt16(this ReadOnlySpan<char> span)
        {
            (var unsignedVal, var _) = DecimalToNumeric(span, 5, false);
            return checked((ushort)unsignedVal);
        }
        public static bool TryDecimalToUInt16(this ReadOnlySpan<char> span, out ushort result)
        {
            try
            {
                result = DecimalToUInt16(span);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
    }

    public static class SpanUtils
    {
        public static unsafe string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1)
        {
            var result = new string('\0', checked(str0.Length + str1.Length));
            fixed (char* resultPtr = result)
            {
                var resultSpan = new Span<char>(resultPtr, result.Length);

                str0.CopyTo(resultSpan);
                resultSpan = resultSpan.Slice(str0.Length);

                str1.CopyTo(resultSpan);
            }
            return result;
        }

        public static unsafe string Join(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, char separator)
        {
            var result = new string('\0', checked(str0.Length + str1.Length + 1));
            fixed (char* resultPtr = result)
            {
                var resultSpan = new Span<char>(resultPtr, result.Length);

                str0.CopyTo(resultSpan);
                resultSpan = resultSpan.Slice(str0.Length);

                resultSpan[0] = separator;
                resultSpan = resultSpan.Slice(1);

                str1.CopyTo(resultSpan);
            }
            return result;
        }

        public static unsafe string CombinePath(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1)
        {
            if ((str0[str0.Length - 1] == '\\') || (str0[str0.Length - 1] == '/'))
                return Concat(str0, str1);
            else
                return Join(str0, str1, '\\');
        }
    }
}
