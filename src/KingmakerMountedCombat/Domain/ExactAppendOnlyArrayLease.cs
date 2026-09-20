using System;

namespace KingmakerMountedCombat.Domain
{
    public sealed class ExactAppendOnlyArrayLease<T> where T : class
    {
        private readonly T[] original;
        private readonly T[] appended;

        public ExactAppendOnlyArrayLease(T[] source, T item)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (item == null) { throw new ArgumentNullException(nameof(item)); }
            for (var index = 0; index < source.Length; index++)
            {
                if (source[index] == null)
                {
                    throw new ArgumentException("The source array contains a null entry.", nameof(source));
                }
                if (ReferenceEquals(source[index], item))
                {
                    throw new ArgumentException("The appended item is already present by exact reference.", nameof(item));
                }
            }

            original = (T[])source.Clone();
            appended = new T[source.Length + 1];
            Array.Copy(source, appended, source.Length);
            appended[source.Length] = item;
        }

        public int OriginalCount => original.Length;

        public int AppendedCount => appended.Length;

        public T[] CreateAppendedValue()
        {
            return (T[])appended.Clone();
        }

        public T[] CreateOriginalValue()
        {
            return (T[])original.Clone();
        }

        public bool MatchesOriginal(T[] value)
        {
            return Matches(value, original);
        }

        public bool MatchesAppended(T[] value)
        {
            return Matches(value, appended);
        }

        public bool TryRestore(T[] current, out T[] restored, out string error)
        {
            restored = null;
            error = null;
            if (MatchesOriginal(current))
            {
                restored = (T[])original.Clone();
                return true;
            }
            if (!MatchesAppended(current))
            {
                error = "The leased array changed after the exact append; compare-before-restore rejected overwrite.";
                return false;
            }

            restored = (T[])original.Clone();
            return true;
        }

        private static bool Matches(T[] left, T[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }
            for (var index = 0; index < left.Length; index++)
            {
                if (!ReferenceEquals(left[index], right[index]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
