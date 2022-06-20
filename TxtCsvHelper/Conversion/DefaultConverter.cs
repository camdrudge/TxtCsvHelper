namespace TxtCsvHelper
{
    public class DefaultConverter : IConverter
    {
        public virtual object ConvertFromString(string value)
        {
            return value;
        }
    }
}
