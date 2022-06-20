using System;
namespace TxtCsvHelper
{
    public class Int16Converter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (Int16.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out Int16 i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
