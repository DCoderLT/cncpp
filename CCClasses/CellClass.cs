using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using CCClasses.FileFormats.Binary;
using System.Diagnostics;
using CCClasses.AbstractHierarchy;

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

        public override string ToString() {
            return String.Format("({0}; {1})", X, Y);
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

        public override string ToString() {
            return String.Format("({0}; {1}; {2})", X, Y, Z);
        }
    }

    public class CellClass {
        public int X, Y;
        public int IsoTileTypeIndex = 65535;
        public int IsoTileTypeSubIndex = 0;
        public int Level = 0;
        public int Slope = 0;
        public int OverlayTypeIndex = -1;
        public int OverlayState = 0;
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
                var xyz = TacticalClass.From3DCellsTo3DLeptons(new CellStruct(X, Y));
                xyz.Z += FloorHeight;
                return xyz;
            }
        }

        public CellStruct Position2DLeptons {
            get {
                return TacticalClass.From3DCellsTo2DLeptons(new CellStruct(X, Y));
            }
        }

        public CellStruct Position2DCells {
            get {
                return TacticalClass.From3DCellsTo2DCells(new CellStruct(X, Y));
            }
        }

        public CellStruct Position2DCellsTL {
            get {
                var p2 = TacticalClass.Position2DCellsTL(new CellStruct(X, Y));

                var h2 = FileFormats.Binary.TMP.TileHeight / 2;

                p2.Y -= (int)(Level * h2);

                return p2;
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
                    tile.DrawSubTile(IsoTileTypeSubIndex, tex, start, Level, OverlayTypeIndex != -1);
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

        internal CellStruct OverlayPosition {
            get {
                var xy = new CellStruct(-30, -15);
                if (OverlayTypeIndex != -1) {
                    xy += OverlayTypeClass.PositionAdjustment(OverlayTypeIndex);
                }
                return xy;
            }
        }

        internal void DrawOverlays(Helpers.ZBufferedTexture T) {
            if (OverlayTypeIndex != -1) {
                var OTypes = CCFactory<OverlayTypeClass, OverlayClass>.Get().FactoryItems;
                var OT = OTypes[OverlayTypeIndex];
                var pos = TacticalPosition + new CellStruct(30, 15) + OverlayPosition;
                pos.Y -= (2);// + OverlayPosition.Y);
                var tImage = OT.SHPImage.Value;
                if (OT.Tiberium) {
                    var ix = OverlayClass.OverlayToTiberium(OverlayTypeIndex);
                    if (ix != -1) {
                        var t = TiberiumClass.All[ix];
                        OverlayTypeClass tOverlay;
                        if (Slope != 0) {
                            tOverlay = OTypes[t.NumImages + t.NumExtraImages / 4 * (Slope - 1) + t.Overlay.ArrayIndex];
                        } else {
                            tOverlay = OTypes[t.Overlay.ArrayIndex + X * Y % t.NumImages];
                        }
//                        tImage.DrawIntoTexture(T, pos, (uint)OverlayState, MapTheater.TemperatePAL);
                    }
                } else if (OT.Wall) {
  //                  tImage.DrawIntoTexture(T, pos, (uint)OverlayState, MapTheater.unitPAL);
                }
            }
        }
    }
}
