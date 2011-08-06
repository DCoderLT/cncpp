using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using CCClasses.FileFormats.Binary;
using System.Diagnostics;

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

        public CellStruct ToCell() {
            return new CellStruct(X >> 8, Y >> 8);
        }
    }

    public class CellClass {
        public int X, Y;
        public int IsoTileTypeIndex = 65535;
        public int IsoTileTypeSubIndex = 0;
        public int Level = 0;
        public int Slope = 0;
        public int OverlayTypeIndex = -1;
        public int SmudgeTypeIndex = -1;

        protected CellStruct _TacticalPosition;
        public bool VisibleInTactical;
        public bool PreviouslyVisibleInTactical;

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

        public CellStruct Position2DCellsTL {
            get {
                var p2 = Position2DCells;

                var w2 = FileFormats.Binary.TMP.TileWidth / 2;
                var h2 = FileFormats.Binary.TMP.TileHeight / 2;

                var x = p2.X - w2;
                var y = p2.Y - h2;

                y -= (int)(Level * h2);

                return new CellStruct(x, y);
            }
        }

        public Rectangle Bounds2D {
            get {
                var b = TileDimensions;

                var tl = Position2DCellsTL;

                b.Offset(tl.X, tl.Y);

                return b;
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

        public void Draw(Helpers.ZBufferedTexture tex) {
            var tile = IsoTile;
            var start = TacticalPosition;
            if (tile != null) {
//                if (!PreviouslyVisibleInTactical || PreviouslyClippedInTactical) {
                    //ClippedInTactical = 
                    tile.DrawSubTile(IsoTileTypeSubIndex, tex, start, Level);
  //              }
            }
            
            //if (!PreviouslyVisibleInTactical || PreviouslyClippedInTactical) {
            //    if (ClippedInTactical) {
            //        //Debug.WriteLine("Cell {0},{1} was clipped", X, Y);
            //    }
            //} else if (!PreviouslyClippedInTactical) {
            //    //Debug.WriteLine("Cell {0},{1} was preclipped", X, Y);
            //}
        }

        public TMP.TileHeader TileTMP {
            get {
                if (IsoTile != null) {
                    return IsoTile.Tile.Tiles[IsoTileTypeSubIndex];
                }
                return null;
            }
        }

        public Rectangle TileDimensions {
            get {
                if (IsoTile != null) {
                    return TileTMP.Bounds;
                }
                return new Rectangle(0, 0, 0, 0);
            }
        }

        public Helpers.WeakRef<AbstractHierarchy.ObjectClass> FirstObject = new Helpers.WeakRef<AbstractHierarchy.ObjectClass>(null);

        public void AddContent(AbstractHierarchy.ObjectClass Content) {
            if (FirstObject.Value == null) {
                FirstObject = new Helpers.WeakRef<AbstractHierarchy.ObjectClass>(Content);
            } else {
                var LastObject = FirstObject.Value; 
                while(LastObject != null && LastObject.NextObject.Value != null) {
                    LastObject = LastObject.NextObject.Value;
                }
                LastObject.NextObject = new Helpers.WeakRef<AbstractHierarchy.ObjectClass>(Content);
            }
        }

        public void RemoveContent(AbstractHierarchy.ObjectClass Content) {
            if (Content == FirstObject.Value) {
                FirstObject = new Helpers.WeakRef<AbstractHierarchy.ObjectClass>(FirstObject.Value.NextObject.Value);
            } else {
                var o = FirstObject.Value;
                AbstractHierarchy.ObjectClass prev = null;
                while (o != null) {
                    if (o == Content) {
                        prev.NextObject = new Helpers.WeakRef<AbstractHierarchy.ObjectClass>(o.NextObject.Value);
                        break;
                    }
                    prev = o;
                    o = o.NextObject.Value;
                }
            }
        }
    }
}
