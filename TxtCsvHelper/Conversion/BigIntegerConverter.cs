using System.Numerics;

namespace TxtCsvHelper
{
    public class BigIntegerConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(BigInteger.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out BigInteger i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
