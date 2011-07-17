using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace CCClasses.FileFormats.Binary {
    public class HVA : BinaryFileFormat {

        public class FileHeader {
            public String id;
            public UInt32 FrameCount;
            public UInt32 SectionCount;

            public bool ReadFile(BinaryReader r) {
                id = HVA.ReadCString(r, 16);

                FrameCount = r.ReadUInt32();
                SectionCount = r.ReadUInt32();

                return true;
            }

        };

        public class TMatrix {
            public Vector4[] TM = new Vector4[3];
        }

        public class Section {
            public String id;
            public TMatrix[] T;

            public Section(uint c) {
                T = new TMatrix[c];
                for (var i = 0u; i < c; ++i) {
                    T[i] = new TMatrix();
                }
            }

            public void ReadID(BinaryReader r) {
                id = HVA.ReadCString(r, 16);
            }

            public void ReadMatrix(BinaryReader r) {
                foreach (var M in T) {
                    for (var i = 0; i < 3; ++i) {
                        M.TM[i].X = r.ReadSingle();
                        M.TM[i].Y = r.ReadSingle();
                        M.TM[i].Z = r.ReadSingle();
                        M.TM[i].W = r.ReadSingle();
                    }
                }
            }
        }

        public HVA(String filename = null) : base(filename) {
        }

        public FileHeader Header = new FileHeader();
        public List<Section> Sections = new List<Section>();

        public override bool ReadFile(BinaryReader r, long length) {
            if (length < 24) {
                return false;
            }

            if (!Header.ReadFile(r)) {
                return false;
            }

            if(length < 24 + (48 * Header.FrameCount + 16) * Header.SectionCount) {
                return false;
            }

            for (var i = 0; i < Header.SectionCount; ++i) {
                var s = new Section(Header.FrameCount);

                s.ReadID(r);

                Sections.Add(s);
            }

            foreach (var s in Sections) {
                s.ReadMatrix(r);
            }

            return true;
        }
    }
}
