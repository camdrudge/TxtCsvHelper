using System;

namespace TxtCsvHelper
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class HeaderName : Attribute
    {
        public HeaderName(string name)
        {
            Name = name;
        }
        public string Name { get; }
    }
}
