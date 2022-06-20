namespace TxtCsvHelper
{
    public interface IConverter
    {
        object ConvertFromString(string value);
    }
}
