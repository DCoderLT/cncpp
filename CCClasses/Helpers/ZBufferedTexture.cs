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
        public int Width, Height;

        protected Texture2D _Texture;

        protected Color[,] Pixels;
        protected int[,] ZIndices;

        public ZBufferedTexture(GraphicsDevice gd, int W, int H) {
            Width = W;
            Height = H;
            _Texture = new Texture2D(gd, Width, Height, false, SurfaceFormat.Color);

            Pixels = new Color[Width, Height];
            ZIndices = new int[Width, Height];
        }

        internal PixelPlacementStatus PutPixel(Color clr, int X, int Y, int Z) {

            if (X < 0 || X >= Width) {
                return PixelPlacementStatus.E_BOUNDS;
            }

            if (Y < 0 || Y >= Height) {
                return PixelPlacementStatus.E_BOUNDS;
            }

            if (ZIndices[X, Y] > Z) {
                return PixelPlacementStatus.E_ZINDEX;
            }

            Compiled = false;

            Pixels[X, Y] = clr;
            ZIndices[X, Y] = Z;

            return PixelPlacementStatus.S_OK;
        }

        private bool Compiled = false;

        public Texture2D Compile() {
            if (!Compiled) {
                var colors = new Color[Height * Width];
                var ix = 0;

                for (var y = 0; y < Height; ++y) {
                    for (var x = 0; x < Width; ++x) {
                        colors[ix] = Pixels[x, y];
                        ++ix;
                    }
                }

                _Texture.SetData(colors);
            }

            Compiled = true;

            return _Texture;
        }
    }
}
