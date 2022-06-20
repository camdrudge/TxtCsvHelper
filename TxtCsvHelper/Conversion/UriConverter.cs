using System;
namespace TxtCsvHelper
{
    public class UriConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
