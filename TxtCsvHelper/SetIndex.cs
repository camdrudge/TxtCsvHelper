using System;

namespace TxtCsvHelper
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class SetIndex : Attribute
    {
        public SetIndex(int index)
        {
            IndexNum = index;
        }

        public int IndexNum { get; }
    }
}
