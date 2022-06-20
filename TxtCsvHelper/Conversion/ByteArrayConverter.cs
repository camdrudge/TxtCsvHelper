using System.Text;

namespace TxtCsvHelper
{
    public class ByteArrayConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            if (bytes != null)
            {
                return bytes;
            }
            return base.ConvertFromString(value);
        }
    }
}
