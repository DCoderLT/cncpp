using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace CCClasses.FileFormats.Binary {
    public class VXL : BinaryFileFormat {

        public class FileHeader {
            public const int Size = 32;

            public String id;
            public UInt32 PaletteCount;
            public UInt32 HeaderCount;
            public UInt32 TailerCount;
            public UInt32 BodySize;

            public bool ReadFile(ArraySegment<byte> input) {
                var offs = input.Offset;

                id = VXL.ReadCString(input, 16);

                PaletteCount = BitConverter.ToUInt32(input.Array, offs + 16);
                HeaderCount = BitConverter.ToUInt32(input.Array, offs + 20);
                TailerCount = BitConverter.ToUInt32(input.Array, offs + 24);
                BodySize = BitConverter.ToUInt32(input.Array, offs + 28);

                return true;
            }

        };

        public class SectionHeader {
            public const int Size = 28;
            public String id;
            public UInt32 LimbNumber;
            public UInt32 unknown1;
            public byte unknown2;

            public bool ReadFile(ArraySegment<byte> input) {
                var offs = input.Offset;
                
                id = VXL.ReadCString(input, 16);

                LimbNumber = BitConverter.ToUInt32(input.Array, offs + 16);
                unknown1 = BitConverter.ToUInt32(input.Array, offs + 20);
                unknown2 = input.Array[offs + 24];

                return true;
            }
        };

        public class Voxel {
            public byte X, Y;
            public byte Z;
            public byte ColorIndex;
            public byte NormalIndex;
        };

        public class SectionSpan {
            public byte X, Y;
            public int StartIndex;
            public int EndIndex;

            public byte Height;

            public List<Voxel> Voxels = new List<Voxel>();

            public int SpanLength {
                get {
                    return (EndIndex - StartIndex) + 1;
                }
            }

            public int ReadFile(ArraySegment<byte> r) {
                if (StartIndex == -1) {
                    Voxels.Clear();
                    return 0;
                }
                var offs = r.Offset + StartIndex;
                byte z = 0;
                while (z < Height) {
                    z += r.Array[offs];
                    ++offs;
                    var c = r.Array[offs];
                    ++offs;
                    for (var i = 0; i < c; ++i) {
                        var v = new Voxel();
                        v.X = X;
                        v.Y = Y;
                        v.Z = z;

                        v.ColorIndex = r.Array[offs];
                        ++offs;
                        v.NormalIndex = r.Array[offs];
                        ++offs;

                        ++z;

                        Voxels.Add(v);
                    }
                    ++offs;
                }
                return offs;
            }
        };

        public class SectionBody {
            public List<SectionSpan> Spans = new List<SectionSpan>();
        };

        public class TransfMatrix {
            public Vector4[] V = new Vector4[3];

            public void ReadFile(ArraySegment<byte> input, int offs) {
                for (var i = 0; i < 3; ++i) {
                    V[i].X = BitConverter.ToSingle(input.Array, offs + i * 16);
                    V[i].Y = BitConverter.ToSingle(input.Array, offs + i * 16 + 4);
                    V[i].Z = BitConverter.ToSingle(input.Array, offs + i * 16 + 8);
                    V[i].W = BitConverter.ToSingle(input.Array, offs + i * 16 + 12);
                }
            }
        };

        public class SectionTailer {
            public const int Size = 92;

            public UInt32 StartingSpanOffset;
            public UInt32 EndingSpanOffset;
            public UInt32 DataSpanOffset;
            public float HVAMultiplier;
            public TransfMatrix TM = new TransfMatrix();
            public Vector3 MinBounds = new Vector3();
            public Vector3 MaxBounds = new Vector3();
            public byte SizeX;
            public byte SizeY;
            public byte SizeZ;
            public byte NormalsMode;

            enum CornerTags {
                HHL = 0,
                HLL = 1,
                LLL = 2,
                LHL = 3,
                HHH = 4,
                HLH = 5,
                LLH = 6,
                LHH = 7
            };

            public Vector3[] Corners = new Vector3[8];

            public bool ReadFile(ArraySegment<byte> input) {
                var offs = input.Offset;

                StartingSpanOffset = BitConverter.ToUInt32(input.Array, offs);
                EndingSpanOffset = BitConverter.ToUInt32(input.Array, offs + 4);
                DataSpanOffset = BitConverter.ToUInt32(input.Array, offs + 8);
                HVAMultiplier = BitConverter.ToSingle(input.Array, offs + 12);
                TM.ReadFile(input, offs + 16);
                MinBounds.X = BitConverter.ToSingle(input.Array, offs + 64);
                MinBounds.Y = BitConverter.ToSingle(input.Array, offs + 68);
                MinBounds.Z = BitConverter.ToSingle(input.Array, offs + 72);
                MaxBounds.X = BitConverter.ToSingle(input.Array, offs + 76);
                MaxBounds.Y = BitConverter.ToSingle(input.Array, offs + 80);
                MaxBounds.Z = BitConverter.ToSingle(input.Array, offs + 84);

                SizeX = input.Array[offs + 88];
                SizeY = input.Array[offs + 89];
                SizeZ = input.Array[offs + 90];
                NormalsMode = input.Array[offs + 91];

                return true;
            }
        };

        public class Section {
            public SectionHeader Head = new SectionHeader();
            public SectionBody Body = new SectionBody();
            public SectionTailer Tail = new SectionTailer();

            internal void PrepareBody() {
                for (byte x = 0; x < Tail.SizeX; ++x) {
                    for (byte y = 0; y < Tail.SizeY; ++y) {
                        var s = new SectionSpan();
                        s.Height = Tail.SizeZ;
                        s.X = x;
                        s.Y = y;
                        Body.Spans.Add(s);
                    }
                }
            }

            internal void ReadFile(ArraySegment<byte> seg) {

                var offs = seg.Offset + (int)Tail.StartingSpanOffset;
                foreach (var s in Body.Spans) {
                    s.StartIndex = BitConverter.ToInt32(seg.Array, offs);
                    offs += 4;
                }

                offs = seg.Offset + (int)Tail.EndingSpanOffset;
                foreach (var s in Body.Spans) {
                    s.EndIndex = BitConverter.ToInt32(seg.Array, offs);
                    offs += 4;
                }

                offs = seg.Offset + (int)Tail.DataSpanOffset;
                foreach (var s in Body.Spans) {
                    var r = new ArraySegment<byte>(seg.Array, offs, 0);
                    s.ReadFile(r);
                }


            }
        };

        public FileHeader Header = new FileHeader();

        public List<PAL> Palettes = new List<PAL>();

        public List<Section> Sections = new List<Section>();

        public VXL(String filename = null) : base(filename) {
        }
        
        public override bool ReadFile(BinaryReader r, long length) {
            if (length < FileHeader.Size) {
                return false;
            }

            var bytes = r.ReadBytes((int)FileHeader.Size);
            var seg = new ArraySegment<byte>(bytes);

            Header.ReadFile(seg);

            if (Header.HeaderCount == 0 || Header.TailerCount == 0 || Header.TailerCount != Header.HeaderCount) {
                return false;
            }

            var skip = 770 * Header.PaletteCount;

            var hstart = (int)(skip + FileHeader.Size);
            var hlen = (int)(SectionHeader.Size * Header.HeaderCount);

            var tstart = (int)(hstart + hlen + Header.BodySize);
            var tlen = (int)(SectionTailer.Size * Header.TailerCount);

            if (tstart + tlen < length) {
                return false;
            }

            r.BaseStream.Seek(skip, SeekOrigin.Current);

            var hbytes = r.ReadBytes(hlen);

            for (var i = 0; i < Header.HeaderCount; ++i) {
                seg = new ArraySegment<byte>(hbytes, i * SectionHeader.Size, SectionHeader.Size);

                var S = new Section();

                Sections.Add(S);

                if (!S.Head.ReadFile(seg)) {
                    return false;
                }
            }

            var SectionBodyData = r.ReadBytes((int)Header.BodySize);

            var tbytes = r.ReadBytes(tlen);

            for (var i = 0; i < Header.TailerCount; ++i) {
                var S = Sections[i];
                seg = new ArraySegment<byte>(tbytes, i * SectionTailer.Size, SectionTailer.Size);

                if (!S.Tail.ReadFile(seg)) {
                    return false;
                }
            }

            seg = new ArraySegment<byte>(SectionBodyData);

            for (var i = 0; i < Header.TailerCount; ++i) {
                var S = Sections[i];

                S.PrepareBody();

                S.ReadFile(seg);
            }

            return true;
        }
    }
}
