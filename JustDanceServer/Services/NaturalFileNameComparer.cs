using System.Globalization;

namespace JustDanceServer.Services;

internal sealed class NaturalFileNameComparer : IComparer<string>
{
    public static NaturalFileNameComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left is null)
            return -1;
        if (right is null)
            return 1;

        ReadOnlySpan<char> a = Path.GetFileName(left);
        ReadOnlySpan<char> b = Path.GetFileName(right);
        int ai = 0;
        int bi = 0;
        while (ai < a.Length && bi < b.Length)
        {
            if (char.IsDigit(a[ai]) && char.IsDigit(b[bi]))
            {
                int aStart = ai;
                int bStart = bi;
                while (ai < a.Length && char.IsDigit(a[ai]))
                    ai++;
                while (bi < b.Length && char.IsDigit(b[bi]))
                    bi++;

                ReadOnlySpan<char> aNumber = a[aStart..ai].TrimStart('0');
                ReadOnlySpan<char> bNumber = b[bStart..bi].TrimStart('0');
                int lengthComparison = aNumber.Length.CompareTo(bNumber.Length);
                if (lengthComparison != 0)
                    return lengthComparison;

                int numberComparison = aNumber.CompareTo(bNumber, StringComparison.Ordinal);
                if (numberComparison != 0)
                    return numberComparison;
            }
            else
            {
                int characterComparison = char.ToUpper(a[ai], CultureInfo.InvariantCulture)
                    .CompareTo(char.ToUpper(b[bi], CultureInfo.InvariantCulture));
                if (characterComparison != 0)
                    return characterComparison;
                ai++;
                bi++;
            }
        }

        return a.Length.CompareTo(b.Length);
    }
}
