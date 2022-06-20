using System;
namespace TxtCsvHelper
{
    public class TimeSpanConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(TimeSpan.TryParse(value, Configuration.CultureInfo, out TimeSpan ts))
            {
                return ts;
            }
            return base.ConvertFromString(value);
        }
    }
}
