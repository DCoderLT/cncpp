using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CCClasses {
    public class IFileFormat {
        public virtual bool ReadFile(String filename) {
            return File.Exists(filename);
        }
    }

    public class TextFileFormat : IFileFormat {
        public override bool ReadFile(String filename) {
            if (!base.ReadFile(filename)) {
                return false;
            }
            using (var s = File.Open(filename, FileMode.Open)) {
                using (var r = new StreamReader(s)) {
                    return ReadFile(r);
                }
            }
        }

        public virtual bool ReadFile(StreamReader r) {
            return true;
        }

        public TextFileFormat(String filename = null) {
            if (filename != null) {
                if (!ReadFile(filename)) {
                    throw new ArgumentException();
                }
            }
        }
}

    public class BinaryFileFormat : IFileFormat {
        public override bool ReadFile(String filename) {
            if (!base.ReadFile(filename)) {
                return false;
            }
            using (FileStream s = File.Open(filename, FileMode.Open)) {
                using (BinaryReader r = new BinaryReader(s)) {
                    return ReadFile(r, s.Length);
                }
            }
        }

        public virtual bool ReadFile(BinaryReader r, long length) {
            return true;
        }

        public BinaryFileFormat(String filename = null) {
            if (filename != null) {
                if (!ReadFile(filename)) {
                    throw new ArgumentException();
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

        public static String ReadCString(ArraySegment<byte> r, uint len) {
            String s = "";
            bool read = true;
            var offset = r.Offset;
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
}
