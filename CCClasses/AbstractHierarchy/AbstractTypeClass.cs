using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.AbstractHierarchy {
    public class AbstractTypeClass : BaseClass {
        public int ArrayIndex;

        protected String _ID;
        public virtual String ID {
            get {
                return _ID;
            }
            set {
                _ID = value;
            }
        }
        public String Name;
        public String UINameLabel;
        public String UIName;

        public AbstractTypeClass() : base() {
        }

        public override void Dispose() {
        }

        public override AbstractID WhatAmI() {
            return AbstractID.AbstractType;
        }

        public virtual bool ReadFromINI(FileFormats.Text.INI iniFile) {
            if (iniFile.SectionExists(ID)) {
                iniFile.GetString(ID, "Name", ref Name);
                if (iniFile.GetString(ID, "UIName", ref UINameLabel)) {
                    UIName = FileFormats.Binary.CSF.StringTable.GetValue(UINameLabel);
                }
                return true;
            }
            return false;
        }

    }
}
