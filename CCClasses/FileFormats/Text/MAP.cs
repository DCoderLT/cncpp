using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CCClasses.Libraries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CCClasses.FileFormats.Text {
    public class MAP : INI {
        public class TilePacked {
            public const int ByteSize = 11;
            public Int16 X;
            public Int16 Y;
            public UInt32 TileTypeIndex;
            public byte TileSubtypeIndex;
            public byte Level;
            public byte Unknown;

            public void ReadFile(ArraySegment<byte> data) {
                var offs = data.Offset;
                X = BitConverter.ToInt16(data.Array, offs);
                Y = BitConverter.ToInt16(data.Array, offs + 2);
                TileTypeIndex = BitConverter.ToUInt32(data.Array, offs + 4);
                TileSubtypeIndex = data.Array[offs + 8];
                Level = data.Array[offs + 9];
                Unknown = data.Array[offs + 10];
            }
        };

        public MAP(CCFileClass ccFile = null) : base(ccFile) {
        }

        private byte[] IsoMapPack;

        public Rectangle PreviewSize;
        public Color[] Preview;
        private byte[] PreviewPack;


        public List<TilePacked> Tiles = new List<TilePacked>();

        public List<int> Overlays = new List<int>();

        protected override bool ReadFile(StreamReader r) {
            if (!base.ReadFile(r)) {
                ParseError("Failed to read map INI.");
                return false;
            }
            if (!SectionExists("IsoMapPack5")) {
                return false;
            }

            IsoMapPack = UnpackSectionLZO("IsoMapPack5");

            var TileCount = IsoMapPack.Length / TilePacked.ByteSize;

            Tiles.Clear();

            for (var i = 0; i < TileCount; ++i) {
                var seg = new ArraySegment<byte>(IsoMapPack, i * TilePacked.ByteSize, TilePacked.ByteSize);

                var T = new TilePacked();
                T.ReadFile(seg);
                Tiles.Add(T);
            }

            if (SectionExists("Preview") && SectionExists("PreviewPack")) {
                int[] PSize;
                if (Get4Integers("Preview", "Size", out PSize, new int[4])) {
                    PreviewSize = new Rectangle(PSize[0], PSize[1], PSize[2], PSize[3]);
                    if (PreviewSize.Width > 0 && PreviewSize.Height > 0) {
                        Preview = new Color[PreviewSize.Width * PreviewSize.Height];
                        PreviewPack = UnpackSectionLZO("PreviewPack");

                        var pixelCount = PreviewPack.Length / 3;

                        for (var y = 0; y < PreviewSize.Height; ++y) {
                            for (var x = 0; x < PreviewSize.Width; ++x) {
                                var ixPix = y * PreviewSize.Width + x;
                                if (ixPix < pixelCount) {
                                    var R = PreviewPack[ixPix * 3];
                                    var G = PreviewPack[ixPix * 3 + 1];
                                    var B = PreviewPack[ixPix * 3 + 2];
                                    Preview[ixPix] = new Color(R, G, B);
                                } else {
                                    Preview[ixPix] = Color.Black;
                                }
                            }
                        }
                    }
                }
            }

            if (SectionExists("OverlayPack")) {
                Overlays.Clear();
                var unpackedOverlays = UnpackSectionLCW("OverlayPack");
                foreach (var ixOverlay in unpackedOverlays) {
                    Overlays.Add(ixOverlay);
                }
            }

            return true;
        }

        private byte[] UnpackSectionLZO(String Section) {
            var Compressed = ReadSection(Section);

            var Un64 = Convert.FromBase64String(Compressed);

            return LZO.Slurp(Un64);
        }

        private byte[] UnpackSectionLCW(String Section) {
            var Compressed = ReadSection(Section);

            var Un64 = Convert.FromBase64String(Compressed);

            return LCW.Slurp(Un64);
        }

        public Microsoft.Xna.Framework.Graphics.Texture2D GetPreviewTexture(Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice) {
            var MapPreview = new Texture2D(GraphicsDevice, PreviewSize.Width, PreviewSize.Height, false, SurfaceFormat.Color);

            MapPreview.SetData(Preview);

            return MapPreview;
        }
    }
}
