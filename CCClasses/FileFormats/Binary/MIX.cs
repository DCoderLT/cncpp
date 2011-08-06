using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CCClasses.Libraries;
using System.Diagnostics;

namespace CCClasses.FileFormats.Binary {
    public class MIX : BinaryFileFormat {

        public static List<MIX> LoadedMIXes = new List<MIX>();

        public enum MIXFlags {
            HasChecksum = 0x00010000,
            HasEncryption = 0x00020000
        }
        public class FileHeader {
            public UInt16 FileCount = 0;
            public UInt32 Size = 0;

            public bool ReadFile(ArraySegment<byte> input) {
                var ofs = input.Offset;
                FileCount = BitConverter.ToUInt16(input.Array, ofs + 0);
                Size = BitConverter.ToUInt32(input.Array, ofs + 2);

                return true;
            }
        };

        public class EntryHeader {
            public UInt32 Hash;
            public UInt32 Offset;
            public UInt32 Length;

            public bool ReadFile(ArraySegment<byte> input) {
                var ofs = input.Offset;
                Hash = BitConverter.ToUInt32(input.Array, ofs + 0);
                Offset = BitConverter.ToUInt32(input.Array, ofs + 4);
                Length = BitConverter.ToUInt32(input.Array, ofs + 8);

                return true;
            }

        };

        public MIXFlags Flags = 0;
        public FileHeader Header = new FileHeader();
        public Dictionary<UInt32, EntryHeader> Entries = new Dictionary<uint, EntryHeader>();

        public uint HeadLength = 0;
        byte[] headerBytes;

        public uint FileLength;

        public uint BodyStart {
            get {
                return HeadLength;// +(Flags.HasFlag(MIXFlags.HasChecksum) ? 20u : 0);
            }
        }

        public uint BodyLength {
            get {
                return FileLength - BodyStart;
            }
        }

        protected BinaryReader source;

        public MIX(CCFileClass ccFile = null)
            : base(ccFile) {
        }

        public void Release() {
            var idx = LoadedMIXes.FindIndex(M => M == this);
            if (idx != -1) {
                LoadedMIXes.RemoveAt(idx);
            }
        }

        protected override bool ReadFile(BinaryReader r) {
            source = r;
            var length = (int)r.BaseStream.Length;
            this.FileLength = (uint)length;

            if (length < 10) {
                ParseError("File length too short to contain file header.");
                return false;
            }

            var fileBytes = r.ReadBytes((int)length);

            UInt32 flags = BitConverter.ToUInt32(fileBytes, 0);
            if ((flags & 0x0000FFFF) != 0) {
                var s = new ArraySegment<byte>(fileBytes, 0, 6);
                if (!Header.ReadFile(s)) {
                    ParseError("Failed to read file header (old style).");
                    return false;
                }

                var headLen = Header.FileCount * 12;
                if (length < (6 + headLen)) {
                    ParseError("File length too short to contain entry headers.");
                    return false;
                }

                HeadLength = (uint)(6 + headLen);
                headerBytes = new byte[HeadLength];
                Buffer.BlockCopy(fileBytes, 0, headerBytes, 0, (int)HeadLength);
            } else {
                if ((flags & (uint)MIXFlags.HasChecksum) != 0) {
                    Flags |= MIXFlags.HasChecksum;
                }
                if ((flags & (uint)MIXFlags.HasEncryption) != 0) {
                    Flags |= MIXFlags.HasEncryption;
                }

                if (Flags.HasFlag(MIXFlags.HasEncryption)) {
                    // uh oh

                    var key80 = fileBytes.Skip(4).Take(80).ToArray();
                    var key56 = new byte[56];
                    MIX_Magic.get_blowfish_key(key80, ref key56);

                    var bf = new Blowfish(key56);
                    var header = fileBytes.Skip(84).Take(8).ToArray();
                    bf.Decipher(header, 8);
                    var s = new ArraySegment<byte>(header, 0, 6);
                    if (!Header.ReadFile(s)) {
                        ParseError("Failed to read file header (encrypted style, yo).");
                        return false;
                    }

                    var hSize = Header.FileCount * 12 + 6;
                    hSize += 8 - (hSize % 8);
                    var blockCount = hSize >> 3;
                    HeadLength = (uint)(84 + hSize);

                    if (length < (84 + hSize)) {
                        ParseError("File length too short to contain entry headers.");
                        return false;
                    }

                    var decoded = new byte[hSize];
                    Buffer.BlockCopy(header, 0, decoded, 0, 8);

                    --blockCount;

                    var encoded = new byte[blockCount * 8];
                    Buffer.BlockCopy(fileBytes, 92, encoded, 0, blockCount * 8);
                    for (var i = 0; i < blockCount; ++i) {
                        bf.Decipher(encoded, 8, i * 8);
                    }
                    Buffer.BlockCopy(encoded, 0, decoded, 8, blockCount * 8);

                    headerBytes = decoded;
                } else {
                    var s = new ArraySegment<byte>(fileBytes, 4, 6);
                    if (!Header.ReadFile(s)) {
                        ParseError("Failed to read file header (new style).");
                        return false;
                    }

                    var headLen = Header.FileCount * 12;
                    if (length < (10 + headLen)) {
                        ParseError("File length too short to contain entry headers.");
                        return false;
                    }

                    HeadLength = (uint)(10 + headLen);
                    headerBytes = new byte[HeadLength - 4];
                    Buffer.BlockCopy(fileBytes, 4, headerBytes, 0, (int)HeadLength - 4);
                }
            }
            if (!ReadEntryHeaders()) {
                ParseError("Failed to read entry headers.");
                return false;
            }

            ParseLMD();

            return true;
        }

