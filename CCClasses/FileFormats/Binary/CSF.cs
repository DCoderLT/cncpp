using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CCClasses.FileFormats.Binary {
    public class FileHeader {
        public static readonly List<String> Languages = new List<string>() {
            "US (English)",
            "UK (English)",
            "German",
            "French",
            "Spanish",
            "Italian",
            "Japanese",
            "Jabberwockie",
            "Korean",
            "Chinese",
            "Unknown",
        };

        public const int ByteSize = 24;
        public int Version;
        public int NumValues;
        public int NumExtraValues;
        public int Unused;
        public int Language;

        public String LanguageLabel {
            get {
                if (Language >= Languages.Count - 1) {
                    return Languages[Languages.Count - 1];
                }
                return Languages[Language];
            }
        }

        public bool ReadFile(BinaryReader r) {
            String Identifier = CSF.ReadCString(r, 4);

            if (!Identifier.Equals(" FSC")) {
                return false;
            }

            Version = r.ReadInt32();

            if (Version != 3) {
                return false;
            }

            NumValues = r.ReadInt32();

            NumExtraValues = r.ReadInt32();

            Unused = r.ReadInt32();

            Language = r.ReadInt32();

            return true;
        }
    };

    public class CSFValue {
        public String Plain = "?";
        public String Extra = "?";
    }

    public class Label {
        public const int ByteSize = 0xC;

        public int StringCount;
        public String ID;

        public List<CSFValue> Values = new List<CSFValue>();

        public String Val {
            get {
                return Values[0].Plain;
            }
        }

        public String ExtraVal {
            get {
                return Values[0].Extra;
            }
        }

        public bool ReadFile(ArraySegment<byte> data, ref int size) {
            var offs = data.Offset;

            String Identifier = CSF.ReadCString(data, 4, offs);
            offs += 4;

            if (!Identifier.Equals(" LBL")) {
                return false;
            }

            StringCount = BitConverter.ToInt32(data.Array, offs);
            offs += 4;

            uint LabelLength = BitConverter.ToUInt32(data.Array, offs);
            offs += 4;

            if (offs + LabelLength > data.Array.Length) {
                return false;
            }

            ID = CSF.ReadCString(data, LabelLength, offs);
            offs += (int)LabelLength;

            for (var ixStr = 0; ixStr < StringCount; ++ixStr) {
                if (offs + 8 > data.Array.Length) {
                    return false;
                }

                String strIdent = CSF.ReadCString(data, 4, offs);
                offs += 4;

                var isPlain = strIdent.Equals(" RTS");
                var isExtended = strIdent.Equals("WRTS");

                if (isPlain || isExtended) {
                    var Val = new CSFValue();

                    var VLen = BitConverter.ToInt32(data.Array, offs);
                    offs += 4;

                    if (offs + VLen * 2 > data.Array.Length) {
                        return false;
                    }

                    for (var i = 0; i < VLen; ++i) {
                        var raw = BitConverter.ToUInt16(data.Array, offs);
                        offs += 2;
                        raw ^= 0xFFFF;

                        Val.Plain += (char)raw;
                    }

                    if (isExtended) {
                        if (offs + 4 > data.Array.Length) {
                            return false;
                        }

                        var XLen = BitConverter.ToUInt32(data.Array, offs);
                        offs += 4;

                        if (offs + XLen > data.Array.Length) {
                            return false;
                        }

                        Val.Extra = CSF.ReadCString(data, XLen, offs);
                        offs += (int)XLen;
                    }

                    Values.Add(Val);

                } else {
                    return false;
                }

            }

            size = offs - data.Offset;

            return true;
        }
    };

    public class CSF : BinaryFileFormat {

        public CSF(String filename) : base(filename) {
        }

        public FileHeader Header = new FileHeader();

        public Dictionary<String, Label> Labels = new Dictionary<string, Label>();

        public override bool ReadFile(BinaryReader r, long length) {
            if (length < FileHeader.ByteSize) {
                return false;
            }

            if (!Header.ReadFile(r)) {
                return false;
            }

            var size = 0;
            var pos = 0;

            var data = r.ReadBytes((int)length - FileHeader.ByteSize);

            for (var i = 0; i < Header.NumValues; ++i) {
                var L = new Label();
                var seg = new ArraySegment<byte>(data, pos, 0);
                if (L.ReadFile(seg, ref size)) {
                    pos += size;
                    Labels[L.ID] = L;
                } else {
                    return false;
                }
            }

            return true;
        }
    }
}
