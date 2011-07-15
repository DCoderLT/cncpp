using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CCClasses {
    public class IFileFormat {
        public virtual bool ReadFile(String filename) {
            return true;
        }
    }

    public class BinaryFileFormat : IFileFormat {
        public override bool ReadFile(String filename) {
            if (!File.Exists(filename)) {
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
    }
}
