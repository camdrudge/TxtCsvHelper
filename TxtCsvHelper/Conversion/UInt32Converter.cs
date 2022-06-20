namespace TxtCsvHelper
{
    public class UInt32Converter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (uint.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out uint i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
