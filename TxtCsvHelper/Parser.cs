using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace TxtCsvHelper
{
    /// <summary>
    /// Parse a delimited file. Delimiter can be any character
    /// Parser can fill objects, create dynamics, or simply split a line
    /// Parser will not have errors with the delimiter or line breaks inside of quotes
    /// </summary>
    public class Parser : IDisposable
    {
        /// <summary>
        /// Creates an instance of Parser
        /// Sets Delimiter to comma, HasHeader to true, and HasSpaces to false
        /// HasSpaces is only necessary if there is a space between the delimiter and the next field
        /// </summary>
        public Parser()
        {
            Delimiter = ',';
            HasHeader = true;
            HasSpaces = false;
            LineCounter = 0;
        }
        /// <summary>
        /// Creates an instance of Parser
        /// </summary>
        /// <param name="delimiter">Sets the delimiter, optional, will default to comma</param>
        /// <param name="hasHeader">Indicates if there is a header row, optional, will default to true</param>
        /// <param name="hasSpaces">Indicates if there are spaces between the delimiter and the fields, optional, will default to false</param>
        public Parser(char delimiter = ',', bool hasHeader = true, bool hasSpaces = false)
        {
            Delimiter = delimiter;
            HasHeader = hasHeader;
            HasSpaces = hasSpaces;
            LineCounter = 0;
        }
        /// <summary>
        /// Creates an instance of Parser with a Stream as parameter
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="delimiter">Sets the delimiter, optional, will default to comma</param>
        /// <param name="hasHeader">Indicates if there is a header row, optional, will default to true</param>
        /// <param name="hasSpaces">Indicates if there are spaces between the delimiter and the fields, optional, will default to false</param>
        public Parser(Stream stream, char delimiter = ',', bool hasHeader = true, bool hasSpaces = false)
        {
            Rs = new ReadStream(stream);
            Delimiter = delimiter;
            HasHeader = hasHeader;
            HasSpaces = hasSpaces;
            LineCounter = 0;
        }
        /// <summary>
        /// Creates an instance of Parser with a StreamReader as parameter
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="delimiter">Sets the delimiter, optional, will default to comma</param>
        /// <param name="hasHeader">Indicates if there is a header row, optional, will default to true</param>
        /// <param name="hasSpaces">Indicates if there are spaces between the delimiter and the fields, optional, will default to false</param>
        public Parser(StreamReader stream, char delimiter = ',', bool hasHeader = true, bool hasSpaces = false)
        {
            Rs = new ReadStream(stream.BaseStream);
            Delimiter = delimiter;
            HasHeader = hasHeader;
            HasSpaces = hasSpaces;
            LineCounter = 0;
        }
        /// <summary>
        /// Creates an instance of Parser with a FileStream as a Parameter
        /// </summary>
        /// <param name="fileStream">FileStream</param>
        /// <param name="delimiter">Sets the delimiter, optional, will default to comma</param>
        /// <param name="hasHeader">Indicates if there is a header row, optional, will default to true</param>
        /// <param name="hasSpaces">Indicates if there are spaces between the delimiter and the fields, optional, will default to false</param>
        public Parser(FileStream fileStream, char delimiter = ',', bool hasHeader = true, bool hasSpaces = false)
        {
            Rs = new ReadStream(fileStream);
            Delimiter = delimiter;
            HasHeader = hasHeader;
            HasSpaces = hasSpaces;
            LineCounter = 0;
        }
        /// <summary>
        /// Creates an instance of Parser with a ReadStream as a Parameter, ReadStream is nearly identical to StreamReader but it allows for line breaks in fields
        /// </summary>
        /// <param name="readStream">sets a ReadStream, ReadStream is nearly identical to StreamReader but it allows for line breaks in fields</param>
        /// <param name="delimiter">Sets the delimiter, optional, will default to comma</param>
        /// <param name="hasHeader">Indicates if there is a header row, optional, will default to true</param>
        /// <param name="hasSpaces">Indicates if there are spaces between the delimiter and the fields, optional, will default to false</param>
        public Parser(ReadStream readStream, char delimiter = ',', bool hasHeader = true, bool hasSpaces = false)
        {
            Rs = readStream;
            Delimiter = delimiter;
            HasHeader = hasHeader;
            HasSpaces = hasSpaces;
            LineCounter = 0;
        }
        /// <summary>
        /// Creates an instance of Parser with a MemoryStream as a Parameter
        /// </summary>
        /// <param name="memoryStream">MemoryStream</param>
        /// <param name="delimiter">Sets the delimiter, optional, will default to comma</param>
        /// <param name="hasHeader">Indicates if there is a header row, optional, will default to true</param>
        /// <param name="hasSpaces">Indicates if there are spaces between the delimiter and the fields, optional, will default to false</param>
        public Parser(MemoryStream memoryStream, char delimiter = ',', bool hasHeader = true, bool hasSpaces = false)
        {
            Rs = new ReadStream(memoryStream);
            Delimiter = delimiter;
            HasHeader = hasHeader;
            HasSpaces = hasSpaces;
            LineCounter = 0;
        }
        /// <summary>
        /// Creates an instance of Parser with a string as a Parameter
        /// </summary>
        /// <param name="str">string</param>
        /// <param name="delimiter">Sets the delimiter, optional, will default to comma</param>
        /// <param name="hasHeader">Indicates if there is a header row, optional, will default to true</param>
        /// <param name="hasSpaces">Indicates if there are spaces between the delimiter and the fields, optional, will default to false</param>
        public Parser(string str, char delimiter = ',', bool hasHeader = true, bool hasSpaces = false)
        {
            UnicodeEncoding uniEncoding = new UnicodeEncoding();
            Ms = new MemoryStream();
            Sw = new StreamWriter(Ms, uniEncoding);
            Sw.Write(str);
            Sw.Flush();
            Ms.Seek(0, SeekOrigin.Begin);
            Rs = new ReadStream(Ms);

            Delimiter = delimiter;
            HasHeader = hasHeader;
            HasSpaces = hasSpaces;
            LineCounter = 0;
        }

        /// <summary>
        /// Disposes of properties
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Delimiter = ',';
                    HasHeader = true;
                    HasSpaces = false;
                    LineCounter = 0;
                    Disposed = true;
                }
            }
        }
        private readonly StreamWriter Sw;
        private readonly MemoryStream Ms;
        private readonly ReadStream Rs;
        private bool Disposed = false;
        private Dictionary<int, PropertyInfo> Header;
        private PropertyInfo Info;
        private char Delimiter;
        private int LineCounter;
        private bool HasSpaces;
        private bool HasHeader;
        private Dictionary<int, string> DynamicHeader;

        /// <summary>
        /// Returns an IEnumerable of a type
        /// </summary>
        /// <typeparam name="T">Takes any object as a type</typeparam>
        /// <returns>IEnumerable of type T</returns>
        public IEnumerable<T> Deserialize<T>()
        {
            try
            {
                Type type = typeof(T);
                List<ExpandoObject> d = new List<ExpandoObject> { };
                List<T> t = new List<T> { };

                using (Rs)
                {
                    if (type == typeof(Object))
                    {
                        if (HasHeader)
                        {
                            LineCounter++;
                            string line = Rs.ReadLine();
                            if (line == "")
                            {
                                return t;
                            }
                            FillDynamicDictionary(line);
                        }
                    }
                    else
                    {
                        if (HasHeader)
                        {
                            LineCounter++;
                            string line = Rs.ReadLine();
                            if(line == "")
                            {
                                return t;
                            }
                            T first = FillDictionary<T>(line);
                            if (first != null)
                            {
                                t.Add(first);
                            }
                        }
                        else
                        {
                            FillDictionaryNoHeader<T>();
                        }
                    }
                    if (type == typeof(Object))
                    {
                        if (HasSpaces)
                        {
                            while (Rs.Peek() >= 0)
                            {
                                LineCounter++;
                                d.Add(FillObjectsDynamicWithSpaces(Rs.ReadLine()));
                            }
                        }
                        else
                        {
                            while (Rs.Peek() >= 0)
                            {
                                LineCounter++;
                                d.Add(FillObjectsDynamic(Rs.ReadLine()));
                            }
                        }
                    }
                    else
                    {
                        if (HasSpaces)
                        {
                            while (Rs.Peek() >= 0)
                            {
                                LineCounter++;
                                t.Add(FillObjectsWithSpaces<T>(Rs.ReadLine()));
                            }
                        }
                        else
                        {
                            while (Rs.Peek() >= 0)
                            {
                                LineCounter++;
                                t.Add(FillObjects<T>(Rs.ReadLine()));
                            }
                        }
                    }
                }
                if (type == typeof(Object))
                {
                    return (IEnumerable<T>)d;
                }
                return t;
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Splits a line by a delimiter (set when Parser is created). Will ignore delimiter between quotes
        /// call using StreamReader or ReadStream if there may be line breaks in fields
        /// </summary>
        /// <param name="line">line</param>
        /// <returns>IEnumerable containging strings from a delimited line</returns>
        public IEnumerable<string> SplitLine(string line)
        {
            List<string> stringList = new List<string> { };
            bool inQuotes = false;
            char currentChar;
            int charsRead = 0;
            string substring;
            var rb = new StringBuilder();
            while (line.Length > charsRead)
            {
                currentChar = line[charsRead];
                charsRead++;
                if (currentChar == '\"')
                {
                    if (line.Length == charsRead)
                    {
                        inQuotes = false;
                        currentChar = Delimiter;
                    }
                    else if (line[charsRead] == Delimiter)
                    {
                        inQuotes = false;
                        continue;
                    }
                    else if (line[charsRead] == '\"')
                    {
                        continue;
                    }
                    else if (!inQuotes)
                    {
                        inQuotes = true;
                        continue;
                    }
                }
                if (charsRead == line.Length)
                {
                    if (currentChar == Delimiter)
                    {
                        substring = rb.ToString();
                        if (HasSpaces)
                        {
                            substring = substring.Trim(' ');
                        }
                        stringList.Add(substring);
                        rb.Clear();
                        rb.Append("");
                        substring = rb.ToString();
                        rb.Clear();
                        stringList.Add(substring);
                        continue;
                    }
                    rb.Append(currentChar);
                    substring = rb.ToString();
                    if (HasSpaces)
                    {
                        substring = substring.Trim(' ');
                    }
                    stringList.Add(substring);
                    rb.Clear();
                    continue;
                }
                if (currentChar == Delimiter && !inQuotes)
                {
                    substring = rb.ToString();
                    if (HasSpaces)
                    {
                        substring = substring.Trim(' ');
                    }
                    stringList.Add(substring);
                    rb.Clear();
                    continue;
                }
                rb.Append(currentChar);
            }
            return stringList;
        }
        private T FillDictionary<T>(string line)
        {
            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            Header = new Dictionary<int, PropertyInfo> { };
            try
            {
                List<string> substrings = new List<string> { };
                var splitLines = SplitLine(line);
                foreach (string s in splitLines)
                {
                    substrings.Add(s);
                }
                foreach (var propertyInfo in propertyInfos)
                {
                    int index = substrings.IndexOf(propertyInfo.Name);
                    if (index == -1)
                    {
                        string attValue = ((HeaderName)propertyInfo.GetCustomAttribute(typeof(HeaderName)))?.Name;
                        if (attValue == "")
                        {
                            continue;
                        }
                        else
                        {
                            index = substrings.IndexOf(attValue);
                        }
                    }
                    if (index == -1)
                    {
                        continue;
                    }
                    else
                    {
                        Header.Add(index, propertyInfo);
                    }
                }
                if (Header.Count == 0)
                {
                    int i = 0;
                    foreach (var propertyInfo in propertyInfos)
                    {
                        Header.Add(i, propertyInfo);
                        i++;

                    }
                    if (HasSpaces)
                    {
                        return FillObjectsWithSpaces<T>(line);
                    }
                    return FillObjects<T>(line);
                }
                return default(T);
            }
            catch
            {
                throw new Exception("Header line is not in correct format. Error on line: " + LineCounter);

            }
        }

        private T FillObjects<T>(string line)
        {
            var t = Activator.CreateInstance<T>();
            int length = line.Length;
            int charsRead = 0;
            int indexCounter = 0;
            bool inQuotes = false;
            char currentChar;
            var rb = new StringBuilder();
            try
            {
                while (length > charsRead)
                {
                    currentChar = line[charsRead];
                    charsRead++;
                    if (currentChar == '\"')
                    {
                        if (length == charsRead)
                        {
                            inQuotes = false;
                            currentChar = Delimiter;
                        }
                        else if (line[charsRead] == Delimiter)
                        {
                            inQuotes = false;
                            continue;
                        }
                        else if (line[charsRead] == '\"')
                        {
                            continue;
                        }
                        else if (!inQuotes)
                        {
                            inQuotes = true;
                            continue;
                        }
                    }
                    if (charsRead == length && currentChar != Delimiter)
                    {
                        rb.Append(currentChar);
                        currentChar = Delimiter;
                    }
                    if (currentChar == Delimiter && !inQuotes)
                    {
                        string substring = rb.ToString();
                        rb.Clear();
                        if (substring == "")
                        {
                            indexCounter++;
                            continue;
                        }
                        if (!Header.ContainsKey(indexCounter))
                        {
                            indexCounter++;
                            continue;
                        }
                        Info = Header[indexCounter];
                        Type type = Info.PropertyType;
                        if(type == typeof(string))
                        {
                            Info.SetValue(t, substring, null);
                            indexCounter++;
                            continue;
                        }
                        if (Nullable.GetUnderlyingType(type) != null)
                        {
                            type = Nullable.GetUnderlyingType(type);
                        }
                        if (type == typeof(bool))
                        {
                            if (substring.ToLower().Contains("t") || substring == "1")
                            {
                                substring = "true";
                            }
                            else
                            {
                                substring = "false";
                            }
                        }
                        if (type == typeof(decimal))
                        {
                            substring = decimal.Parse(substring, NumberStyles.Currency).ToString();
                        }
                        var value = StringToTypedValue(substring, type, CultureInfo.InvariantCulture);
                        Info.SetValue(t, value, null);
                        indexCounter++;
                        continue;
                    }
                    rb.Append(currentChar);
                }
                return t;
            }
            catch
            {
                throw new Exception("Error on line: " + LineCounter);
            }
        }
        private T FillObjectsWithSpaces<T>(string line)
        {
            var t = Activator.CreateInstance<T>();
            int length = line.Length;
            int charsRead = 0;
            int indexCounter = 0;
            bool inQuotes = false;
            char currentChar;
            var rb = new StringBuilder();
            try
            {
                while (length > charsRead)
                {
                    currentChar = line[charsRead];
                    charsRead++;
                    if (currentChar == '\"')
                    {
                        if (length == charsRead)
                        {
                            inQuotes = false;
                            currentChar = Delimiter;
                        }
                        else if (line[charsRead] == Delimiter)
                        {
                            inQuotes = false;
                            continue;
                        }
                        else if (line[charsRead] == '\"')
                        {
                            continue;
                        }
                        else if (!inQuotes)
                        {
                            inQuotes = true;
                            continue;
                        }
                    }
                    if (charsRead == length && currentChar != Delimiter)
                    {
                        rb.Append(currentChar);
                        currentChar = Delimiter;
                    }
                    if (currentChar == Delimiter && !inQuotes)
                    {
                        string substring = rb.ToString();
                        substring = substring.Trim(' ');
                        rb.Clear();
                        if (substring == "")
                        {
                            indexCounter++;
                            continue;
                        }
                        if (Header.ContainsKey(indexCounter))
                        {
                            Info = Header[indexCounter];
                        }
                        else
                        {
                            indexCounter++;
                            continue;
                        }

                        Type type = Info.PropertyType;
                        if (type == typeof(string))
                        {
                            Info.SetValue(t, substring, null);
                            indexCounter++;
                            continue;
                        }
                        if (Nullable.GetUnderlyingType(type) != null)
                        {
                            type = Nullable.GetUnderlyingType(type);
                        }
                        if (type == typeof(bool))
                        {
                            if (substring.ToLower().Contains("t") || substring == "1")
                            {
                                substring = "true";
                            }
                            else
                            {
                                substring = "false";
                            }
                        }
                        if (type == typeof(decimal))
                        {
                            substring = decimal.Parse(substring, NumberStyles.Currency).ToString();
                        }
                        var value = StringToTypedValue(substring, type, CultureInfo.InvariantCulture);
                        Info.SetValue(t, value, null);
                        indexCounter++;
                        continue;
                    }
                    rb.Append(currentChar);
                }
                return t;
            }
            catch
            {
                throw new Exception("Error on line: " + LineCounter);
            }
        }
        private void FillDictionaryNoHeader<T>()
        {
            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            Dictionary<int, PropertyInfo> indexProp = new Dictionary<int, PropertyInfo> { };
            try
            {
                foreach (var propertyInfo in propertyInfos)
                {
                    int? attValue = ((TxtCsvHelper.SetIndex)propertyInfo.GetCustomAttribute(typeof(TxtCsvHelper.SetIndex)))?.IndexNum;
                    if (attValue == null)
                    {
                        continue;
                    }
                    indexProp.Add(attValue.GetValueOrDefault(), propertyInfo);
                }
            }
            catch
            {
                throw new Exception("Model attributes missing or in incorrect format");
            }
            Header = indexProp;
        }

        private void FillDynamicDictionary(string line)
        {
            int i = 0;
            Dictionary<int, string> header = new Dictionary<int, string> { };
            try
            {
                var arr = SplitLine(line);
                foreach (string s in arr)
                {
                    header.Add(i, s.Trim(' '));
                    i++;
                }
            }
            catch
            {
                throw new Exception("Header line is not in correct format. Error on line: " + LineCounter);
            }
            DynamicHeader = header;
        }

        private ExpandoObject FillObjectsDynamic(string line)
        {
            dynamic t = new ExpandoObject();
            int length = line.Length;
            int charsRead = 0;
            int indexCounter = 0;
            bool inQuotes = false;
            char currentChar;
            var rb = new StringBuilder();
            try
            {
                while (length > charsRead)
                {
                    currentChar = line[charsRead];
                    charsRead++;
                    if (currentChar == '\"')
                    {
                        if (length == charsRead)
                        {
                            inQuotes = false;
                            currentChar = Delimiter;
                        }
                        else if (line[charsRead] == Delimiter)
                        {
                            inQuotes = false;
                            continue;
                        }
                        else if (line[charsRead] == '\"')
                        {
                            continue;
                        }
                        else if (!inQuotes)
                        {
                            inQuotes = true;
                            continue;
                        }
                    }
                    if (charsRead == length && currentChar != Delimiter)
                    {
                        rb.Append(currentChar);
                        currentChar = Delimiter;
                    }
                    if (currentChar == Delimiter && !inQuotes)
                    {
                        string substring = rb.ToString();
                        string propName;
                        rb.Clear();
                        if (substring == "")
                        {
                            indexCounter++;
                            continue;
                        }
                        if (DynamicHeader.ContainsKey(indexCounter))
                        {
                            propName = DynamicHeader[indexCounter];
                        }
                        else
                        {
                            indexCounter++;
                            continue;
                        }
                        ((IDictionary<string, object>)t).Add(propName, substring);
                        indexCounter++;
                        continue;
                    }
                    rb.Append(currentChar);
                }
                return t;
            }
            catch
            {
                throw new Exception("Error on line: " + LineCounter);
            }
        }

        private ExpandoObject FillObjectsDynamicWithSpaces(string line)
        {
            dynamic t = new ExpandoObject();
            int length = line.Length;
            int charsRead = 0;
            int indexCounter = 0;
            bool inQuotes = false;
            char currentChar;
            var rb = new StringBuilder();
            try
            {
                while (length > charsRead)
                {
                    currentChar = line[charsRead];
                    charsRead++;
                    if (currentChar == '\"')
                    {
                        if (length == charsRead)
                        {
                            inQuotes = false;
                            currentChar = Delimiter;
                        }
                        else if (line[charsRead] == Delimiter)
                        {
                            inQuotes = false;
                            continue;
                        }
                        else if (line[charsRead] == '\"')
                        {
                            continue;
                        }
                        else if (!inQuotes)
                        {
                            inQuotes = true;
                            continue;
                        }
                    }
                    if (charsRead == length && currentChar != Delimiter)
                    {
                        rb.Append(currentChar);
                        currentChar = Delimiter;
                    }
                    if (currentChar == Delimiter && !inQuotes)
                    {
                        string substring = rb.ToString();
                        string propName;
                        rb.Clear();
                        if (substring == "")
                        {
                            indexCounter++;
                            continue;
                        }
                        if (DynamicHeader.ContainsKey(indexCounter))
                        {
                            propName = DynamicHeader[indexCounter];
                        }
                        else
                        {
                            indexCounter++;
                            continue;
                        }
                            ((IDictionary<string, object>)t).Add(propName, substring);
                        indexCounter++;
                        continue;
                    }
                    rb.Append(currentChar);
                }
                return t;
            }
            catch
            {
                throw new Exception("Error on line: " + LineCounter);
            }
        }
        private object StringToTypedValue(string SourceString, Type TargetType, CultureInfo Culture)
        {
            object Result = null;
            System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(TargetType);

            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {

                Result = converter.ConvertFromString(null, Culture, SourceString);
            }
            return Result;

        }
    }
}
