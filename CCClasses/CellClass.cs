using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using CCClasses.FileFormats.Binary;

namespace CCClasses {
    public class CellStruct {
        public int X, Y;

        public CellStruct(int x = 0, int y = 0) {
            X = x;
            Y = y;
        }

        public static CellStruct operator +(CellStruct lhs, CellStruct rhs) {
            return new CellStruct(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static CellStruct operator -(CellStruct lhs, CellStruct rhs) {
            return new CellStruct(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

        public static CellStruct operator *(CellStruct lhs, CellStruct rhs) {
            return new CellStruct(lhs.X * rhs.X, lhs.Y * rhs.Y);
        }

        public static CellStruct operator /(CellStruct lhs, CellStruct rhs) {
            return new CellStruct(lhs.X / rhs.X, lhs.Y / rhs.Y);
        }
    };

    public class CoordStruct {
        public int X, Y, Z;

        public CoordStruct(int x = 0, int y = 0, int z = 0) {
            X = x;
            Y = y;
            Z = z;
        }
}

    public class CellClass {
        public int X, Y;
        public int IsoTileTypeIndex = -1;
        public int IsoTileTypeSubIndex = -1;
        public int Level = 0;
        public int Slope = 0;
        public int OverlayTypeIndex = -1;
        public int SmudgeTypeIndex = -1;

        protected CellStruct _TacticalPosition;
        public bool VisibleInTactical;

        public CellStruct TacticalPosition {
            get {
                return _TacticalPosition;
            }
            set {
                _TacticalPosition = value;
            }
        }

        public int FloorHeight {
            get {
                return (int)(TacticalClass.CellLevelHeight * Level + 0.5);
            }
        }

        public CoordStruct Position3DLeptons {
            get {
                return new CoordStruct((X << 8) + 128, (Y << 8) + 128, FloorHeight);
            }
        }

        public CellStruct Position2DLeptons {
            get {
                var p3 = Position3DLeptons;

                var dx = -60 * p3.Y / 2 + 60 * p3.X / 2;
                var dy = 30 * p3.Y / 2 + 30 * p3.X / 2;

                return new CellStruct(dx, dy);
            }
        }

        public CellStruct Position2DCells {
            get {
                var pl = Position2DLeptons;
                return new CellStruct(pl.X / 256, pl.Y / 256);
            }
        }

        public IsoTileTypeClass IsoTile {
            get {
                if (IsoTileTypeIndex == -1) {
                    return null;
                }

                if (IsoTileTypeIndex == 65535) {
                    return IsoTileTypeClass.All[IsoTileTypeClass.TilesetIndices["ClearTile"].TileIndex];
                }

                return IsoTileTypeClass.All[IsoTileTypeIndex];
            }
        }

        public void DrawBase(Helpers.ZBufferedTexture tex) {
            var start = TacticalPosition - new CellStruct(30, 15);
            var tile = IsoTile;
            if (tile != null) {
                tile.DrawSubTileBase(IsoTileTypeSubIndex, tex, start, Level);
            }
        }

        public void DrawExtra(Helpers.ZBufferedTexture tex) {
            var start = TacticalPosition - new CellStruct(30, 15);
            var tile = IsoTile;
            if (tile != null) {
                tile.DrawSubTileExtra(IsoTileTypeSubIndex, tex, start, Level);
            }
        }
    }
}
