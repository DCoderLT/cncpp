using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.AbstractHierarchy {
    public class TiberiumClass : AbstractTypeClass {

        public static CCTypeCollection<TiberiumClass> All = new CCTypeCollection<TiberiumClass>();

        public int Spread;
        public double SpreadPercentage;
        public int Growth;
        public double GrowthPercentage;
        public int Value;
        public int Power;

        public Helpers.WeakRef<OverlayTypeClass> ImageOverlay;
        public int NumFrames;
        public int NumImages;
        public int NumExtraImages;

        public TiberiumClass()
            : base() {
        }

        public override void Dispose() {
            base.Dispose();
        }

        public override AbstractID WhatAmI() {
            return AbstractID.Tiberium;
        }

        public override bool ReadFromINI(FileFormats.Text.INI iniFile) {
            if (!base.ReadFromINI(iniFile)) {
                return false;
            }

            iniFile.GetInteger(ID, "Spread", ref Spread);
            iniFile.GetDouble(ID, "SpreadPercentage", ref SpreadPercentage);
            iniFile.GetInteger(ID, "Growth", ref Growth);
            iniFile.GetDouble(ID, "GrowthPercentage", ref GrowthPercentage);

            iniFile.GetInteger(ID, "Value", ref Value);
            iniFile.GetInteger(ID, "Power", ref Power);

            var ImageIndex = -1;

            iniFile.GetInteger(ID, "Image", ref ImageIndex);

            var lst = CCFactory<OverlayTypeClass, OverlayClass>.Get().FactoryItems;

            switch (ImageIndex) {
                case -1:
                    break;

                case 2:
                    ImageOverlay = new Helpers.WeakRef<OverlayTypeClass>(lst[0x1B]);
                    NumFrames = NumImages = 12;
                    break;

                case 3:
                    ImageOverlay = new Helpers.WeakRef<OverlayTypeClass>(lst[0x7F]);
                    NumFrames = NumImages = 12;
                    NumExtraImages = 8;
                    break;

                case 4:
                    ImageOverlay = new Helpers.WeakRef<OverlayTypeClass>(lst[0x93]);
                    NumFrames = NumImages = 12;
                    NumExtraImages = 8;
                    break;

                case 0:
                case 1:
                default:
                    ImageOverlay = new Helpers.WeakRef<OverlayTypeClass>(lst[0x66]);
                    NumFrames = NumImages = 12;
                    NumExtraImages = 8;
                    break;

            }

            return true;
        }

        public static void LoadListFromINI(FileFormats.Text.INI iniFile) {
            All.Clear();

            if (iniFile.SectionExists("Tiberiums")) {
                foreach (var entry in iniFile.Sections["Tiberiums"].Entries) {
                    var ident = entry.Value.Value;
                    All.FindOrAllocate(ident);
                }
            }
        }

        public OverlayTypeClass Overlay {
            get {
                return CCFactory<OverlayTypeClass, OverlayClass>.Get().FactoryItems[ImageOverlay.Value.ArrayIndex];
            }
        }

    }
}
