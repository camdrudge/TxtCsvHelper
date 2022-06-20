namespace TxtCsvHelper
{
    public class DecimalConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (decimal.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out decimal i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
