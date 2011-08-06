using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace CCClasses.Helpers {
    internal enum PixelPlacementStatus {
        S_OK = 1,
        E_BOUNDS = 2,
        E_ZINDEX = 3
    };

    public class ZBufferedTexture {
        readonly Color DummyColor = new Color(0, 0, 0, 0);

        public int Width, Height;

        protected Texture2D _Texture;

        protected Color[] Pixels;
        protected int[] ZIndices;

        public ZBufferedTexture(int W, int H) {
            Width = W;
            Height = H;

            Pixels = new Color[Width * Height];
            ZIndices = new int[Width * Height];
        }

        ~ZBufferedTexture() {
            if (_Texture != null) {
                _Texture.Dispose();
                _Texture = null;
            }
        }

        internal PixelPlacementStatus PutPixel(Color clr, int X, int Y, int Z) {

            if (X < 0 || X >= Width) {
                return PixelPlacementStatus.E_BOUNDS;
            }

            if (Y < 0 || Y >= Height) {
                return PixelPlacementStatus.E_BOUNDS;
            }

            var ixPx = Y * Width + X;

            if (ZIndices[ixPx] > Z) {
                return PixelPlacementStatus.E_ZINDEX;
            }

            Compiled = false;

            Pixels[ixPx] = clr;
            ZIndices[ixPx] = Z;

            return PixelPlacementStatus.S_OK;
        }

        public void Clear() {
            for (var i = 0; i < Width * Height; ++i) {
                Pixels[i] = DummyColor;
                ZIndices[i] = 0;
            }
            Compiled = false;
        }

        private bool Compiled = false;

        public Texture2D Compile(GraphicsDevice gd) {
            if (_Texture == null) {
                _Texture = new Texture2D(gd, Width, Height, false, SurfaceFormat.Color);
            }
            if (!Compiled) {
                _Texture.SetData(Pixels);
            }

            Compiled = true;

            return _Texture;
        }

        public void ApplyTo(Texture2D tex) {
            tex.SetData(Pixels);
        }

        internal bool CopyBlockFrom(ZBufferedTexture tex, int shiftX, int shiftY, int shiftZ = 0, bool CopyTransparent = true) {
            var clipped = false;
            for (var y = 0; y < tex.Height - Math.Abs(shiftY); ++y) {
                for (var x = 0; x < tex.Width - Math.Abs(shiftX); ++x) {
                    var oldIx = y * tex.Width + x;
                    var shX = x + shiftX;
                    var shY = y + shiftY;
                    if (shX >= 0 && shX < Width && shY >= 0 && shY < Height) {
                        var newIx = shY * Width + shX;

                        var oldPx = tex.Pixels[oldIx];
                        if (oldPx != CCClasses.FileFormats.Binary.PAL.TranslucentColor || CopyTransparent) {
                            Pixels[newIx] = tex.Pixels[oldIx];
                            ZIndices[newIx] = tex.ZIndices[oldIx] + shiftZ;
                        }
                    } else {
                        clipped = true;
                    }
                }
            }

            return clipped;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="start"></param>
        /// <param name="shiftZ"></param>
        /// <param name="CopyTransparent"></param>
        /// <returns>Was the texture clipped when drawing?</returns>
        internal bool CopyTexture(ZBufferedTexture tex, CellStruct start, int shiftZ = 0, bool CopyTransparent = true) {
            var clipped = false;
            for (var y = 0; y < tex.Height; ++y) {
                for (var x = 0; x < tex.Width; ++x) {
                    var oldIx = y * tex.Width + x;
                    var shX = x + start.X;
                    var shY = y + start.Y;
                    if (shX >= 0 && shX < Width && shY >= 0 && shY < Height) {
                        var newIx = shY * Width + shX;

                        var oldPx = tex.Pixels[oldIx];
                        if (oldPx != DummyColor || CopyTransparent) {
                            var ixZ = tex.ZIndices[oldIx] + shiftZ;
                            if (ixZ >= ZIndices[newIx]) {
                                Pixels[newIx] = tex.Pixels[oldIx];
                                ZIndices[newIx] = ixZ;
                            }
                        }
                    } else {
                        clipped = true;
                    }
                }
            }

            return clipped;
        }

    }
}
