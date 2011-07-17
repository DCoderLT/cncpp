﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CCClasses.Libraries;

namespace CCClasses.FileFormats.Binary {
    public class MIX : BinaryFileFormat {
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
        byte[] fileBytes;

        public uint FileLength;

        public uint BodyStart {
            get {
                return HeadLength + (Flags.HasFlag(MIXFlags.HasChecksum) ? 0x20u : 0);
            }
        }

        public uint BodyLength {
            get {
                return FileLength - HeadLength;
            }
        }

        public MIX(String filename = null) : base(filename) {
        }
        
        public override bool ReadFile(BinaryReader r, long length) {
            this.FileLength = (uint)length;

            if (length < 10) {
                return false;
            }

            fileBytes = r.ReadBytes((int)length);

            UInt32 flags = BitConverter.ToUInt32(fileBytes, 0);
            if ((flags & 0x0000FFFF) != 0) {
                return false;
            }
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
                    return false;
                }

                var hSize = Header.FileCount * 12 + 6;
                hSize += (hSize % 8);
                var blockCount = hSize >> 3;

                if (length < (84 + hSize)) {
                    return false;
                }

                var decoded = new byte[hSize];
                Buffer.BlockCopy(header, 0, decoded, 0, 8);

                var encoded = new byte[8];
                for (var i = 1; i < blockCount; ++i) {
                    encoded = fileBytes.Skip(84 + 8 * i).Take(8).ToArray();
                    bf.Decipher(encoded, 8);
                    Buffer.BlockCopy(encoded, 0, decoded, i * 8, 8);
                }

                HeadLength = (uint)(84 + 8 * blockCount);
                headerBytes = decoded;
            } else {
                var s = new ArraySegment<byte>(fileBytes, 4, 6);
                if (!Header.ReadFile(s)) {
                    return false;
                }

                var headLen = Header.FileCount * 12;
                if (length < (10 + headLen)) {
                    return false;
                }

                HeadLength = (uint)(10 + headLen);
                headerBytes = new byte[HeadLength];
                Buffer.BlockCopy(fileBytes, 4, headerBytes, 0, (int)HeadLength);
            }

            if (!ReadEntryHeaders()) {
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

        MemoryStream GetFileContents(UInt32 hash) {
            if (Entries.ContainsKey(hash)) {
                var e = Entries[hash];
                int pos = (int)(BodyStart + e.Offset);
                int len = (int)e.Length;
                return new MemoryStream(fileBytes, pos, len, false, false);
            }
            return null;
        }
    }
}