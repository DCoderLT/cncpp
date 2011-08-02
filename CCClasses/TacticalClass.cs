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

        public Rectangle EntireMap = new Rectangle();
        public Rectangle VisibleMap = new Rectangle();
        public Rectangle ScreenArea = new Rectangle();
        public int Width;
        public int Height;

        public Rectangle ScreenBounds;

        private TacticalClass(int W, int H) {
            Width = W;
            Height = H;

            ScreenArea = new Rectangle(0, 0, W, H);

            var scale = new Point(FileFormats.Binary.TMP.TileWidth, FileFormats.Binary.TMP.TileHeight);

            ScreenBounds = new Rectangle(0, 0, W, H); //(int)Math.Ceiling((double)W / scale.X) * scale.X, (int)Math.Ceiling((double)H / scale.Y) * scale.Y);

            _Instance = this;
        }

        public static TacticalClass Create(int W, int H) {
            if (_Instance == null) {
                return new TacticalClass(W, H);
            }
            throw new InvalidOperationException("Tactical Class already initialized.");
        }

        public bool NudgeX(int amount = 60) {
            ScreenArea.X += amount;
            if (ScreenArea.Right > VisibleMap.Right) {
                ScreenArea.X = VisibleMap.Right - ScreenArea.Width;
                return false;
            }
            if (ScreenArea.Left < VisibleMap.Left) {
                ScreenArea.X = VisibleMap.Left;
                return false;
            }
            return true;
        }

        public bool NudgeY(int amount = 30) {
            ScreenArea.Y += amount;
            if (ScreenArea.Top < VisibleMap.Top) {
                ScreenArea.Y = VisibleMap.Top;
                return false;
            }
            if (ScreenArea.Bottom > VisibleMap.Bottom) {
                ScreenArea.Y = VisibleMap.Bottom - ScreenArea.Height;
                return false;
            }
            return true;
        }

        public void UpdateCellPosition(CellClass c) {
            if (c != null) {
                c.PreviouslyClippedInTactical = c.ClippedInTactical;
                c.PreviouslyVisibleInTactical = c.VisibleInTactical;
                CellPosition(c);
            }
        }

        public void CellPosition(CellClass c) {
            var p2 = c.Position2DCells;

            var b = c.TileDimensions;

            var w2 = FileFormats.Binary.TMP.TileWidth / 2;
            var h2 = FileFormats.Binary.TMP.TileHeight / 2;

            var x = p2.X - ScreenArea.Left - w2;
            var y = p2.Y - ScreenArea.Top - h2;

            y -= (int)(c.Level * h2);

            var dx = x + b.Left;
            var dy = y + b.Top;

            var mx = dx + b.Width;
            var my = dy + b.Height;

            //Debug.WriteLine("Cell {0},{1} => {2},{3},{4},{5}", c.X, c.Y, dx, dy, mx, my);

            var pointsToCheck = new Point[] {
                new Point(dx, dy),
                new Point(dx, my),
                new Point(mx, dy),
                new Point(mx, my),
            };

            var contains = pointsToCheck.Count(p => ScreenBounds.Contains(p));

            if (contains > 0) {
                c.TacticalPosition = new CellStruct(x, y);
                c.VisibleInTactical = true;
                c.ClippedInTactical = contains < 4;
                //Debug.WriteLine("Cell {0},{1} => {2},{3},{4},{5} => {6}:{7}", c.X, c.Y, dx, dy, mx, my, c.PreviouslyClippedInTactical, c.ClippedInTactical);
                return;
            }

            c.TacticalPosition = new CellStruct();
            c.VisibleInTactical = false;
            c.ClippedInTactical = true;
            return;
        }

        private MapClass Map;

        public void SetMap(MapClass _Map) {
            Map = _Map;

            Map.cellIter.Reset();

            int X1 = Int32.MaxValue, Y1 = Int32.MaxValue, X2 = Int32.MinValue, Y2 = Int32.MinValue;

            foreach(var cell in Map.cellIter.Range()) {
                var p = cell.Position2DCells;
                X1 = Math.Min(X1, p.X);
                X2 = Math.Max(X2, p.X + 60);
                Y1 = Math.Min(Y1, p.Y);
                Y2 = Math.Max(Y2, p.Y + 30);
            }

            EntireMap = new Rectangle(X1, Y1, X2 - X1, Y2 - Y1);

            Debug.Assert(EntireMap.Width / 60 == Map.MapSize.Width, "Map size misconfigured?");
            Debug.Assert(EntireMap.Height / 30 == Map.MapSize.Height, "Map size misconfigured?");

            var visibleX = Map.LocalSize.X - Map.MapSize.X;
            var visibleY = Map.LocalSize.Y - Map.MapSize.Y;

            VisibleMap = new Rectangle(EntireMap.X + visibleX * 60 - 30, EntireMap.Y + visibleY * 30 - 15, Map.LocalSize.Width * 60 + 60, Map.LocalSize.Height * 30 + 30);

            ScreenArea = new Rectangle(VisibleMap.X, VisibleMap.Y, Width, Height);
        }
    }
}
