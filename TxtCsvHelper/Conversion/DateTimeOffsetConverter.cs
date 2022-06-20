using System;

namespace TxtCsvHelper
{
    public class DateTimeOffsetConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (DateTimeOffset.TryParse(value, Configuration.CultureInfo, Configuration.DateStyles, out DateTimeOffset i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
