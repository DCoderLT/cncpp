using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace CCClasses.AbstractHierarchy {
    public class OverlayClass : ObjectClass {
        public override AbstractID WhatAmI() {
            return AbstractID.Overlay;
        }

        protected Helpers.WeakRef<OverlayTypeClass> MyType;

        public override ObjectTypeClass GetObjectType() {
            return MyType.Value;
        }

        public override void SetObjectType(ObjectTypeClass T) {
            MyType = new Helpers.WeakRef<OverlayTypeClass>(T as OverlayTypeClass);
        }

        public override void Draw(Helpers.ZBufferedTexture Texture) {
            var Type = GetObjectType() as OverlayTypeClass;
            if (Type.Voxel) {
            } else {
                var image = Type.SHPImage.Value;
                if (image != null) {
                    var adjust = OverlayTypeClass.PositionAdjustment(Type.ArrayIndex);

                    var cell = GetCell();

                    var h = 2 + TacticalClass.Adjust2DYTo3DZ(Location.Z);

                    var TL = TacticalClass.Instance.Position2DOnScreen(Location);// +new CellStruct(0, 30 - TacticalClass.From3DZTo2DY(Location.Z));// +adjust;

//                    Debug.WriteLine("Drawing overlay @ {0}, {1}", TL.X, TL.Y);
                    if (Type.Tiberium) {
                        var ix = OverlayToTiberium(Type.ArrayIndex);
                        if (ix != -1) {
                            var OTypes = CCFactory<OverlayTypeClass, OverlayClass>.Get().FactoryItems;
                            var tib = TiberiumClass.All[ix];
                            OverlayTypeClass tOverlay;
                            if (cell.Slope != 0) {
                                tOverlay = OTypes[tib.NumImages + tib.NumExtraImages / 4 * (cell.Slope - 1) + tib.Overlay.ArrayIndex];
                            } else {
                                tOverlay = OTypes[tib.Overlay.ArrayIndex + cell.X * cell.Y % tib.NumImages];
                            }
                            tOverlay.SHPImage.Value.DrawIntoTextureBL(Texture, TL, (uint)cell.OverlayState, MapTheater.TemperatePAL, h);
                        }
                    } else if(Type.Wall) {
                        var tp = cell.TacticalPosition;
//                        Debug.WriteLine("Drawing overlay @ {0},{1}", tp.X, tp.Y);
                        image.DrawIntoTextureBL(Texture, TL, (uint)cell.OverlayState, MapTheater.unitPAL, h);
                    } else {
                        image.DrawIntoTextureBL(Texture, TL, 0, MapTheater.isoPAL, h);
                    }
                }
            }
        }

        public static int OverlayToTiberium(int OverlayTypeIndex) {
            if (OverlayTypeIndex == -1) {
                return -1;
            }

            var over = CCFactory<OverlayTypeClass, OverlayClass>.Get().FactoryItems[OverlayTypeIndex];
            if (!over.Tiberium) {
                return 0;
            }

            foreach (var t in TiberiumClass.All) {
                var im = t.ImageOverlay.Value.ArrayIndex;
                if (OverlayTypeIndex >= im && OverlayTypeIndex < im + t.NumImages + t.NumExtraImages) {
                    return t.ArrayIndex;
                }
            }
            return 0;
        }
    }
}
