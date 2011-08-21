using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CCClasses.AbstractHierarchy {
    public abstract class ObjectClass : AbstractClass {
        public ObjectClass() : base() {
        }

        public override AbstractID WhatAmI() {
            return AbstractID.Object;
        }

        public override bool isObject {
            get {
                return true;
            }
        }

        public CoordStruct Location;
        public bool BombVisible;
        public int Health;
        public bool IsAlive;
        public bool IsSensed;
        public bool InLimbo;
        public bool InOpenToppedTransport;
        public bool IsSelected;
        public bool HasParachute;
        public bool OnBridge;
        public bool IsFallingDown;
        public int FallRate;
        public bool IsABomb;

        public Helpers.WeakRef<ObjectClass> NextObject = new Helpers.WeakRef<ObjectClass>(null);

        public virtual bool Put(CoordStruct XYZ) {
            Location = XYZ;
            InLimbo = false;
            IsSensed = false;

            var xy = XYZ.ToCell();

            MapClass.Instance.GetCellAt(xy).AddContent(this);

            return true;
        }

        public virtual CellClass GetCell() {
            var xy = Location.ToCell();
            return MapClass.Instance.GetCellAt(xy);
        }

        public bool IsVisible() {
            return GetCell().VisibleInTactical;
        }

        public virtual void Draw(Helpers.ZBufferedTexture Texture) {
            var Type = GetObjectType();
            if (Type.Voxel) {
            } else {
                var image = Type.SHPImage.Value;
                if(image != null) {
                    var center = GetCell().TacticalPosition;
                    image.DrawIntoTexture(Texture, center, 0, MapTheater.unitPAL);
                }
            }
        }
    }
}
