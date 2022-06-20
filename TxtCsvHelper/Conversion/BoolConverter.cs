

namespace TxtCsvHelper
{
    public class BoolConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (bool.TryParse(value, out var b))
            {
                return b;
            }

            if (short.TryParse(value, out var sh))
            {
                if (sh == 0)
                {
                    return false;
                }
                if (sh == 1)
                {
                    return true;
                }
            }
            return base.ConvertFromString(value);
        }
    }
}
