using System;

namespace TxtCsvHelper
{
    public class Int64Converter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (Int64.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out Int64 i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
