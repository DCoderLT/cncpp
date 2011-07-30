using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CCClasses.FileFormats.Binary {
    public class BAG : BinaryFileFormat {
        public byte[] Data;

        public BAG(CCFileClass ccFile = null) : base(ccFile) { 
        }

        protected override bool ReadFile(BinaryReader r) {
            Data = r.ReadBytes((int)r.BaseStream.Length);
            return true;
        }

        private ArraySegment<byte> _Segment;
        private bool SegmentInitialized = false;

        public ArraySegment<byte> Segment {
            get {
                if (!SegmentInitialized) {
                    _Segment = new ArraySegment<byte>(Data);
                    SegmentInitialized = true;
                }
                return _Segment;
            }
        }
    }
}
