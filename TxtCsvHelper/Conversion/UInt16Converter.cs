namespace TxtCsvHelper
{
    public class UInt16Converter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(ushort.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out ushort i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
