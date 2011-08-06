using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.AbstractHierarchy {
    public enum AbstractID {
        Abstract = 1,
        AbstractType = 2,
        Object = 3,
        ObjectType = 4,

        Overlay = 5,
        OverlayType = 6,
    };

    public class BaseClass {

        public static readonly Dictionary<AbstractID, String> AbstractIDs = new Dictionary<AbstractID, string>() {
            {AbstractID.Abstract, "AbstractClass"},
            {AbstractID.AbstractType, "AbstractTypeClass"},
            {AbstractID.Object, "ObjectClass"},
            {AbstractID.ObjectType, "ObjectTypeClass"},
            {AbstractID.Overlay, "OverlayClass"},
            {AbstractID.OverlayType, "OverlayTypeClass"},
        };

        public uint UniqueID;
        public bool isDirty;

        public virtual bool isObject {
            get {
                return false;
            }
        }

        public virtual bool isTechno {
            get {
                return false;
            }
        }

        public virtual bool isFoot {
            get {
                return false;
            }
        }

        public virtual AbstractID WhatAmI() {
            return AbstractID.Abstract;
        }

        public virtual CoordStruct GetCoordsLeptons() {
            return new CoordStruct();
        }

        public virtual void Update() {
        }

        public virtual bool IsOnFloor() {
            return false;
        }

        public virtual bool IsInAir() {
            return false;
        }

        public virtual void Dispose() {
        }
    }

    public class CCFactory<TFactory, TProduction>
        where TFactory : AbstractTypeClass, new()
        where TProduction : AbstractClass, new() {

        public List<TFactory> FactoryItems = new List<TFactory>();
        public List<TProduction> ProductionItems = new List<TProduction>();

        private static CCFactory<TFactory, TProduction> Instance;
        public static CCFactory<TFactory, TProduction> Get() {
            if (Instance == null) {
                Instance = new CCFactory<TFactory, TProduction>();
            }
            return Instance;
        }

        public TProduction CreateObject(TFactory Fact) {
            var o = new TProduction() {
            };
            o.SetObjectType(Fact as ObjectTypeClass);
            ProductionItems.Add(o);
            return o;
        }

        public void CreatedObject(TProduction Prod) {
            ProductionItems.Add(Prod);
        }

        public TFactory CreateFactory(String ident) {
            var o = new TFactory() {
                ID = ident
            };
            CreatedFactory(o);
            return o;
        }

        public void CreatedFactory(TFactory Fact) {
            Fact.ArrayIndex = FactoryItems.Count;
            FactoryItems.Add(Fact);
        }

        public TFactory Find(String ID) {
            return FactoryItems.Find(f => f.ID.Equals(ID));
        }

        public TFactory FindOrAllocate(String ID) {
            var o = Find(ID);
            if (o == null) {
                o = CreateFactory(ID);
            }
            return o;
        }

        public int FindIndex(String ID) {
            return FactoryItems.FindIndex(f => f.ID.Equals(ID));
        }

        public void ReadAllFromINI(FileFormats.Text.INI iniFile) {
            foreach (var f in FactoryItems) {
                f.ReadFromINI(iniFile);
            }
        }
    }
}
