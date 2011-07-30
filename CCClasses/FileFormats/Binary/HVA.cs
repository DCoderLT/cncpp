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

        public class Section {
            public String id;
            public Matrix[] T;

            public Section(uint c) {
                T = new Matrix[c];
                for (var i = 0u; i < c; ++i) {
                    T[i] = new Matrix();
                }
            }

            public int ByteSize {
                get {
                    return T.Length * 48;
                }
            }

            public void ReadID(ArraySegment<byte> s) {
                id = HVA.ReadCString(s, 16);
            }

            public void ReadMatrix(ArraySegment<byte> s, int frameIdx) {
                var offs = s.Offset;
                var M = T[frameIdx];
                M.M11 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M12 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M13 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M14 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;

                M.M21 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M22 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M23 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M24 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;

                M.M31 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M32 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M33 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;
                M.M34 = BitConverter.ToSingle(s.Array, offs);
                offs += 4;

                T[frameIdx] = M;
            }

            public Quaternion GetRotation(int frameIdx) {
                var M = T[frameIdx];
                var scale = new Vector3();
                var rot = new Quaternion();
                var transl = new Vector3();
                
                M.Decompose(out scale, out rot, out transl);

                return rot;
            }

            public Vector3 GetPosition(int frameIdx) {
                var pos = new Vector3();

                var FM = T[frameIdx];

                pos.X = FM.M14;
                pos.Y = FM.M24;
                pos.Z = FM.M34;

                return pos;
            }
        }

        public HVA(CCFileClass ccFile = null) : base(ccFile) {
        }

        public FileHeader Header = new FileHeader();
        public List<Section> Sections = new List<Section>();

        protected override bool ReadFile(BinaryReader r) {
            var length = (int)r.BaseStream.Length;
            if (length < 24) {
                return false;
            }

            if (!Header.ReadFile(r)) {
                return false;
            }

            if(length < 24 + (48 * Header.FrameCount + 16) * Header.SectionCount) {
                return false;
            }

            var data = r.ReadBytes(16 * (int)Header.SectionCount);

            var sectionSize = 0;

            for (var i = 0; i < Header.SectionCount; ++i) {
                var s = new Section(Header.FrameCount);

                var seg = new ArraySegment<byte>(data, i * 16, 16);
                s.ReadID(seg);

                Sections.Add(s);
                sectionSize = s.ByteSize;
            }

            data = r.ReadBytes(sectionSize * (int)Header.SectionCount);

            //for (var j = 0; j < Header.SectionCount; ++j) {
            //    for (var i = 0; i < Header.FrameCount; ++i) {
            //            var s = Sections[j];
            //        var seg = new ArraySegment<byte>(data, (j * (int)Header.FrameCount + i) * 48, 48);

            //        s.ReadMatrix(seg, i);
            //    }
            //}

            for (var i = 0; i < Header.FrameCount; ++i) {
                for (var j = 0; j < Header.SectionCount; ++j) {
                    var s = Sections[j];
                    var seg = new ArraySegment<byte>(data, (i * (int)Header.SectionCount + j) * 48, 48);

                    s.ReadMatrix(seg, i);
                }
            }

            return true;
        }
    }
}
