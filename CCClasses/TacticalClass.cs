using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace CCClasses {
    public class TacticalClass {
        private static TacticalClass _Instance;

        public static TacticalClass Instance {
            get {
                return _Instance;
            }
        }

        public static readonly double MQ01, MQ02, MQ03, MQ04, MQ05, MQ06, MQ07, MQ08, MQ09, MQ10, MQ11, MQ12;

        public static double CellLevelHeight {
            get {
                return MQ07;
            }
        }

        static TacticalClass() {
            var sq3 = 0.0174532925199433;
            MQ01 = Math.Sqrt(2) * 256;
            MQ02 = MQ01 / 60;
            MQ03 = 1 / MQ02;
            MQ04 = sq3 * 45;
            MQ05 = sq3 * 60;
            MQ06 = sq3 * 90;
            MQ07 = (int)(Math.Tan(sq3 * 30) * MQ01 * 0.5);
            MQ08 = Math.Atan(MQ07 * 3.90625e-3);
            MQ09 = Math.Atan(MQ07 * 2 / MQ01);
            MQ10 = (int)(MQ07 + 0.5);

            MQ11 = 60.0 / MQ01;
            MQ12 = Math.Sin(MQ05) * MQ11;
        }

        public Rectangle VisibleMap = new Rectangle();
        public Rectangle ScreenArea = new Rectangle();
        public int Width;
        public int Height;

        private TacticalClass(int W, int H) {
            Width = W;
            Height = H;

            ScreenArea = new Rectangle(0, 0, W, H);

            _Instance = this;
        }

        public static TacticalClass Create(int W, int H) {
            if (_Instance == null) {
                return new TacticalClass(W, H);
            }
            throw new InvalidOperationException("Tactical Class already initialized.");
        }

        public bool UpdateCellPosition(CellClass c) {
            if (c == null) {
                return false;
            }
            CellPosition(c);
            return c.VisibleInTactical;
        }

        public CellStruct CellPosition(CellClass c) {
            var p2 = c.Position2DCells;

            var dx = p2.X - ScreenArea.Left;
            var dy = p2.Y - ScreenArea.Top;

            dy -= (int)(c.Level * 15);

//            Debug.WriteLine("Cell at {0}x{1} got {2}x{3} as its tactical", c.X, c.Y, dx, dy);

            if (dx >= -30 && dx <= Width + 30) {
                if (dy >= -30 && dy <= Height + 15) {
                    c.TacticalPosition = new CellStruct(dx, dy);
                    c.VisibleInTactical = true;
                    return c.TacticalPosition;
                }
            }

            c.TacticalPosition = new CellStruct();
            c.VisibleInTactical = false;
            return c.TacticalPosition;
        }

        private MapClass Map;

        public void SetMap(MapClass _Map) {
            Map = _Map;

            Map.cellIter.Reset();
            var start = Map.cellIter.NextCell();
            CellClass t = Map.cellIter.NextCell(), end = t;
            while((t = Map.cellIter.NextCell()) != null) {
                end = t;
            }

            VisibleMap = new Rectangle(start.Position2DCells.X, start.Position2DCells.Y, end.X - start.X, end.Y - start.Y);
            ScreenArea = new Rectangle(VisibleMap.X, VisibleMap.Y, Width, Height);
        }
    }
}
