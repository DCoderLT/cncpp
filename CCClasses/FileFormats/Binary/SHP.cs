using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace CCClasses.FileFormats.Binary {
    public class SHP : BinaryFileFormat {
        public class FileHeader {
            public int zero;
            public int Width;
            public int Height;
            public int FrameCount;

            public bool Read(ArraySegment<byte> input) {
                var data = input.Array;
                var ofs = input.Offset;
                zero = BitConverter.ToInt16(data, ofs + 0);
                Width = BitConverter.ToInt16(data, ofs + 2);
                Height = BitConverter.ToInt16(data, ofs + 4);
                FrameCount = BitConverter.ToInt16(data, ofs + 6);
                return true;
            }
        }

        public class FrameHeader {
            public int X, Y, Width, Height, Compression, Unknown, Zero, Offset;
            public byte[] ProcessedBytes;

            public int Length {
                get {
                    return Width * Height;
                }
            }

            public Rectangle Bounds {
                get {
                    return new Rectangle(X, Y, Width, Height);
                }
            }

            public bool Read(ArraySegment<byte> input) {
                var data = input.Array;
                var ofs = input.Offset;
                X = BitConverter.ToUInt16(data, ofs + 0);
                Y = BitConverter.ToUInt16(data, ofs + 2);
                Width = BitConverter.ToUInt16(data, ofs + 4);
                Height = BitConverter.ToUInt16(data, ofs + 6);
                Compression = BitConverter.ToInt32(data, ofs + 8);
                Unknown = BitConverter.ToInt32(data, ofs + 12);
                Zero = BitConverter.ToInt32(data, ofs + 16);
                Offset = BitConverter.ToInt32(data, ofs + 20);
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
                } catch (ArgumentOutOfRangeException Ex) {
                    Debug.WriteLine("Caught an Out of Range exception: {0}...", Ex.Message);
                    return false;
                }
                return true;
            }

            public void DrawIntoTexture(Helpers.ZBufferedTexture Texture, CellStruct StartXY, PAL tmpPalette, int zIndex = 0) {
                var fw = (int)Width;
                var fh = (int)Height;

                if (fw * fh != ProcessedBytes.Length) {
                    throw new InvalidDataException("Frame does not decompress to the right amount of bytes");
                }

                for (var y = 0; y < fh; ++y) {
                    for (var x = 0; x < fw; ++x) {
                        var ixPix = y * fw + x;
                        var ixClr = ProcessedBytes[ixPix];
                        var clr = PAL.TranslucentColor;
                        if (ixClr != 0) {
                            clr = tmpPalette.Colors[ixClr];
                        }
                        Texture.PutPixel(clr, StartXY.X + x, StartXY.Y + y, zIndex);
                    }
                }
            }
        }

        public FileHeader Header = new FileHeader();
        public List<FrameHeader> FrameHeaders = new List<FrameHeader>();
        public PAL Palette;

        protected static Dictionary<String, SHP> LoadedFiles = new Dictionary<string, SHP>();

        public static SHP LoadFile(String filename) {
            if (LoadedFiles.ContainsKey(filename)) {
                return LoadedFiles[filename];
            }
            var shp = FileSystem.LoadFile(filename);
            if (shp != null) {
                LoadedFiles[filename] = new SHP(shp);
                return LoadedFiles[filename];
            }
            return null;
        }

        public SHP(CCFileClass ccFile = null)
            : base(ccFile) {
        }

        public int FrameCount {
            get {
                return Header.FrameCount;
            }
        }

        public Rectangle Bounds {
            get {
                return new Rectangle(0, 0, Header.Width, Header.Height);
            }
        }

        protected Helpers.ZBufferedTexture tex;

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
            if (bytes.Length < (8 + (Header.FrameCount * 24))) {
                throw new InvalidDataException("File is too short to contain enough frame headers", null);
            }

            for (var i = 0; i < Header.FrameCount; ++i) {
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

        public void GetTexture(uint FrameIndex, ref Helpers.ZBufferedTexture resultTexture) {
            if (Palette == null) {
                throw new InvalidOperationException("Cannot create texture without a palette.");
            }
            if (FrameIndex > FrameHeaders.Count) {
                throw new InvalidOperationException(String.Format("Frame {0} is not present in this file.", FrameIndex));
            }

            var frame = FrameHeaders[(int)FrameIndex];
            var fw = (int)frame.Width;
            var fh = (int)frame.Height;

            int hw = fw * fh;

            if (hw != frame.ProcessedBytes.Length) {
                throw new InvalidDataException("Frame does not decompress to the right amount of bytes");
            }

            if (resultTexture == null) {
                resultTexture = new Helpers.ZBufferedTexture((int)frame.X + fw, (int)frame.Y + fh);
            } else {
                resultTexture.Clear();
            }

            for (var y = 0; y < fh; ++y) {
                for (var x = 0; x < fw; ++x) {
                    var ix = frame.ProcessedBytes[y * fw + x];
                    Color c;
                    if (ix == 0) {
                        c = PAL.TranslucentColor;
                    } else {
                        c = Palette.Colors[ix];
                    }
                    resultTexture.PutPixel(c, (int)frame.X + x, (int)frame.Y + y, 0);
                }
            }
        }

        public void DrawIntoTexture(Helpers.ZBufferedTexture Texture, CellStruct CenterPoint, uint FrameIndex, PAL tmpPalette, int zIndex = 0) {
            if (FrameIndex > FrameHeaders.Count) {
                throw new InvalidOperationException(String.Format("Frame {0} is not present in this file.", FrameIndex));
            }

            var frame = FrameHeaders[(int)FrameIndex];
            var fw = (int)frame.Width;
            var fh = (int)frame.Height;

            var startX = (int)(CenterPoint.X - fw / 2);
            var startY = (int)(CenterPoint.Y - fh / 2);

            frame.DrawIntoTexture(Texture, new CellStruct(startX, startY), tmpPalette, zIndex);

        }

        public void DrawIntoTextureTL(Helpers.ZBufferedTexture Texture, CellStruct topLeft, uint FrameIndex, PAL tmpPalette, int zIndex = 0) {
            if (FrameIndex > FrameHeaders.Count) {
                throw new InvalidOperationException(String.Format("Frame {0} is not present in this file.", FrameIndex));
            }

            var frame = FrameHeaders[(int)FrameIndex];

            frame.DrawIntoTexture(Texture, topLeft, tmpPalette, zIndex);
        }


        public void DrawIntoTextureBL(Helpers.ZBufferedTexture Texture, CellStruct BottomLeft, uint FrameIndex, PAL tmpPalette, int zIndex = 0) {
            if (FrameIndex > FrameHeaders.Count) {
                throw new InvalidOperationException(String.Format("Frame {0} is not present in this file.", FrameIndex));
            }

            var frame = FrameHeaders[(int)FrameIndex];

            var fh = (int)frame.Height;

            var startX = (int)(BottomLeft.X);
            var startY = (int)(BottomLeft.Y - fh);

            frame.DrawIntoTexture(Texture, new CellStruct(startX, startY), tmpPalette, zIndex);
        }

    }
}
