using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.AbstractHierarchy {
    public abstract class AbstractClass : BaseClass {

        public AbstractClass() {
        }

        public override bool isObject {
            get {
                return false;
            }
        }

        public override bool isTechno {
            get {
                return false;
            }
        }

        public override bool isFoot {
            get {
                return false;
            }
        }

        public override AbstractID WhatAmI() {
            return AbstractID.Abstract;
        }

        public override CoordStruct GetCoordsLeptons() {
            return new CoordStruct();
        }

        public override void Update() {
        }

        public override bool IsOnFloor() {
            return false;
        }

        public override bool IsInAir() {
            return false;
        }

        public override void Dispose() {
        }

        public virtual ObjectTypeClass GetObjectType() {
            return null;
        }

        public virtual void SetObjectType(ObjectTypeClass T) {
        }
    }
}
