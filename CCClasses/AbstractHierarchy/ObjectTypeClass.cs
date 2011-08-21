using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.AbstractHierarchy {
    public class ObjectTypeClass : AbstractTypeClass {

        public ObjectTypeClass()
            : base() {
        }

        public override void Dispose() {
            base.Dispose();
        }

        public override string ID {
            get {
                return base.ID;
            }
            set {
                base.ID = value;
                if (ImageName == null) {
                    ImageName = value;
                }
            }
        }

        public int Armor;
        public int Strength;

        public String ImageName;
        public CCClasses.Helpers.WeakRef<FileFormats.Binary.SHP> SHPImage = new Helpers.WeakRef<FileFormats.Binary.SHP>(null);
        public bool SHPImageIsOutdated;

        public String AlphaImageName = "";
        public CCClasses.Helpers.WeakRef<FileFormats.Binary.SHP> AlphaImage = new Helpers.WeakRef<FileFormats.Binary.SHP>(null);

        public bool AlternateArcticArt;
        public bool Theater;
        public bool Crushable;
        public bool Bombable;
        public bool RadarInvisible;
        public bool Selectable;
        public bool LegalTarget;
        public bool Insignificant;
        public bool Immune;
        public bool Voxel;
        public bool NewTheater;
        public bool HasRadialIndicator;
        public bool IgnoresFirestorm;
        public bool UseLineTrail;

        public override AbstractID WhatAmI() {
            return AbstractID.ObjectType;
        }

        public override bool ReadFromINI(FileFormats.Text.INI iniFile) {
            if (!base.ReadFromINI(iniFile)) {
                return false;
            }

            iniFile.GetInteger(ID, "Strength", out Strength, Strength);
            iniFile.GetString(ID, "Image", out ImageName, ImageName);
            iniFile.GetString(ID, "AlphaImage", out AlphaImageName, AlphaImageName);
            iniFile.GetBool(ID, "AlternateArcticArt", out AlternateArcticArt, AlternateArcticArt);
            iniFile.GetBool(ID, "Crushable", out Crushable, Crushable);
            iniFile.GetBool(ID, "Bombable", out Bombable, Bombable);
            iniFile.GetBool(ID, "RadarInvisible", out RadarInvisible, RadarInvisible);
            iniFile.GetBool(ID, "Selectable", out Selectable, Selectable);
            iniFile.GetBool(ID, "LegalTarget", out LegalTarget, LegalTarget);
            iniFile.GetBool(ID, "Insignificant", out Insignificant, Insignificant);
            iniFile.GetBool(ID, "Immune", out Immune, Immune);
            iniFile.GetBool(ID, "HasRadialIndicator", out HasRadialIndicator, HasRadialIndicator);
            iniFile.GetBool(ID, "IgnoresFirestorm", out IgnoresFirestorm, IgnoresFirestorm);
            
            FileFormats.Text.INI.Art_INI.GetBool(ID, "UseLineTrail", ref UseLineTrail);
            FileFormats.Text.INI.Art_INI.GetBool(ID, "Theater", ref Theater);
            FileFormats.Text.INI.Art_INI.GetBool(ID, "NewTheater", ref NewTheater);
            FileFormats.Text.INI.Art_INI.GetBool(ID, "Voxel", ref Voxel);

            if (!Voxel) {
                Load2DArt();
            }

            if (AlphaImageName.Length > 0) {
                AlphaImage = new Helpers.WeakRef<FileFormats.Binary.SHP>(FileFormats.Binary.SHP.LoadFile(AlphaImageName + ".SHP"));
            } else {
                AlphaImage.Empty();
            }

            return true;
        }

        public virtual void Load2DArt() {
            var filename = ImageName;
            if (AlternateArcticArt) {
                filename += "A";
            }
            if (Theater) {
                filename += "." + MapTheater.CurrentTheater.Extension;
            } else if (NewTheater) {
                var repl = MapTheater.CurrentTheater.NewTheaterChar;
                var l2 = filename[1].ToString().ToUpper()[0];
                if (l2 >= 'A' && l2 <= 'Z') {
                    filename = filename.Substring(0, 1) + repl + filename.Substring(2);
                }
                filename += ".SHP";
            }

            var loadedFile = FileFormats.Binary.SHP.LoadFile(filename);
            if (loadedFile == null) {
                filename = filename[0] + 'G' + filename.Substring(2);
                loadedFile = FileFormats.Binary.SHP.LoadFile(filename);
            }

            SHPImage = new Helpers.WeakRef<FileFormats.Binary.SHP>(loadedFile);
        }
    }
}
