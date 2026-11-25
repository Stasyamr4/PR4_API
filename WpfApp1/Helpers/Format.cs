using System.Globalization;

namespace WpfApp1.Helpers
{
    /// <summary>Единое форматирование денег, дат и звёзд рейтинга.</summary>
    public static class Format
    {
        public static readonly CultureInfo Ru = new("ru-RU");

        public static string Money(decimal value) => value.ToString("N0", Ru) + " ₽";

        public static string Date(DateTime value) => value.ToString("dd.MM.yyyy HH:mm", Ru);

        /// <summary>Рейтинг 0..5 в виде звезд (округляется до целого).</summary>
        public static string Stars(double rating)
        {
            int n = (int)Math.Round(Math.Clamp(rating, 0, 5));
            return new string('★', n) + new string('☆', 5 - n);
        }
    }
}
