using System.Globalization;

namespace TxtCsvHelper
{
    public class Configuration
    {
        public static CultureInfo CultureInfo { get; set; } = CultureInfo.CurrentCulture;
        public static NumberStyles NumberStyles { get; set; } = NumberStyles.Currency;
        public static DateTimeStyles DateStyles { get; set; } = DateTimeStyles.AssumeLocal;
    }
}
