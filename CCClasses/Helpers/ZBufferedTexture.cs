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

        protected Color[] Pixels;
        protected int[] ZIndices;

        public ZBufferedTexture(GraphicsDevice gd, int W, int H) {
            Width = W;
            Height = H;
            _Texture = new Texture2D(gd, Width, Height, false, SurfaceFormat.Color);

            Pixels = new Color[Width * Height];
            ZIndices = new int[Width * Height];
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

        private bool Compiled = false;

        public Texture2D Compile() {
            if (!Compiled) {
                _Texture.SetData(Pixels);
            }

            Compiled = true;

            return _Texture;
        }
    }
}
