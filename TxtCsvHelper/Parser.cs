using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TxtCsvHelper
{
    public class Parser : IDisposable
    {
        private readonly TextReader Reader;
        private readonly string Delimiter;
        private bool Disposed = false;
        private bool InQuotes;
        private bool QuotedField;
        private char[] buffer;
        private int bufferSize = 4096;
        private int charsRead;
        private int Position;
        private char CurrentChar;
        private int RowStartPos;
        private int FieldStart;
        private int FieldPos;
        private int Row;
        private Fields[] FieldArray;
        private Dictionary<int, PropertyInfo> Header;
        private readonly Dictionary<Type, IConverter> kvp;
        private PropertyInfo Info;
        private readonly string LineBreak;
        private readonly bool HasHeader;
        private Dictionary<int, string> DynamicHeader;
        private Dictionary<string, int> CachedHeader;
        private int CharCount;
        public List<string> Substrings
        {
            get
            {
                List<string> s = new List<string>();
                for (int i = 0; i < FieldPos; i++)
                {
                    s.Add(GetField(i));
                }
                return s;
            }
        }
        public string this[int index]
        {
            get
            {
                return GetField(index);
            }
        }
        /// <summary>
        /// Creates an instance of Parser with a comma as the delimiter and HasHeader set to true
        /// </summary>
        public Parser()
        {
            HasHeader = true;
            Delimiter = ",";
            LineBreak = Configuration.NewLine;
        }
        /// <summary>
        /// Creates an instance of Parser without a stream for use with SplitLine()
        /// </summary>
        /// <param name="delimiter">the delimiter</param>
        /// <param name="hasHeader">if the file has a header row</param>
        public Parser(string delimiter, bool hasHeader = true)
        {
            if (delimiter == null)
            {
                Delimiter = ",";
            }
            else
            {
                Delimiter = delimiter;
            }
            HasHeader = hasHeader;
            LineBreak = Configuration.NewLine;
            kvp = Converter.CreateConverters();
        }
        /// <summary>
        /// Creates an instance of Parser using a TextReader/StreamReader
        /// </summary>
        /// <param name="reader">streamreader</param>
        /// <param name="delimiter">delimiter</param>
        /// <param name="hasHeader">if the file has a header row</param>
        public Parser(TextReader reader, string delimiter = ",", bool hasHeader = true)
        {
            Reader = reader;
            Delimiter = delimiter;
            HasHeader = hasHeader;
            buffer = new char[bufferSize];
            LineBreak = Configuration.NewLine;
            FieldArray = new Fields[128];
            kvp = Converter.CreateConverters();
        }
        /// <summary>
        /// Creates an instance of Parser using a Stream
        /// </summary>
        /// <param name="reader">streamreader</param>
        /// <param name="delimiter">delimiter</param>
        /// <param name="hasHeader">if the file has a header row</param>
        public Parser(Stream stream, string delimiter = ",", bool hasHeader = true)
        {
            Reader = new StreamReader(stream);
            Delimiter = delimiter;
            HasHeader = hasHeader;
            buffer = new char[bufferSize];
            LineBreak = Configuration.NewLine;
            FieldArray = new Fields[128];
            kvp = Converter.CreateConverters();
        }
        /// <summary>
        /// Returns an IEnumerable of type T
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>iEnumerable of type T</returns>
        public IEnumerable<T> Deserialize<T>()
        {
            try
            {
                Type type = typeof(T);
                List<ExpandoObject> d = new List<ExpandoObject> { };
                List<T> t = new List<T> { };
                if (HasHeader)
                {
                    Read();
                    if (type == typeof(Object))
                    {
                        if (HasHeader)
                        {
                            Row++;
                            FillDynamicDictionary();
                        }
                    }
                    else
                    {
                        Row++;
                        var n = FillDictionary<T>();
                        if (n != null)
                        {
                            t.Add(n);
                        }
                    }
                }
                else
                {
                    FillDictionaryNoHeader<T>();
                }
                if (type == typeof(Object))
                {
                    while (Read())
                    {
                        Row++;
                        d.Add(FillObjectsDynamic());
                    }
                    return (IEnumerable<T>)d;
                }
                else
                {
                    while (Read())
                    {
                        Row++;
                        t.Add(FillObjects<T>());
                    }
                }
                return t;
            }
            finally
            {
                Dispose();
            }
        }

        private T FillDictionary<T>()
        {
            try
            {
                PropertyInfo[] propertyInfos = typeof(T).GetProperties();
                Header = new Dictionary<int, PropertyInfo> { };
                List<string> substrings = new List<string>();
                for (int i = 0; i < FieldArray.Length; i++)
                {
                    if (FieldArray[i].Start == 0 && FieldArray[i].Length == 0)
                    {
                        break;
                    }
                    substrings.Add(this[i]);
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
                    return FillObjects<T>();
                }
                return default(T);
            }
            catch
            {
                throw new Exception("Header line is not in correct format. Error on line: " + Row);
            }
        }
        private void FillDictionaryNoHeader<T>()
        {
            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            Header = new Dictionary<int, PropertyInfo> { };
            try
            {
                foreach (var propertyInfo in propertyInfos)
                {
                    int? attValue = ((TxtCsvHelper.SetIndex)propertyInfo.GetCustomAttribute(typeof(TxtCsvHelper.SetIndex)))?.IndexNum;
                    if (attValue == null)
                    {
                        continue;
                    }
                    Header.Add(attValue.GetValueOrDefault(), propertyInfo);
                }
            }
            catch
            {
                throw new Exception("Model attributes missing or in incorrect format");
            }
        }
        private void FillDynamicDictionary()
        {
            try
            {
                DynamicHeader = new Dictionary<int, string>();
                for (int i = 0; i < FieldArray.Length; i++)
                {
                    if (FieldArray[i].Length == 0)
                    {
                        break;
                    }
                    DynamicHeader.Add(i, this[i]);
                }
            }
            catch
            {
                throw new Exception("Header line is not in correct format. Error on line: " + Row);
            }
        }
        private T FillObjects<T>()
        {
            var t = Activator.CreateInstance<T>();
            foreach (var kvp in Header)
            {
                Info = kvp.Value;
                string substring = this[kvp.Key];
                if (substring == "")
                {
                    continue;
                }
                else if (Info.PropertyType == typeof(string))
                {
                    Info.SetValue(t, substring, null);
                    continue;
                }
                Type type = Info.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(type);
                type = underlyingType ?? type;
                try
                {
                    var value = TypeChanger(substring, type);
                    Info.SetValue(t, value, null);
                }
                catch
                {
                    throw new Exception("Error on line: " + Row + ". " + "Conversion from \"" + substring + "\" to " + type + " failed");
                }

            }
            return t;
        }
        private ExpandoObject FillObjectsDynamic()
        {
            try
            {
                ExpandoObject t = new ExpandoObject();
                foreach (var kvp in DynamicHeader)
                {
                    string propName = kvp.Value;
                    string substring = this[kvp.Key];
                    ((IDictionary<string, object>)t).Add(propName, substring);
                }
                return t;
            }
            catch
            {
                throw new Exception("Header line is not in correct format. Error on line: " + Row);
            }
        }
        /// <summary>
        /// Converts to a type from a string, check Conversion for supported type conversions
        /// </summary>
        /// <param name="input">string</param>
        /// <param name="t">Type</param>
        /// <returns>Object</returns>
        public object TypeChanger(string input, Type t)
        {
            var converter = kvp[t];
            var value = Converter.GetField(input, converter);
            return value;
        }
        /// <summary>
        /// Reads to line ending
        /// </summary>
        /// <returns>a Bool, false when reading is done</returns>
        public bool Read()
        {
            RowStartPos = Position;
            FieldStart = RowStartPos;
            CurrentChar = '\0';
            FieldPos = 0;
            while (true)
            {
                if (Position >= charsRead)
                {
                    if (!FillBuffer())
                    {
                        if (Position != 0)
                        {
                            return ReadEndOfFile();
                        }
                        return false;
                    }
                }

                if (ReadLine())
                {
                    return true;
                }

            }
        }
        /// <summary>
        /// Reads to line ending
        /// </summary>
        /// <returns>a Bool, false when reading is done</returns>
        public async Task<bool> ReadAsync()
        {
            RowStartPos = Position;
            FieldStart = RowStartPos;
            CurrentChar = '\0';
            FieldPos = 0;
            while (true)
            {
                if (Position >= charsRead)
                {
                    if (!await FillBufferAsync())
                    {
                        if (Position != 0)
                        {
                            return ReadEndOfFile();
                        }
                        return false;
                    }
                }

                if (ReadLine())
                {
                    return true;
                }

            }
        }
        private bool ReadLine()
        {
            CurrentChar = buffer[Position];
            Position++;
            CharCount++;
            if (CurrentChar == '\"')
            {
                if (Position >= buffer.Length)
                {
                    return false;
                }
                else if (buffer[Position] == Delimiter[0])
                {
                    InQuotes = false;
                }
                else if (buffer[Position] == LineBreak[0])
                {
                    int length = Position - (LineBreak.Length - 1) - FieldStart;
                    AddField(FieldStart, length);
                    Position += LineBreak.Length;
                    InQuotes = false;
                    return true;
                }
                else
                {
                    InQuotes = true;
                    QuotedField = true;
                    return false;
                }
            }
            if (InQuotes)
            {
                return false;
            }
            if (CurrentChar == LineBreak[0])
            {
                int length = Position - (LineBreak.Length - 1) - FieldStart;
                AddField(FieldStart, length);
                Position += LineBreak.Length - 1;
                return true;
            }
            if (CurrentChar == Delimiter[0])
            {
                if (ReadDelimiter())
                {
                    return true;
                }
            }
            return false;
        }
        private bool ReadDelimiter()
        {
            int length = Position - 1 - FieldStart;
            AddField(FieldStart, length);
            FieldStart = Position + (Delimiter.Length - 1);
            QuotedField = false;
            if (Position >= buffer.Length)
            {
                return false;
            }
            if (buffer[Position] == LineBreak[0])
            {
                AddField(FieldStart, 0);
                Position += LineBreak.Length;
                return true; ;
            }
            return false;
        }
        private bool ReadEndOfFile()
        {
            int i = 0;
            FieldPos = 0;
            FieldStart = 0;
            while (i < Position)
            {
                CurrentChar = buffer[i];
                i++;
                if (CurrentChar == '\"')
                {
                    if (i >= Position)
                    {
                        continue;
                    }
                    else if (buffer[i] == Delimiter[0])
                    {
                        InQuotes = false;
                    }
                    else
                    {
                        InQuotes = true;
                        QuotedField = true;
                        continue;
                    }
                }
                if (!InQuotes)
                {
                    if (CurrentChar == Delimiter[0])
                    {
                        AddField(FieldStart, i - FieldStart - 1);
                        FieldStart = i + (Delimiter.Length - 1);
                    }
                }
            }
            AddField(FieldStart, i - FieldStart);
            return true;
        }
        private void AddField(int start, int Length)
        {
            if (FieldPos > FieldArray.Length)
            {
                Array.Resize(ref FieldArray, FieldArray.Length * 2);
            }
            ref var Field = ref FieldArray[FieldPos];
            Field.Start = start - RowStartPos;
            Field.Length = Length;
            Field.Qouted = QuotedField;
            FieldPos++;
            QuotedField = false;

        }
        /// <summary>
        /// Gets the string at the specified index
        /// </summary>
        /// <param name="index">int Index</param>
        /// <returns>a string from the current line at the specified index</returns>
        public string GetField(int index)
        {
            if (index > FieldArray.Length)
            {
                throw new Exception("Index is out of Range");
            }
            var field = FieldArray[index];
            int start = field.Start + RowStartPos;
            var processedField = new ProcessedField(start, field.Length, buffer);
            string s = field.Qouted ? new string(processedField.Buffer, processedField.Start, processedField.Length).Trim('\"').Replace("\"\"", "\"") : new string(processedField.Buffer, processedField.Start, processedField.Length);
            return s;
        }
        /// <summary>
        /// Gets the object at the specified index and converts it to the given type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="index">int Index</param>
        /// <returns>an object of converted from a string at the specified index to T</returns>
        public T GetField<T>(int index)
        {
            Type type = typeof(T);
            string substring = this[index];
            if (substring == "")
            {
                return default(T);
            }
            var underlyingType = Nullable.GetUnderlyingType(type);
            type = underlyingType ?? type;
            var value = TypeChanger(substring, type);
            return (T)value;

        }
        /// <summary>
        /// Reads in the header row, must call this to get fields by column name
        /// </summary>
        /// <returns></returns>
        public bool CacheHeaderRow()
        {
            CachedHeader = new Dictionary<string, int>();
            for (int i = 0; i < FieldArray.Length; i++)
            {
                if (FieldArray[i].Start == 0 && FieldArray[i].Length == 0)
                {
                    break;
                }
                CachedHeader.Add(this[i], i);
            }
            return true;
        }
        /// <summary>
        /// Gets the object at the specified column name and converts it to the given type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="columnName">name of column</param>
        /// <returns>an object of converted from a string at the specified columb name to T</returns>
        public T GetField<T>(string columnName)
        {
            Type type = typeof(T);
            int index = CachedHeader[columnName];
            string substring = this[index];
            if (substring == "")
            {
                return default(T);
            }
            var underlyingType = Nullable.GetUnderlyingType(type);
            type = underlyingType ?? type;
            var value = TypeChanger(substring, type);
            return (T)value;

        }
        /// <summary>
        /// Gets the string of the current line at the specified column name
        /// </summary>
        /// <param name="columnName">name of column</param>
        /// <returns>a string from the current line in the specified column</returns>
        public string GetField(string columnName)
        {
            int index = CachedHeader[columnName];
            return this[index];
        }
        private bool FillBuffer()
        {
            if (RowStartPos > charsRead)
            {
                RowStartPos = 0;
            }
            if (RowStartPos == 0 && CharCount > 0 && charsRead == bufferSize)
            {
                // The record is longer than the memory buffer. Increase the buffer.
                bufferSize *= 2;
                var tempBuffer = new char[bufferSize];
                buffer.CopyTo(tempBuffer, 0);
                buffer = tempBuffer;
            }
            var charsLeft = Math.Max(charsRead - RowStartPos, 0);

            Array.Copy(buffer, RowStartPos, buffer, 0, charsLeft);
            FieldStart -= RowStartPos;
            RowStartPos = 0;
            Position = charsLeft;

            charsRead = Reader.Read(buffer, charsLeft, buffer.Length - charsLeft);

            if (charsRead == 0)
            {
                return false;
            }
            charsRead += charsLeft;

            return true;
        }
        private async Task<bool> FillBufferAsync()
        {
            if (RowStartPos > charsRead)
            {
                RowStartPos = 0;
            }
            if (RowStartPos == 0 && CharCount > 0 && charsRead == bufferSize)
            {
                // The record is longer than the memory buffer. Increase the buffer.
                bufferSize *= 2;
                var tempBuffer = new char[bufferSize];
                buffer.CopyTo(tempBuffer, 0);
                buffer = tempBuffer;
            }
            var charsLeft = Math.Max(charsRead - RowStartPos, 0);

            Array.Copy(buffer, RowStartPos, buffer, 0, charsLeft);
            FieldStart -= RowStartPos;
            RowStartPos = 0;
            Position = charsLeft;

            charsRead = await Reader.ReadAsync(buffer, charsLeft, buffer.Length - charsLeft);

            if (charsRead == 0)
            {
                return false;
            }
            charsRead += charsLeft;

            return true;
        }
        /// <summary>
        /// Splits the line at a delimiter
        /// </summary>
        /// <param name="line">a delimited string</param>
        /// <returns>IEnumerable of strings from the given line</returns>
        public IEnumerable<string> SplitLine(string line)
        {
            int numOfExtraQuotesDeep = 0;
            List<string> stringList = new List<string>();
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
                    //needs logic to add and subtract quotesdeep
                    numOfExtraQuotesDeep++;
                    if (line.Length == charsRead)
                    {
                        inQuotes = false;
                        currentChar = Delimiter[0];
                    }
                    else if (line[charsRead] == Delimiter[0])
                    {
                        //need quotes deep here
                        if (numOfExtraQuotesDeep % 2 == 0)
                        {
                            inQuotes = false;
                            charsRead += Delimiter.Length - 1;
                            continue;
                        }
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
                    //probably needs quotes deep here
                }
                if (charsRead == line.Length)
                {
                    if (currentChar == Delimiter[0])
                    {
                        substring = rb.ToString();
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
                    stringList.Add(substring);
                    rb.Clear();
                    continue;
                }
                if (currentChar == Delimiter[0] && !inQuotes)
                {
                    substring = rb.ToString();
                    stringList.Add(substring);
                    rb.Clear();
                    charsRead += Delimiter.Length - 1;
                    numOfExtraQuotesDeep = 0;
                    continue;
                }
                rb.Append(currentChar);
            }
            return stringList;
        }
        /// <summary>
        /// Splits the line at a given delimiter, use this if most lines may not contain quotes
        /// </summary>
        /// <param name="line">delimited line</param>
        /// <returns>IEnumerable of strings from the given line</returns>
        public IEnumerable<string> MixedSplit(string line)
        {
            if (line.Contains("\""))
            {
                return SplitLine(line);
            }
            return line.Split(Delimiter.ToCharArray());
        }
        /// <summary>
        /// Disposes of the Reader
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
                    if (Reader != null)
                    {
                        Reader.Dispose();
                    }
                }
            }
        }
        private struct Fields
        {
            public int Start;
            public int Length;
            public bool Qouted;
        }
        protected readonly ref struct ProcessedField
        {
            public readonly int Start;
            public readonly int Length;
            public readonly char[] Buffer;
            public ProcessedField(int start, int length, char[] buffer)
            {
                Start = start;
                Length = length;
                Buffer = buffer;
            }
        }
    }

}
