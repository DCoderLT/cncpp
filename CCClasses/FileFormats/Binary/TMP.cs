using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace CCClasses.FileFormats.Binary {
    public class TMP : BinaryFileFormat {
        public static int TileWidth = 60;
        public static int TileHeight = 30;

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

            internal bool ReadFile(ArraySegment<byte> data) {
                var offs = data.Offset;
                XBlocks = BitConverter.ToUInt32(data.Array, offs);
                YBlocks = BitConverter.ToUInt32(data.Array, offs + 4);
                BlockWidth = BitConverter.ToUInt32(data.Array, offs + 8);
                BlockHeight = BitConverter.ToUInt32(data.Array, offs + 12);

                return BlockHeight == TileHeight && BlockWidth == TileWidth;
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
            public byte[] DamagedGraphics;
            public byte[] ZData;
            public byte[] Extras;
            public byte[] ExtraZData;

            internal int Position;

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
                HasDamagedData = (flags & 4) != 0;
                Height = data.Array[offs + 40];
                TerrainType = data.Array[offs + 41];
                RampType = data.Array[offs + 42];
                RadarLeftColor = new Color(data.Array[offs + 43], data.Array[offs + 44], data.Array[offs + 45]);
                RadarRightColor = new Color(data.Array[offs + 46], data.Array[offs + 47], data.Array[offs + 48]);

                var GraphicsLength = TileHeight * TileWidth / 2;

                Graphics = new byte[GraphicsLength];
                Buffer.BlockCopy(data.Array, offs + 52, Graphics, 0, GraphicsLength);

                if (HasDamagedData) {
                    if (data.Array.Length - offs + 52 + 2 * GraphicsLength < GraphicsLength) {
                        throw new IndexOutOfRangeException();
                    }

                    DamagedGraphics = new byte[GraphicsLength];
                    Buffer.BlockCopy(data.Array, offs + 52 + GraphicsLength, DamagedGraphics, 0, GraphicsLength);
                }

                if (HasZData) {
                    if (data.Array.Length - offs + ZOffset < GraphicsLength) {
                        throw new IndexOutOfRangeException();
                    }

                    ZData = new byte[GraphicsLength];
                    Buffer.BlockCopy(data.Array, offs + ZOffset, ZData, 0, GraphicsLength);
                }

                if (HasExtraData) {
                    var extraArea = ExtraWidth * ExtraHeight;

                    if (data.Array.Length - offs + ExtraOffset < extraArea) {
                        throw new IndexOutOfRangeException();
                    }

                    Extras = new byte[extraArea];
                    Buffer.BlockCopy(data.Array, offs + ExtraOffset, Extras, 0, extraArea);

                    if (HasZData) {
                        ExtraZData = new byte[extraArea];
                        Buffer.BlockCopy(data.Array, offs + ExtraZOffset, ExtraZData, 0, extraArea);
                    }
                }
            }

            /// <summary>
            /// Draws this subtile at a given position in the texture - no extra data
            /// </summary>
            /// <param name="data"></param>
            /// <param name="Palette"></param>
            /// <param name="TextureBounds"></param>
            /// <param name="TopLeft"></param>
            public void GetBaseTextureStandalone(Helpers.ZBufferedTexture tex, PAL Palette, CellStruct TopLeft, int CellLevel) {
                var beginX = TopLeft.X;
                var beginY = TopLeft.Y;

                for (var y = 0; y < TileHeight; ++y) {
                    for (var x = 0; x < TileWidth; ++x) {
                        var ixPix = IndexOfPixel(x, y);
                        if (ixPix != -1) {
                            var ixClr = Graphics[ixPix];
                            if (ixClr != 0) {
                                var clr = Palette.Colors[ixClr];

                                var z = CellLevel * 30;
                                if (HasZData) {
                                    z += ZData[ixPix];
                                }

                                tex.PutPixel(clr, beginX + x, beginY + y, z);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Draws the extra data of this subtile at a given position in the texture
            /// </summary>
            /// <param name="data"></param>
            /// <param name="Palette"></param>
            /// <param name="TextureBounds"></param>
            /// <param name="TopLeft"></param>
            public void GetExtrasTextureStandalone(Helpers.ZBufferedTexture tex, PAL Palette, CellStruct TopLeft, int CellLevel) {
                var beginX = TopLeft.X + (ExtraX - X);
                var beginY = TopLeft.Y + (ExtraY - Y);

                for (var y = 0; y < ExtraHeight; ++y) {
                    for (var x = 0; x < ExtraWidth; ++x) {
                            var ixPix = y * ExtraWidth + x;
                            var ixClr = Extras[ixPix];
                            if (ixClr != 0) {
                                var clr = Palette.Colors[ixClr];

                                var z = CellLevel * 30;
                                if (HasZData) {
                                    z += ExtraZData[ixPix];
                                }

                                tex.PutPixel(clr, beginX + x, beginY + y, z);
                            }
                    }
                }
            }


            /// <summary>
            /// Draws this subtile into a texture that was specifically allocated for this tile only - not very useful overall, just for testing single combined tile drawing
            /// </summary>
            /// <param name="data"></param>
            /// <param name="Palette"></param>
            /// <param name="TextureBounds"></param>
            /// <param name="MaxHeight"></param>
            public void GetCombinedTextureInTile(ref Color[] data, PAL Palette, Rectangle TextureBounds, int MaxHeight) {
                //                var data = new Color[Width * Height];

                var H = MaxHeight - Height;
                var ixC = ((Y - TextureBounds.Y + (H * TileHeight / 2)) * TextureBounds.Width) + (X - TextureBounds.X);
                for (var y = 0; y < TileHeight; ++y) {
                    for (var x = 0; x < TileWidth; ++x) {
                        var ixPix = IndexOfPixel(x, y);
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
                    ixC = ((ExtraY - TextureBounds.Y + (H * TileHeight / 2)) * TextureBounds.Width) + (ExtraX - TextureBounds.X);
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

            private int PixelsInRow(int y) {
                if (y > (TileHeight - 2)) {
                    return 0;
                }
                if (y > ((TileHeight >> 1) - 1)) {
                    y = TileHeight - 2 - y;
                }
                return 4 * (y + 1);
            }

            private int FirstPixelInRow(int y) {
                if (y > (TileHeight - 2)) {
                    return -1;
                }
                if (y > ((TileHeight >> 1) - 1)) {
                    y = TileHeight - 2 - y;
                }

                return TileHeight - 2 * (y + 1);
            }

            private int IndexOfPixel(int x, int y) {
                if (y > TileHeight - 2) {
                    return -1;
                }
                var amountInPrevRows = 0;
                for (var r = 0; r < y; ++r) {
                    amountInPrevRows += PixelsInRow(r);
                }
                var firstInRow = FirstPixelInRow(y);
                if (firstInRow <= x) {
                    if (TileWidth - firstInRow > x) {
                        return amountInPrevRows + (x - firstInRow);
                    }
                }

                return -1;
            }
        };

        public FileHeader Header = new FileHeader();
        public List<TileHeader> Tiles = new List<TileHeader>();

        public TMP(CCFileClass ccFile = null)
            : base(ccFile) {
        }

        protected override bool ReadFile(BinaryReader r) {
            var length = (int)r.BaseStream.Length;
            if (length < FileHeader.ByteSize) {
                return false;
            }

            byte[] h = r.ReadBytes(FileHeader.ByteSize);

            var seg = new ArraySegment<byte>(h);
            if (!Header.ReadFile(seg)) {
                return false;
            }

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
                    Tiles.Add(null);
                    continue;
                }

                var T = new TileHeader();
                T.Position = pos;
                Tiles.Add(T);
            }

            byte[] contents = r.ReadBytes((int)(length - offset));

            foreach (var T in TilesReal) {
                seg = new ArraySegment<byte>(contents, T.Position - offset, 0);
                T.ReadFile(seg);
            }

            return true;
        }

        internal IEnumerable<TileHeader> TilesReal {
            get {
                int idx = -1;
                while (idx < Tiles.Count - 1) {
                    ++idx;
                    if (Tiles[idx] != null) {
                        yield return Tiles[idx];
                    }
                }
            }
        }

        internal int MaxHeight {
            get {
                return TilesReal.Max(T => T.Height);
            }
        }

        private Rectangle GetBounds() {
            var x = Int32.MaxValue;
            var y = Int32.MaxValue;
            var w = Int32.MinValue;
            var h = Int32.MinValue;

            var bigY = Int32.MinValue;
            long bigYval = 0;

            foreach (var T in TilesReal) {
                var H = MaxHeight - T.Height;
                var HeightComponent = (int)(H * Header.BlockHeight / 2);
                var x1 = T.X;
                var x2 = x1 + TMP.TileWidth;
                var y1 = T.Y + HeightComponent;
                var y2 = y1 + TMP.TileHeight;

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

            var t = new Texture2D(gd, bounds.Width, bounds.Height, false, SurfaceFormat.Color);

            var data = new Color[bounds.Width * bounds.Height];

            for (var i = 0; i < data.Length; ++i) {
                data[i] = PAL.TranslucentColor;
            }

            foreach (var T in TilesReal) {
                T.GetCombinedTextureInTile(ref data, Palette, bounds, MaxHeight);
            }

            t.SetData(data);

            return t;
        }

    }
}