        private void ParseLMD() {
            var LMD = GetFileContents(0x366E051F);

            if (LMD == null) {
                return;
            }

            using (var r = new BinaryReader(LMD)) {
                r.BaseStream.Seek(0x20, SeekOrigin.Begin);
                uint len = r.ReadUInt32(),
                    lmdType = r.ReadUInt32(),
                    lmdVer = r.ReadUInt32(),
                    gameVer = r.ReadUInt32(),
                    nameCount = r.ReadUInt32();

                var contentLen = r.BaseStream.Length - 0x34;

                if (len > contentLen) {
                    len = (uint)contentLen;
                }

                List<String> names = new List<String>();

                for (var i = 0u; i < nameCount; ++i) {
                    String s = "";
                    char c;
                    do {
                        c = r.ReadChar();
                        if (c == 0) {
                            break;
                        }
                        s += c;
                        --len;
                    } while (len > 0);

                    if (s.Length > 0) {
                        names.Add(s);
                    }
                }


            }
        }

        bool ReadEntryHeaders() {
            for (var i = 0; i < Header.FileCount; ++i) {
                var s = new ArraySegment<byte>(headerBytes, (6 + i * 12), 12);
                var Entry = new EntryHeader();
                if (!Entry.ReadFile(s)) {
                    return false;
                }

                if (Entry.Offset + Entry.Length > BodyLength) {
                    return false;
                }

                Entries.Add(Entry.Hash, Entry);
            }

            return true;
        }

        public MemoryStream GetFileContents(UInt32 hash) {
            if (Entries.ContainsKey(hash)) {
                var e = Entries[hash];
                int pos = (int)(BodyStart + e.Offset);
                int len = (int)e.Length;


                source.BaseStream.Seek(pos, SeekOrigin.Begin);
                var contents = source.ReadBytes(len);
                return new MemoryStream(contents, 0, len, false, false);
            }
            return null;
        }

        public MemoryStream GetFileContents(String filename) {
            var hash = MIX_Magic.getID(filename);
            return GetFileContents(hash);
        }

        public bool ContainsFile(String filename) {
            var hash = MIX_Magic.getID(filename);
            return Entries.ContainsKey(hash);
        }

        public IEnumerable<string> EntriesText {
            get {
                return Entries.Select(E => String.Format("ID: {0:X8} Offset: {1:X8}  Size: {2:X8}", E.Value.Hash, E.Value.Offset, E.Value.Length));
            }
        }
    }
}
