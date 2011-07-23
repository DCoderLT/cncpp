using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.FileFormats.Binary {
    public class BAG : BinaryFileFormat {
        public byte[] Data;

        public BAG(String filename = null) : base(filename) { 
        }

        public override bool ReadFile(System.IO.BinaryReader r, long length) {
            Data = r.ReadBytes((int)length);
            return true;
        }

        private ArraySegment<byte> _Segment;

        public ArraySegment<byte> Segment {
            get {
                if(_Segment == null) {
                    _Segment = new ArraySegment<byte>(Data);
                }
                return _Segment;
            }
        }
    }
}
