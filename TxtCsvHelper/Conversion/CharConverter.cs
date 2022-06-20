namespace TxtCsvHelper
{
    public class CharConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(value.Length > 1)
            {
                value = value.Trim();
            }
            if(char.TryParse(value, out char c))
            {
                return c;
            }
            return base.ConvertFromString(value);
        }
    }
}
