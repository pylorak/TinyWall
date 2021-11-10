using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKSoft
{
    [Flags]
    public enum SpanSplitOptions
    {
        None,
        RemoveEmptyEntries
    }

    public static class ReadOnlySpanExtension
    {
        public static ReadOnlySpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> span, T separator, SpanSplitOptions options = SpanSplitOptions.None) where T : IEquatable<T>
            => new ReadOnlySpanSplitEnumerator<T>(span, separator, options);
    }

    public ref struct ReadOnlySpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly T Separator;
        private readonly SpanSplitOptions Options;

        private ReadOnlySpan<T> ParentSpan;
        private ReadOnlySpan<T> CurrentItem;
        private int ScanStart;

        public ReadOnlySpanSplitEnumerator(ReadOnlySpan<T> parent, T separator, SpanSplitOptions options)
        {
            ParentSpan = parent;
            Separator = separator;
            Options = options;
            CurrentItem = default;
            ScanStart = 0;
        }

        public ReadOnlySpanSplitEnumerator<T> GetEnumerator() => this;

        public ReadOnlySpan<T> Current => CurrentItem;

        public void Dispose()
        {
            // Empty on purpose
        }

        public bool MoveNext()
        {
            do
            {
                // Terminate if we reached the end of the parent span
                if (ScanStart > ParentSpan.Length)
                    return false;

                ParentSpan = ParentSpan.Slice(ScanStart);

                // Scan for next separator
                var idx = ParentSpan.IndexOf(Separator);
                if (idx == -1)
                {
                    // Separator not found, so the result is the whole of the span
                    CurrentItem = ParentSpan;
                }
                else
                {
                    // Separator found
                    CurrentItem = ParentSpan.Slice(0, idx);
                }
                ScanStart = CurrentItem.Length + 1;
            }
            while (((Options & SpanSplitOptions.RemoveEmptyEntries) != 0) && (CurrentItem.Length == 0));

            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
