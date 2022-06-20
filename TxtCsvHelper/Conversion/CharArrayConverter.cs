namespace TxtCsvHelper
{
    public class CharArrayConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if(value == null)
            {
                return base.ConvertFromString(value);
            }
            char[] ch = new char[value.Length];
            for(int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                ch.SetValue(c, i);
            }
            return ch;
        }
    }
}
