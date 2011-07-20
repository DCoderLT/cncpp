using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace CCClasses.FileFormats.Binary {
    public class TMP : BinaryFileFormat {
        public class FileHeader {
            internal const int ByteSize = 16;

            public UInt32 XBlocks;
            public UInt32 YBlocks;
            public UInt32 BlockWidth;
            public UInt32 BlockHeight;

            public UInt32 Area {
                get {
                    return XBlocks * YBlocks;
                }
            }

            internal void ReadFile(ArraySegment<byte> data) {
                var offs = data.Offset;
                XBlocks = BitConverter.ToUInt32(data.Array, offs);
                YBlocks = BitConverter.ToUInt32(data.Array, offs + 4);
                BlockWidth = BitConverter.ToUInt32(data.Array, offs + 8);
                BlockHeight = BitConverter.ToUInt32(data.Array, offs + 12);
            }
        };

        public class TileHeader {
            internal const int ByteSize = 52;

            public Int32 X;
            public Int32 Y;
            public Int32 ExtraOffset;
            public Int32 ZOffset;
            public Int32 ExtraZOffset;
            public Int32 ExtraX;
            public Int32 ExtraY;
            public Int32 ExtraWidth;
            public Int32 ExtraHeight;
            public bool HasExtraData;
            public bool HasZData;
            public bool HasDamagedData;
            public byte Height;
            public byte TerrainType;
            public byte RampType;
            public Color RadarLeftColor;
            public Color RadarRightColor;

            public byte[] Graphics;
            public byte[] HeightData;
            public byte[] Extras;

            internal int Position;

            public int BlockWidth, BlockHeight;

            public int ExtrasArea {
                get {
                    return ExtraWidth * ExtraHeight;
                }
            }

            internal void ReadFile(ArraySegment<byte> data) {
                var offs = data.Offset;
                X = BitConverter.ToInt32(data.Array, offs);
                Y = BitConverter.ToInt32(data.Array, offs + 4);
                ExtraOffset = BitConverter.ToInt32(data.Array, offs + 8);
                ZOffset = BitConverter.ToInt32(data.Array, offs + 12);
                ExtraZOffset = BitConverter.ToInt32(data.Array, offs + 16);
                ExtraX = BitConverter.ToInt32(data.Array, offs + 20);
                ExtraY = BitConverter.ToInt32(data.Array, offs + 24);
                ExtraWidth = BitConverter.ToInt32(data.Array, offs + 28);
                ExtraHeight = BitConverter.ToInt32(data.Array, offs + 32);
                var flags = BitConverter.ToUInt32(data.Array, offs + 36);
                HasExtraData = (flags & 1) != 0;
                HasZData = (flags & 2) != 0;
                HasZData = (flags & 4) != 0;
                Height = data.Array[offs + 40];
                TerrainType = data.Array[offs + 41];
                RampType = data.Array[offs + 42];
                RadarLeftColor = new Color(data.Array[offs + 43], data.Array[offs + 44], data.Array[offs + 45]);
                RadarRightColor = new Color(data.Array[offs + 46], data.Array[offs + 47], data.Array[offs + 48]);

                var GraphicsLength = BlockHeight * BlockWidth / 2;

                Graphics = new byte[GraphicsLength];
                Buffer.BlockCopy(data.Array, offs + 52, Graphics, 0, GraphicsLength);

                //HeightData = new byte[576];
                //Buffer.BlockCopy(data.Array, offs + 52 + 576, HeightData, 0, 576);

                if (HasExtraData) {
                    var extraArea = ExtraWidth * ExtraHeight;

                    if (data.Array.Length - offs + ExtraOffset < extraArea) {
                        throw new IndexOutOfRangeException();
                    }

                    Extras = new byte[extraArea];
                    Buffer.BlockCopy(data.Array, offs + ExtraOffset, Extras, 0, extraArea);
                }
            }

            public void GetTexture(ref Color[] data, PAL Palette, Rectangle TextureBounds, int MaxHeight) {
//                var data = new Color[Width * Height];

                var H = MaxHeight - Height;
                var ixC = ((Y - TextureBounds.Y + (H * BlockHeight / 2)) * TextureBounds.Width) + (X - TextureBounds.X);
                for (var y = 0; y < BlockHeight; ++y) {
                    for (var x = 0; x < BlockWidth; ++x) {
                        var ixPix = IndexOfPixel(x, y);
                     //   Console.WriteLine("{0};{1} => {2}", x, y, ixPix);
                        if (ixPix != -1) {
                            var ix = Graphics[ixPix];
                            if (ix == 0) {
                                data[ixC + x] = PAL.TranslucentColor;
                            } else {
                                data[ixC + x] = Palette.Colors[ix];
                            }
                        }
                    }
                    ixC += (TextureBounds.Width);
                }

                if (HasExtraData) {
                    ixC = ((ExtraY - TextureBounds.Y + (H * BlockHeight / 2)) * TextureBounds.Width) + (ExtraX - TextureBounds.X);
                    var ixPix = 0;
                    for (var y = 0; y < ExtraHeight; ++y) {
                        for (var x = 0; x < ExtraWidth; ++x) {
                            var ix = Extras[ixPix];
                            if (ix != 0) {
                                data[ixC + x] = Palette.Colors[ix];
                            }
                            ++ixPix;
                        }
                        ixC += TextureBounds.Width;
                    }
                }
            }

            internal int PixelsInRow(int y) {
                if (y > (BlockHeight - 2)) {
                    return 0;
                }
                if (y > ((BlockHeight >> 1) - 1)) {
                    y = BlockHeight - 2 - y;
                }
                return 4 * (y + 1);
            }

            internal int FirstPixelInRow(int y) {
                if (y > (BlockHeight - 2)) {
                    return -1;
                }
                if (y > ((BlockHeight >> 1) - 1)) {
                    y = BlockHeight - 2 - y;
                }

                return BlockHeight - 2 * (y + 1);
            }

            internal int IndexOfPixel(int x, int y) {
                if (y > BlockHeight - 2) {
                    return -1;
                }
                var amountInPrevRows = 0;
                for (var r = 0; r < y; ++r) {
                    amountInPrevRows += PixelsInRow(r);
                }
                var firstInRow = FirstPixelInRow(y);
                if (firstInRow <= x) {
                    if (BlockWidth - firstInRow > x) {
                        return amountInPrevRows + (x - firstInRow);
                    }
                }

                return -1;
            }
        };

        public FileHeader Header = new FileHeader();
        public List<TileHeader> Tiles = new List<TileHeader>();

        public TMP(String filename = null) : base(filename) {
        }

        public override bool ReadFile(System.IO.BinaryReader r, long length) {
            if (length < FileHeader.ByteSize) {
                return false;
            }

            byte[] h = r.ReadBytes(FileHeader.ByteSize);

            var seg = new ArraySegment<byte>(h);
            Header.ReadFile(seg);

            var offset = FileHeader.ByteSize;

            if (length < FileHeader.ByteSize + Header.Area * 4) {
                return false;
            }

            offset += (int)Header.Area * 4;

            UInt32[] Positions = new UInt32[Header.Area];

            var TileSize = Header.BlockHeight * Header.BlockWidth / 2;

            for (var i = 0; i < Header.Area; ++i) {
                var pos = r.ReadInt32();
                if (pos + TileSize + TileHeader.ByteSize > length) {
                    return false;
                }
                if (pos == 0) {
                    continue;
                }

                var T = new TileHeader();
                T.BlockWidth = (int) Header.BlockWidth;
                T.BlockHeight = (int) Header.BlockHeight;
                T.Position = pos;
                Tiles.Add(T);
            }

            byte[] contents = r.ReadBytes((int)(length - offset));

            foreach(var T in Tiles) {
                seg = new ArraySegment<byte>(contents, T.Position - offset, 0);
                T.ReadFile(seg);
            }

            return true;
        }

        internal int MaxHeight {
            get {
                return Tiles.Max(T => T.Height);
            }
        }

        private Rectangle GetBounds() {
            var x = Int32.MaxValue;
            var y = Int32.MaxValue;
            var w = Int32.MinValue;
            var h = Int32.MinValue;

            var bigY = Int32.MinValue;
            long bigYval = 0;

            foreach (var T in Tiles) {
                var H = MaxHeight - T.Height;
                var HeightComponent = (int)(H * Header.BlockHeight / 2);
                var x1 = T.X;
                var x2 = x1 + T.BlockWidth;
                var y1 = T.Y + HeightComponent;
                var y2 = y1 + T.BlockHeight;

                if (T.HasExtraData) {
                    var yE1 = T.ExtraY + HeightComponent;
                    var yE2 = T.ExtraHeight + yE1;
                    if (yE1 < y) {
                        y = yE1;
                    }
                    if (yE2 > h) {
                        h = yE2;
                    }
                }


                if (x1 < x) {
                    x = x1;
                }
                if (x2 > w) {
                    w = x2;
                }

                if (y1 < y) {
                    y = y1;
                }
                if (y2 > h) {
                    h = y2;
                }

                if (bigY < T.Y) {
                    bigY = T.Y;
                    bigYval = T.Y + Header.BlockWidth + HeightComponent;
                    if (T.HasExtraData) {
                        bigYval -= T.ExtraY;
                    }
                }
            }

            w -= x;
            h -= y;

            if (h < bigYval) {
                h = (int)bigYval;
            }

            return new Rectangle(x, y, w, h);
        }

        public Texture2D GetTexture(GraphicsDevice gd, PAL Palette) {
            var bounds = GetBounds();

            for (var i = 0; i < Header.BlockHeight; ++i) {
                Console.WriteLine("Line {0} should contain {1} ({2}) pixels", i, Tiles[0].PixelsInRow(i), Tiles[0].FirstPixelInRow(i));
            }

            var t = new Texture2D(gd, bounds.Width, bounds.Height, false, SurfaceFormat.Color);

            var data = new Color[bounds.Width * bounds.Height];

            for (var i = 0; i < data.Length; ++i) {
                data[i] = PAL.TranslucentColor;
            }

            foreach (var T in Tiles) {
                T.GetTexture(ref data, Palette, bounds, MaxHeight);
            }

            t.SetData(data);

            return t;
        }

    }
}
