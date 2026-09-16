using System.Text.RegularExpressions;
using MyDergiApp.Models;

namespace MyDergiApp.Helpers
{
    /// <summary>
    /// Cilt ve Sayi alanlari metin (orn. "10", "Özel Sayı 2"); veritabaninda metinsel siralama
    /// "9" &gt; "10" sonucunu verir. Bu yardimci, metnin icindeki ilk sayiyi alarak dogal sirada siralar.
    /// Yil -> Cilt -> Sayi, hepsi azalan.
    /// </summary>
    public static class IssueOrdering
    {
        private static readonly Regex FirstNumber = new(@"\d+", RegexOptions.Compiled);

        public static int NumericKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return int.MinValue;

            var m = FirstNumber.Match(value);
            return m.Success && int.TryParse(m.Value, out var n) ? n : int.MinValue;
        }

        public static IOrderedEnumerable<Issue> NewestFirst(this IEnumerable<Issue> issues)
            => issues
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => NumericKey(i.Volume))
                .ThenByDescending(i => i.Volume, StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(i => NumericKey(i.Number))
                .ThenByDescending(i => i.Number, StringComparer.CurrentCultureIgnoreCase);
    }
}
