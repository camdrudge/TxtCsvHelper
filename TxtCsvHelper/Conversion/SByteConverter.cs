namespace TxtCsvHelper
{
    public class SByteConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (sbyte.TryParse(value, out sbyte b))
            {
                return b;
            }
            return base.ConvertFromString(value);
        }
    }
}
