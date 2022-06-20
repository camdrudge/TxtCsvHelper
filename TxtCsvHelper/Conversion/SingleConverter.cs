using System.Globalization;

namespace TxtCsvHelper
{
    public class SingleConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(float.TryParse(value, NumberStyles.AllowThousands, Configuration.CultureInfo, out float f))
            {
                return f;
            }
            return base.ConvertFromString(value);
        }
    }
}
