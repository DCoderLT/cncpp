using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CCClasses {
    public class FileParserException : FormatException {
    }

    public class FileFormat {
        public String Filename;

        public virtual bool ReadFile(CCFileClass ccFile) {
            return true;
        }

        protected List<String> ParserErrors = new List<string>();
        public List<String> errors {
            get {
                return ParserErrors;
            }
        }
        protected void ParseError(String message, params object[] args) {
            ParserErrors.Add(String.Format(message, args));
            throw new FileParserException();
        }
    }

    public class TextFileFormat : FileFormat {
        protected virtual bool ReadFile(StreamReader r) {
            return true;
        }

        public TextFileFormat(CCFileClass ccFile = null) {
            if (ccFile != null && ccFile.Exists) {
                Filename = ccFile.Filename;
                ParserErrors.Clear();
                try {
                    if (!ReadFile(ccFile.TextStream)) {
                        throw new ArgumentException();
                    }
                } catch (FileParserException E) {
                    throw new ArgumentException(String.Join("\n", errors), E);
                }
            }
        }
    }

    public class BinaryFileFormat : FileFormat {
        protected virtual bool ReadFile(BinaryReader r) {
            return true;
        }

        public BinaryFileFormat(CCFileClass ccFile = null) {
            if (ccFile != null && ccFile.Exists) {
                Filename = ccFile.Filename;
                ParserErrors.Clear();
                if (!ReadFile(ccFile.BinaryStream)) {
                    throw new ArgumentException(String.Format("File {0} could not be loaded.", ccFile.Filename));
                }
            }
        }

        public static String ReadCString(BinaryReader r, uint len) {
            String s = "";
            bool read = true;
            for (var i = 0u; i < len; ++i) {
                char c = r.ReadChar();
                if (c != 0) {
                    if (read) {
                        s += c;
                    }
                } else {
                    read = false;
                }
            }
            return s;
        }

        public static String ReadCString(ArraySegment<byte> r, uint len, int offset = -1) {
            String s = "";
            bool read = true;
            if (offset == -1) {
                offset = r.Offset;
            }
            for (var i = 0u; i < len; ++i) {
                char c = (char)r.Array[offset + i];
                if (c != 0) {
                    if (read) {
                        s += c;
                    }
                } else {
                    read = false;
                }
            }
            return s;
        }
    }

    public class CCFileClass : IDisposable {
        public String Filename;

        private Stream _Contents;
        public Stream Contents {
            get {
                return _Contents;
            }
        }

        public bool Exists {
            get {
                return _Contents != null;
            }
        }

        public CCFileClass(String fname) {
            Filename = fname;
        }

        public CCFileClass(String fname, Stream contents) {
            Filename = fname;
            _Contents = contents;
        }

        public void Reset() {
            if (_sreader != null) {
                _sreader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
            if (_breader != null) {
                _breader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
        }

        private StreamReader _sreader;
        private BinaryReader _breader;

        public StreamReader TextStream {
            get {
                if (_sreader == null) {
                    _sreader = new StreamReader(_Contents);
                }
                return _sreader;
            }
        }

        public BinaryReader BinaryStream {
            get {
                if (_breader == null) {
                    _breader = new BinaryReader(_Contents);
                }
                return _breader;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    var disposedStream = false;
                    if (_sreader != null) {
                        _sreader.Dispose();
                        disposedStream = true;
                    }
                    if (_breader != null) {
                        _breader.Dispose();
                        disposedStream = true;
                    }

                    if (_Contents != null && !disposedStream) {
                        _Contents.Dispose();
                    }
                 }
                _sreader = null;
                _breader = null;
                _Contents = null;
                _disposed = true;
            }
        }

        ~CCFileClass() {
            Dispose(false);
        }
    }
}
