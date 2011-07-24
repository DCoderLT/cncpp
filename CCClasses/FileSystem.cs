using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CCClasses.FileFormats.Binary;

namespace CCClasses {
    public class FileSystem {
        public static String MainDir = "";

        public static Stream LoadFile(String filename) {
            foreach (var M in MIX.LoadedMIXes) {
                if (M.ContainsFile(filename)) {
                    return M.GetFileContents(filename);
                }
            }

            var loose = MainDir + filename;
            if (File.Exists(loose)) {
                return new FileStream(loose, FileMode.Open);
            }

            return null;
        }

        public static bool LoadMIX(String filename) {
            foreach (var M in MIX.LoadedMIXes) {
                if (M.ContainsFile(filename)) {
                    var X = new MIX();
                    using (var r = new BinaryReader(M.GetFileContents(filename))) {
                        X.ReadFile(r, r.BaseStream.Length);
                    }
                    MIX.LoadedMIXes.Insert(0, X);
                    return true;
                }
            }

            var loose = MainDir + filename;
            if (!File.Exists(loose)) {
                return false;
            }

            var MX = new MIX(loose);
            MIX.LoadedMIXes.Insert(0, MX);

            return true;
        }
    }
}
