using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

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
                    var yAdjust = 0;
                    if (Type.Tiberium || Type.Wall || Type.ArrayIndex == 126 || Type.Crate) {
                        yAdjust = -12;
                    }
                    if (Type.ArrayIndex == 126) {
                        --yAdjust;
                    }

                    var topLeft = GetCell().TacticalPosition - new CellStruct(0, -2 - yAdjust);
                    image.DrawIntoTexture(Texture, topLeft, 0, MapTheater.isoPAL);
                }
            }
        }
    }
}
