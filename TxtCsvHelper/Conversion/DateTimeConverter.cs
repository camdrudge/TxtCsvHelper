using System;

namespace TxtCsvHelper
{
    public class DateTimeConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (DateTime.TryParse(value, Configuration.CultureInfo, Configuration.DateStyles, out DateTime i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
