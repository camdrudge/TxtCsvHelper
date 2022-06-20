using System;
namespace TxtCsvHelper
{
    public class UInt64Converter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (UInt64.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out UInt64 i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
