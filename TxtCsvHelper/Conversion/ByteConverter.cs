namespace TxtCsvHelper
{
    public class ByteConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(byte.TryParse(value, out byte b))
            {
                return b;
            }
            return base.ConvertFromString(value);
        }
    }
}
