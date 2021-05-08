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
        /// Allows you to set Delimiter equal to an character, indicate if there is a header row and/or there are spaces between delimiters and fields
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
        /// Instantiates a Parser using a Stream
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
        /// Instantiates a Parser with a FileStream as a Parameter
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
        /// Instantiates a Parser with a ReadStream as a Parameter, ReadStream is nearly identical to StreamReader but it allows for line breaks in fields
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
        /// Instantiates a Parser with a MemoryStream as a Parameter
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
        /// Instantiates a Parser with a string as a Parameter, converts the string to a MemoryStream to utilize ReadStream
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
                    if (Sw != null)
                    {
                        Sw.Dispose();
                        Ms.Dispose();
                    }
                    if (Rs != null)
                    {
                        Rs.Dispose();
                    }
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
        /// Returns an IEnumerable of objects
        /// </summary>
        /// <typeparam name="T">Takes any object as a type</typeparam>
        /// <returns>IEnumerable of type T</returns>
        public IEnumerable<T> Deserialize<T>()
        {
            try
            {
                List<T> t = new List<T> { };
                using (Rs)
                {
                    if (HasHeader)
                    {
                        LineCounter++;
                        FillDictionary<T>(Rs.ReadLine());
                    }
                    else
                    {
                        FillDictionaryNoHeader<T>();
                    }
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
                return t;
            }
            finally
            {
                Dispose();
            }
        }
       
        /// <summary>
        /// Splits a line by a delimiter (set when Parser is created). Will ignore delimiter between quotes
        /// call using ReadStream
        /// </summary>
        /// <param name="line">line</param>
        /// <returns>IEnumerable containging strings from a delimited line</returns>
        public IEnumerable<string> SplitLine(string line)
        {
            try
            {
                List<string> stringList = new List<string> { };
                bool inQuotes = false;
                char currentChar;
                int charsRead = 0;
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
                        else
                        {
                            inQuotes = true;
                            continue;
                        }
                    }
                    if (charsRead == line.Length && currentChar != Delimiter)
                    {
                        rb.Append(currentChar);
                        currentChar = Delimiter;
                    }
                    if (currentChar == Delimiter && !inQuotes)
                    {
                        string substring = rb.ToString();
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
            finally
            {
                Dispose();
            }
        }
        private void FillDictionary<T>(string line)
        {
            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            Dictionary<int, PropertyInfo> indexProp = new Dictionary<int, PropertyInfo> { };
            try
            {
                List<string> substrings = new List<string> { }; ;
                if (HasSpaces)
                {
                    var arr = line.Split(Delimiter);
                    foreach (string s in arr)
                    {
                        substrings.Add(s.Trim(' '));
                    }
                }
                else
                {
                    var arr = line.Split(Delimiter);
                    foreach (string s in arr)
                    {
                        substrings.Add(s);
                    }
                }
                foreach (var propertyInfo in propertyInfos)
                {
                    int index = substrings.IndexOf(propertyInfo.Name);
                    if (index == -1)
                    {
                        continue;
                    }
                    indexProp.Add(index, propertyInfo);
                }
            }
            catch
            {
                throw new Exception("Header line is not in correct format. Error on line: " + LineCounter);
            }
            Header = indexProp;
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
                        else
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
                        if (rb == null)
                        {
                            indexCounter++;
                            rb.Clear();
                            continue;
                        }
                        string substring = rb.ToString();
                        rb.Clear();
                        if (!Header.ContainsKey(indexCounter))
                        {
                            indexCounter++;
                            continue;
                        }
                        Info = Header[indexCounter];
                        Type type = Info.PropertyType;
                        if (Nullable.GetUnderlyingType(type) != null)
                        {
                            type = Nullable.GetUnderlyingType(type);
                        }
                        if (type == typeof(bool))
                        {
                            if (substring.ToLower().Contains("t") || substring != "0")
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
                        Info.SetValue(t, Convert.ChangeType(substring, type), null);
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
                        else
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
                        if (rb == null)
                        {
                            indexCounter++;
                            rb.Clear();
                            continue;
                        }
                        string substring = rb.ToString();
                        substring = substring.Trim(' ');
                        rb.Clear();
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
                        if (Nullable.GetUnderlyingType(type) != null)
                        {
                            type = Nullable.GetUnderlyingType(type);
                        }
                        if (type == inQuotes.GetType())
                        {
                            if (substring.ToLower().Contains("t") || substring != "0")
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
                        Info.SetValue(t, Convert.ChangeType(substring, type), null);
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
                    int? attValue = ((SetIndex)propertyInfo.GetCustomAttribute(typeof(SetIndex)))?.IndexNum;
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
                var arr = line.Split(Delimiter);
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
                        else
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
                        if (rb == null)
                        {
                            indexCounter++;
                            rb.Clear();
                            continue;
                        }
                        string substring = rb.ToString();
                        string propName;
                        rb.Clear();
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
                        else
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
                        if (rb == null)
                        {
                            indexCounter++;
                            rb.Clear();
                            continue;
                        }
                        string substring = rb.ToString();
                        substring = substring.Trim(' ');
                        string propName;
                        rb.Clear();
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

        /// <summary>
        /// Returns an IEnumerable of dynamic objects, must have a header row
        /// </summary>
        /// <returns>IEnumerable of type dynamic</returns>
        public IEnumerable<ExpandoObject> Deserialize()
        {
            try
            {
                List<ExpandoObject> d = new List<ExpandoObject> { };
                using (Rs)
                {
                    LineCounter++;
                    FillDynamicDictionary(Rs.ReadLine());
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
                return d;
            }
            finally
            {
                Dispose();
            }
        }
    }
}
