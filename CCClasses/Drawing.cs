using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace CCClasses {
    public class Drawing {
        public static GraphicsDevice GD;

        public static Texture2D CreateTexture(int W, int H) {
            return new Texture2D(GD, W, H, false, SurfaceFormat.Color);
        }
    }
}
