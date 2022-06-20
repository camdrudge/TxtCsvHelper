using System;
using System.Collections.Generic;
using System.Numerics;

namespace TxtCsvHelper
{
    public class Converter
    {
		public static object GetField(string input, IConverter converter)
		{
			var value = converter.ConvertFromString(input);
			return value;
		}
		public static Dictionary<Type, IConverter> CreateConverters()
		{
			Dictionary<Type, IConverter> kvp = new Dictionary<Type, IConverter>();
			kvp.Add(typeof(BigInteger), new BigIntegerConverter());
			kvp.Add(typeof(bool), new BoolConverter());
			kvp.Add(typeof(byte), new ByteConverter());
			kvp.Add(typeof(byte[]), new ByteArrayConverter());
			kvp.Add(typeof(char[]), new CharArrayConverter());
			kvp.Add(typeof(char), new CharConverter());
			kvp.Add(typeof(DateTime), new DateTimeConverter());
			kvp.Add(typeof(DateTimeOffset), new DateTimeOffsetConverter());
			kvp.Add(typeof(decimal), new DecimalConverter());
			kvp.Add(typeof(double), new DoubleConverter());
			kvp.Add(typeof(float), new SingleConverter());
			kvp.Add(typeof(Guid), new GuidConverter());
			kvp.Add(typeof(short), new Int16Converter());
			kvp.Add(typeof(int), new Int32Converter());
			kvp.Add(typeof(long), new Int64Converter());
			kvp.Add(typeof(sbyte), new SByteConverter());
			kvp.Add(typeof(TimeSpan), new TimeSpanConverter());
			kvp.Add(typeof(ushort), new UInt16Converter());
			kvp.Add(typeof(uint), new UInt32Converter());
			kvp.Add(typeof(ulong), new UInt64Converter());
			kvp.Add(typeof(Uri), new UriConverter());
			kvp.Add(typeof(string), new DefaultConverter());
			return kvp;
		}
	}
}
