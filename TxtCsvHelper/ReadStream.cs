using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;

namespace TxtCsvHelper
{
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ReadStream : TextReader
    {
        public new static readonly ReadStream Null = new NullReadStream();

        internal static int DefaultBufferSize
        {
            get
            {
                return 1024;
            }
        }

        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;

        private Stream stream;
        private Encoding encoding;
        private Decoder decoder;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private byte[] _preamble;
        private int charPos;
        private int charLen;
        private int byteLen;
        private int bytePos;
        private bool InQuotes;
        private int _maxCharsPerBuffer;
        private bool _detectEncoding;
        private bool _checkPreamble;
        private bool _isBlocked;
        private bool _closable;
        internal ReadStream()
        {
        }

        public ReadStream(Stream stream)
            : this(stream, true)
        {
        }

        public ReadStream(Stream stream, bool detectEncodingFromByteOrderMarks)
            : this(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }

        public ReadStream(Stream stream, Encoding encoding)
            : this(stream, encoding, true, DefaultBufferSize, false)
        {
        }

        public ReadStream(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }
        public ReadStream(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false)
        {
        }

        public ReadStream(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            if (stream == null || encoding == null)
                throw new ArgumentNullException((stream == null ? "stream" : "encoding"));
            if (!stream.CanRead)
                throw new ArgumentException("Argument_StreamNotReadable");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", "ArgumentOutOfRange_NeedPosNum");
            Contract.EndContractBlock();

            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public ReadStream(String path)
            : this(path, true)
        {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public ReadStream(String path, bool detectEncodingFromByteOrderMarks)
            : this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public ReadStream(String path, Encoding encoding)
            : this(path, encoding, true, DefaultBufferSize)
        {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public ReadStream(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : this(path, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public ReadStream(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
            : this(path, encoding, detectEncodingFromByteOrderMarks, bufferSize, true)
        {
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal ReadStream(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool checkHost)
        {
            if (path == null || encoding == null)
                throw new ArgumentNullException((path == null ? "path" : "encoding"));
            if (path.Length == 0)
                throw new ArgumentException("Argument_EmptyPath");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", "ArgumentOutOfRange_NeedPosNum");
            Contract.EndContractBlock();

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.SequentialScan);
            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false);
        }

        private void Init(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            this.stream = stream;
            this.encoding = encoding;
            decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize) bufferSize = MinBufferSize;
            byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            charBuffer = new char[_maxCharsPerBuffer];
            byteLen = 0;
            bytePos = 0;
            _detectEncoding = detectEncodingFromByteOrderMarks;
            _preamble = encoding.GetPreamble();
            _checkPreamble = (_preamble.Length > 0);
            _isBlocked = false;
            _closable = !leaveOpen;
        }
        internal void Init(Stream stream)
        {
            this.stream = stream;
            _closable = true;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!LeaveOpen && disposing && (stream != null))
                    stream.Close();
            }
            finally
            {
                if (!LeaveOpen && (stream != null))
                {
                    stream = null;
                    encoding = null;
                    decoder = null;
                    byteBuffer = null;
                    charBuffer = null;
                    charPos = 0;
                    charLen = 0;
                    base.Dispose(disposing);
                }
            }
        }

        public virtual Encoding CurrentEncoding
        {
            get { return encoding; }
        }

        public virtual Stream BaseStream
        {
            get { return stream; }
        }

        internal bool LeaveOpen
        {
            get { return !_closable; }
        }
        public void DiscardBufferedData()
        {
            byteLen = 0;
            charLen = 0;
            charPos = 0;
            if (encoding != null)
            {
                decoder = encoding.GetDecoder();
            }
            _isBlocked = false;
        }

        public bool EndOfStream
        {
            get
            {
                if (stream == null)
                    throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");
                if (charPos < charLen)
                    return false;

                int numRead = ReadBuffer();
                return numRead == 0;
            }
        }

        [Pure]
        public override int Peek()
        {
            if (stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            if (charPos == charLen)
            {
                if (_isBlocked || ReadBuffer() == 0) return -1;
            }
            return charBuffer[charPos];
        }

        public override int Read()
        {
            if (stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            if (charPos == charLen)
            {
                if (ReadBuffer() == 0) return -1;
            }
            int result = charBuffer[charPos];
            charPos++;
            return result;
        }

        public override int Read([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", "ArgumentNull_Buffer");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), "ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - index < count)
                throw new ArgumentException("Argument_InvalidOffLen");
            Contract.EndContractBlock();

            if (stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            int charsRead = 0;
            bool readToUserBuffer = false;
            while (count > 0)
            {
                int n = charLen - charPos;
                if (n == 0) n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);
                if (n == 0) break;
                if (n > count) n = count;
                if (!readToUserBuffer)
                {
                    Buffer.BlockCopy(charBuffer, charPos * 2, buffer, (index + charsRead) * 2, n * 2);
                    charPos += n;
                }
                charsRead += n;
                count -= n;
                if (_isBlocked)
                    break;
            }

            return charsRead;
        }

        public override String ReadToEnd()
        {
            if (stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            StringBuilder sb = new StringBuilder(charLen - charPos);
            do
            {
                sb.Append(charBuffer, charPos, charLen - charPos);
                charPos = charLen;
                ReadBuffer();
            } while (charLen > 0);
            return sb.ToString();
        }

        public override int ReadBlock([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", "ArgumentNull_Buffer");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), "ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - index < count)
                throw new ArgumentException("Argument_InvalidOffLen");
            Contract.EndContractBlock();

            if (stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");


            return base.ReadBlock(buffer, index, count);
        }
        private void CompressBuffer(int n)
        {
            Contract.Assert(byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this ReadStream at the same time?");
            Buffer.BlockCopy(byteBuffer, n, byteBuffer, 0, byteLen - n);
            byteLen -= n;
        }

        private void DetectEncoding()
        {
            if (byteLen < 2)
                return;
            _detectEncoding = false;
            bool changedEncoding = false;
            if (byteBuffer[0] == 0xFE && byteBuffer[1] == 0xFF)
            {
                encoding = new UnicodeEncoding(true, true);
                CompressBuffer(2);
                changedEncoding = true;
            }

            else if (byteBuffer[0] == 0xFF && byteBuffer[1] == 0xFE)
            {
                if (byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
                {
                    encoding = new UnicodeEncoding(false, true);
                    CompressBuffer(2);
                    changedEncoding = true;
                }
            }

            else if (byteLen >= 3 && byteBuffer[0] == 0xEF && byteBuffer[1] == 0xBB && byteBuffer[2] == 0xBF)
            {
                encoding = Encoding.UTF8;
                CompressBuffer(3);
                changedEncoding = true;
            }
            else if (byteLen == 2)
                _detectEncoding = true;

            if (changedEncoding)
            {
                decoder = encoding.GetDecoder();
                _maxCharsPerBuffer = encoding.GetMaxCharCount(byteBuffer.Length);
                charBuffer = new char[_maxCharsPerBuffer];
            }
        }
        private bool IsPreamble()
        {
            if (!_checkPreamble)
                return _checkPreamble;

            Contract.Assert(bytePos <= _preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this ReadStream at the same time?");
            int len = (byteLen >= (_preamble.Length)) ? (_preamble.Length - bytePos) : (byteLen - bytePos);

            for (int i = 0; i < len; i++, bytePos++)
            {
                if (byteBuffer[bytePos] != _preamble[bytePos])
                {
                    bytePos = 0;
                    _checkPreamble = false;
                    break;
                }
            }

            Contract.Assert(bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this ReadStream at the same time?");

            if (_checkPreamble)
            {
                if (bytePos == _preamble.Length)
                {
                    CompressBuffer(_preamble.Length);
                    bytePos = 0;
                    _checkPreamble = false;
                    _detectEncoding = false;
                }
            }

            return _checkPreamble;
        }

        internal virtual int ReadBuffer()
        {
            charLen = 0;
            charPos = 0;

            if (!_checkPreamble)
                byteLen = 0;
            do
            {
                if (_checkPreamble)
                {
                    Contract.Assert(bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this ReadStream at the same time?");
                    int len = stream.Read(byteBuffer, bytePos, byteBuffer.Length - bytePos);
                    Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        if (byteLen > 0)
                        {
                            charLen += decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charLen);
                            bytePos = byteLen = 0;
                        }

                        return charLen;
                    }

                    byteLen += len;
                }
                else
                {
                    Contract.Assert(bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this ReadStream at the same time?");
                    byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);
                    Contract.Assert(byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (byteLen == 0)
                        return charLen;
                }

                _isBlocked = (byteLen < byteBuffer.Length);

                if (IsPreamble())
                    continue;

                if (_detectEncoding && byteLen >= 2)
                    DetectEncoding();

                charLen += decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charLen);
            } while (charLen == 0);
            return charLen;
        }

        private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
        {
            charLen = 0;
            charPos = 0;

            if (!_checkPreamble)
                byteLen = 0;

            int charsRead = 0;
            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

            do
            {
                Contract.Assert(charsRead == 0);

                if (_checkPreamble)
                {
                    Contract.Assert(bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this ReadStream at the same time?");
                    int len = stream.Read(byteBuffer, bytePos, byteBuffer.Length - bytePos);
                    Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        if (byteLen > 0)
                        {
                            if (readToUserBuffer)
                            {
                                charsRead = decoder.GetChars(byteBuffer, 0, byteLen, userBuffer, userOffset + charsRead);
                                charLen = 0;
                            }
                            else
                            {
                                charsRead = decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charsRead);
                                charLen += charsRead;
                            }
                        }

                        return charsRead;
                    }

                    byteLen += len;
                }
                else
                {
                    Contract.Assert(bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this ReadStream at the same time?");

                    byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);

                    Contract.Assert(byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (byteLen == 0)
                        break;
                }

                _isBlocked = (byteLen < byteBuffer.Length);

                if (IsPreamble())
                    continue;

                if (_detectEncoding && byteLen >= 2)
                {
                    DetectEncoding();
                    readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                }

                charPos = 0;
                if (readToUserBuffer)
                {
                    charsRead += decoder.GetChars(byteBuffer, 0, byteLen, userBuffer, userOffset + charsRead);
                    charLen = 0;
                }
                else
                {
                    charsRead = decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charsRead);
                    charLen += charsRead;
                }
            } while (charsRead == 0);

            _isBlocked &= charsRead < desiredChars;
            return charsRead;
        }
        public override String ReadLine()
        {
            if (stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");


            if (charPos == charLen)
            {
                if (ReadBuffer() == 0) return null;
            }
            InQuotes = false;
            StringBuilder sb = null;
            do
            {
                int i = charPos;
                do
                {
                    char ch = charBuffer[i];
                    if (ch == '\"')
                    {
                        InQuotes = !InQuotes;
                    }
                    if ((ch == '\r' || ch == '\n') && !InQuotes)
                    {
                        String s;
                        if (sb != null)
                        {
                            sb.Append(charBuffer, charPos, i - charPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new String(charBuffer, charPos, i - charPos);
                        }
                        charPos = i + 1;
                        if (ch == '\r' && (charPos < charLen || ReadBuffer() > 0))
                        {
                            if (charBuffer[charPos] == '\n') charPos++;
                        }
                        return s;
                    }
                    i++;
                } while (i < charLen);
                i = charLen - charPos;
                if (sb == null) sb = new StringBuilder(i + 80);
                sb.Append(charBuffer, charPos, i);
            } while (ReadBuffer() > 0);
            return sb.ToString();
        }
        private class NullReadStream : ReadStream
        {
            internal NullReadStream()
            {
                Init(Stream.Null);
            }

            public override Stream BaseStream
            {
                get { return Stream.Null; }
            }

            public override Encoding CurrentEncoding
            {
                get { return Encoding.Unicode; }
            }

            protected override void Dispose(bool disposing)
            {

            }

            public override int Peek()
            {
                return -1;
            }

            public override int Read()
            {
                return -1;
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]
            public override int Read(char[] buffer, int index, int count)
            {
                return 0;
            }

            public override String ReadLine()
            {
                return null;
            }

            public override String ReadToEnd()
            {
                return String.Empty;
            }

            internal override int ReadBuffer()
            {
                return 0;
            }

        }
    }
}

