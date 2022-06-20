using System.Globalization;
namespace TxtCsvHelper
{
    public class DoubleConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(double.TryParse(value, NumberStyles.Float, Configuration.CultureInfo, out double d))
            {
                return d;
            }
            return base.ConvertFromString(value);
        }
    }
}
