using System;
namespace TxtCsvHelper
{
    public class GuidConverter : DefaultConverter
    {
        public override object ConvertFromString(string value)
        {
            if (value == null)
            {
                return base.ConvertFromString(value);
            }
            return new Guid(value);
        }
    }
}
