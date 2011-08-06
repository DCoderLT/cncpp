using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.AbstractHierarchy {
    public class OverlayTypeClass : ObjectTypeClass {
        public int DamageLevels;
        public bool Wall;
        public bool Tiberium;
        public bool Crate;
        public bool CrateTrigger;
        public bool NoUseTileLandType;
        public bool IsVeinholeMonster;
        public bool IsVeins;
        public bool Explodes;
        public bool ChainReaction;
        public bool Overrides;
        public bool DrawFlat;
        public bool IsRubble;
        public bool IsARock;

        public override AbstractID WhatAmI() {
            return AbstractID.OverlayType;
        }

        public override bool ReadFromINI(FileFormats.Text.INI iniFile) {
            if (!base.ReadFromINI(iniFile)) {
                return false;
            }

            iniFile.GetInteger(ID, "DamageLevels", ref DamageLevels);

            iniFile.GetBool(ID, "Wall", ref Wall);
            iniFile.GetBool(ID, "Tiberium", ref Tiberium);
            iniFile.GetBool(ID, "Crate", ref Crate);
            iniFile.GetBool(ID, "CrateTrigger", ref CrateTrigger);
            iniFile.GetBool(ID, "NoUseLandTileType", ref NoUseTileLandType);
            iniFile.GetBool(ID, "IsVeinholeMonster", ref IsVeinholeMonster);
            iniFile.GetBool(ID, "IsVeins", ref IsVeins);
            iniFile.GetBool(ID, "Explodes", ref Explodes);
            iniFile.GetBool(ID, "ChainReaction", ref ChainReaction);
            iniFile.GetBool(ID, "Overrides", ref Overrides);
            iniFile.GetBool(ID, "DrawFlat", ref DrawFlat);
            iniFile.GetBool(ID, "IsRubble", ref IsRubble);
            iniFile.GetBool(ID, "IsARock", ref IsARock);

            return true;
        }


        public static void LoadListFromINI(FileFormats.Text.INI iniFile) {
            var All = CCFactory<OverlayTypeClass, OverlayClass>.Get();
            All.FactoryItems.Clear();

            if (iniFile.SectionExists("OverlayTypes")) {
                foreach (var entry in iniFile.Sections["OverlayTypes"].Entries) {
                    var ident = entry.Value.Value;
                    All.FindOrAllocate(ident);
                }
            }
        }
    }
}
