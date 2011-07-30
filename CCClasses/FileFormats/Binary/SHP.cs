using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework;

namespace CCClasses.FileFormats.Binary {
    public class SHP : BinaryFileFormat {
        public class FileHeader {
            public int zero;
            public uint Width;
            public uint Height;
            public uint FrameCount;

            public bool Read(ArraySegment<byte> input) {
                var data = input.Array;
                var ofs = input.Offset;
                zero = BitConverter.ToInt16(data, ofs + 0);
                Width = BitConverter.ToUInt16(data, ofs + 2);
                Height = BitConverter.ToUInt16(data, ofs + 4);
                FrameCount = BitConverter.ToUInt16(data, ofs + 6);
                return true;
            }
        }

        public class FrameHeader {
            public uint X, Y, Width, Height, Compression, Unknown, Zero, Offset;
            public byte[] ProcessedBytes;

            public uint Length {
                get {
                    return Width * Height;
                }
            }

            public bool Read(ArraySegment<byte> input) {
                var data = input.Array;
                var ofs = input.Offset;
                X = BitConverter.ToUInt16(data, ofs + 0);
                Y = BitConverter.ToUInt16(data, ofs + 2);
                Width = BitConverter.ToUInt16(data, ofs + 4);
                Height = BitConverter.ToUInt16(data, ofs + 6);
                Compression = BitConverter.ToUInt32(data, ofs + 8);
                Unknown = BitConverter.ToUInt32(data, ofs + 12);
                Zero = BitConverter.ToUInt32(data, ofs + 16);
                Offset = BitConverter.ToUInt32(data, ofs + 20);
                return true;
            }

            internal bool ProcessBytes(ArraySegment<byte> input) {
                if ((Compression & 2) == 0) {
                    ProcessedBytes = input.Array.Skip(input.Offset).Take((int)Length).ToArray();
                    return true;
                }
                List<byte> decoded = new List<byte>();
                try {
                    var RawBytes = input.Array;
                    var Offset = input.Offset;
                    for (var y = 0; y < Height; ++y) {
                        var count = BitConverter.ToUInt16(RawBytes, Offset) - 2;
                        Offset += 2;
                        int x = 0;
                        while (count-- != 0) {
                            byte v = RawBytes[Offset];
                            ++Offset;
                            if (v != 0) {
                                ++x;
                                decoded.Add(v);
                            } else {
                                --count;
                                v = RawBytes[Offset];
                                ++Offset;
                                if (x + v > Width) {
                                    v = (byte)(Width - x);
                                }
                                x += v;
                                while (v-- != 0) {
                                    decoded.Add(0);
                                }
                            }
                        }
                    }
                    ProcessedBytes = decoded.ToArray();
                } catch (ArgumentOutOfRangeException) {
                    return false;
                }
                return true;
            }
        }

        public FileHeader Header = new FileHeader();
        public List<FrameHeader> FrameHeaders = new List<FrameHeader>();
        public PAL Palette;

        public SHP(CCFileClass ccFile = null) : base(ccFile) {
        }

        public uint FrameCount {
            get {
                return Header.FrameCount;
            }
        }

        protected override bool ReadFile(BinaryReader r) {
            var length = (int)r.BaseStream.Length;
            if (length < 8) {
                throw new InvalidDataException("File is too short to contain even a header", null);
            }

            byte[] bytes = r.ReadBytes((int)length);

            var head = new ArraySegment<byte>(bytes, 0, 8);

            if (!Header.Read(head)) {
                throw new InvalidDataException("File does not contain a valid header", null);
            }
            if(bytes.Length < (8 + (Header.FrameCount * 24))) {
                throw new InvalidDataException("File is too short to contain enough frame headers", null);
            }

            for(var i = 0; i < Header.FrameCount; ++i) {
                var seg = new ArraySegment<byte>(bytes, 8 + (i * 24), 24);
                var fh = new FrameHeader();
                if (fh.Read(seg)) {
                    FrameHeaders.Add(fh);
                } else {
                    throw new InvalidDataException(String.Format("File does not contain a valid frame header #{0}", i), null);
                }
            }

            foreach (var h in FrameHeaders) {
                if (h.Offset > length) {
                    throw new InvalidDataException(String.Format("File is too short to contain a valid frame (at {0} bytes)", h.Offset), null);
                }
                var len = (int)(length - h.Offset);
                var seg = new ArraySegment<byte>(bytes, (int)h.Offset, len);
                if (!h.ProcessBytes(seg)) {
                    throw new InvalidDataException(String.Format("File does not contain a valid frame (at {0} bytes)", h.Offset), null);
                }
            }

            return true;
        }

        public void ApplyPalette(PAL NewPalette) {
            Palette = NewPalette;
        }

        public Texture2D GetTexture(uint FrameIndex, GraphicsDevice gd) {
            if (Palette == null) {
                throw new InvalidOperationException("Cannot create texture without a palette.");
            }
            if (FrameIndex > FrameHeaders.Count) {
                throw new InvalidOperationException(String.Format("Frame {0} is not present in this file.", FrameIndex));
            }

            var frame = FrameHeaders[(int)FrameIndex];
            var fw = (int)frame.Width;
            var fh = (int)frame.Height;

            var t = new Texture2D(gd, fw, fh, false, SurfaceFormat.Color);

            int hw = fw * fh;

            if (hw != frame.ProcessedBytes.Length) {
                throw new InvalidDataException("Frame does not decompress to the right amount of bytes");
            }

            var data = new Color[hw];

            for (var i = 0; i < hw; ++i) {
                var ix = frame.ProcessedBytes[i];
                if(ix == 0) {
                    data[i] = PAL.TranslucentColor;
                } else {
                    data[i] = Palette.Colors[ix];
                }
            }

            t.SetData(data);

            return t;
        }
    }
}
