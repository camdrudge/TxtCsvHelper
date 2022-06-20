namespace TxtCsvHelper
{
    public class Int32Converter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (int.TryParse(value, Configuration.NumberStyles, Configuration.CultureInfo, out int i))
            {
                return i;
            }
            return base.ConvertFromString(value);
        }
    }
}
