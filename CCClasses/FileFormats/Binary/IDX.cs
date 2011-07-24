using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CCClasses.Helpers;

namespace CCClasses.FileFormats.Binary {
    public class IDX : BinaryFileFormat {
        public class IDXHeader {
            public const int ByteSize = 12;
            public int Version;
            public int SampleCount;

            public bool ReadFile(BinaryReader r) {
                String Identifier = IDX.ReadCString(r, 4);

                if (!Identifier.Equals("GABA")) {
                    return false;
                }

                Version = r.ReadInt32();

                SampleCount = r.ReadInt32();

                return true;
            }
        };

        public class WAVHeader {
            protected static readonly UInt32 hRIFF = Util.ReverseEndian(0x52494646);
            protected static readonly UInt32 hWAVE = Util.ReverseEndian(0x57415645);
            protected static readonly UInt32 hFMT = Util.ReverseEndian(0x666D7420);
            protected static readonly UInt32 hDATA = Util.ReverseEndian(0x64617461);

            protected SampleHeader IDXSample;
            protected int Format;
            protected int FileHeaderSize;
            protected int FmtExtraSize;

            public WAVHeader(SampleHeader S) {
                IDXSample = S;
            }

            protected virtual int BytesPerSecond {
                get {
                    throw new NotImplementedException();
                }
            }
            protected virtual Int16 BlockAlign {
                get {
                    throw new NotImplementedException();
                }
            }

            protected virtual Int16 BitsPerSample {
                get {
                    throw new NotImplementedException();
                }
            }

            protected virtual int SamplesInChannel {
                get {
                    throw new NotImplementedException();
                }
            }

            public SampleHeader Sample {
                get {
                    return IDXSample;
                }
            }

            public int ChannelCount {
                get {
                    return IDXSample.Flags.HasFlag(SampleHeader.SampleFlags.Stereo) ? 2 : 1;
                }
            }

            public int SampleRate {
                get {
                    return IDXSample.SampleRate;
                }
            }

            public byte[] Compile(ArraySegment<byte> bag) {
                Int32 dataLen = FileHeaderSize + IDXSample.Size;
                var data = new byte[dataLen];

                var seg = new ArraySegment<byte>(data, 0, 4);
                Util.WriteInt(seg, (Int32)hRIFF);

                seg = new ArraySegment<byte>(data, 4, 4);
                Util.WriteInt(seg, dataLen);

                seg = new ArraySegment<byte>(data, 8, 4);
                Util.WriteInt(seg, (Int32)hWAVE);

                seg = new ArraySegment<byte>(data, 12, 4);
                Util.WriteInt(seg, (Int32)hFMT);

                seg = new ArraySegment<byte>(data, 16, 4);
                Util.WriteInt(seg, 16 + FmtExtraSize);

                seg = new ArraySegment<byte>(data, 20, 2);
                Util.WriteInt(seg, (Int16)Format);

                seg = new ArraySegment<byte>(data, 22, 2);
                Util.WriteInt(seg, (Int16)ChannelCount);

                seg = new ArraySegment<byte>(data, 24, 4);
                Util.WriteInt(seg, SampleRate);

                seg = new ArraySegment<byte>(data, 28, 4);
                Util.WriteInt(seg, BytesPerSecond);

                seg = new ArraySegment<byte>(data, 32, 2);
                Util.WriteInt(seg, BlockAlign);

                seg = new ArraySegment<byte>(data, 34, 2);
                Util.WriteInt(seg, BitsPerSample);

                int ExtraBytes = WriteExtraHeader(ref data, 36);
                if (36 + ExtraBytes + 8 != FileHeaderSize) {
                    throw new InvalidDataException();
                }

                seg = new ArraySegment<byte>(data, FileHeaderSize - 8, 4);
                Util.WriteInt(seg, (Int32)hDATA);

                seg = new ArraySegment<byte>(data, FileHeaderSize - 4, 4);
                Util.WriteInt(seg, IDXSample.Size);

                Buffer.BlockCopy(bag.Array, IDXSample.Offset, data, FileHeaderSize, IDXSample.Size);

                return data;
            }

            protected virtual int WriteExtraHeader(ref byte[] data, int offset) {
                return 0;
            }
        };

        public class PCMHeader : WAVHeader {
            protected const int BytesPerSample = 2;

            public PCMHeader(SampleHeader S) : base(S) {
                Format = 1;
                FileHeaderSize = 44;
            }

            protected override int BytesPerSecond {
                get {
                    return BytesPerSample * ChannelCount * SampleRate;
                }
            }

            protected override short BlockAlign {
                get {
                    return (short)(BytesPerSample * ChannelCount);
                }
            }

            protected override short BitsPerSample {
                get {
                    return BytesPerSample << 3;
                }
            }

            protected override int SamplesInChannel {
                get {
                    return IDXSample.Size / ChannelCount >> 1;
                }
            }
        };

        public class ADPCMHeader : WAVHeader {
            protected const Int16 ExtraValue = 1017;
            protected static readonly UInt32 hFACT = Util.ReverseEndian(0x66616374);

            public ADPCMHeader(SampleHeader S) : base(S) {
                FileHeaderSize = 60;
                FmtExtraSize = 4;
                Format = 0x11;
            }

            protected override int BytesPerSecond {
                get {
                    return (11100 * ChannelCount * SampleRate) / 22050;
                }
            }

            protected override short BlockAlign {
                get {
                    return (short)(512 * ChannelCount);
                }
            }

            protected override short BitsPerSample {
                get {
                    return 4;
                }
            }

            protected override int SamplesInChannel {
                get {
                    return IDXSample.Size / ChannelCount >> 1;
                }
            }

            protected override int WriteExtraHeader(ref byte[] data, int offset) {
                var seg = new ArraySegment<byte>(data, offset, 2);
                Util.WriteInt(seg, (Int16)2);

                seg = new ArraySegment<byte>(data, offset + 2, 2);
                Util.WriteInt(seg, ExtraValue);

                seg = new ArraySegment<byte>(data, offset + 4, 4);
                Util.WriteInt(seg, (Int32)hFACT);

                seg = new ArraySegment<byte>(data, offset + 8, 4);
                Util.WriteInt(seg, (Int32)4);

                seg = new ArraySegment<byte>(data, offset + 12, 4);
                Util.WriteInt(seg, (Int32)0);

                return 16;
            }
        };

        public class SampleHeader {
            public enum SampleFlags {
                Stereo = 0x1,
                PCM = 0x2,
                Unknown4 = 0x4,
                ADPCM = 0x8,
            };

            public const int ByteSize = 36;

            public String Name;
            public int Offset;
            public int Size;
            public int SampleRate;
            public SampleFlags Flags;
            public int ChunkSize;

            public void ReadFile(ArraySegment<byte> data, bool NewVersion) {
                var offs = data.Offset;
                Name = IDX.ReadCString(data, 16);
                Offset = BitConverter.ToInt32(data.Array, offs + 16);
                Size = BitConverter.ToInt32(data.Array, offs + 20);
                SampleRate = BitConverter.ToInt32(data.Array, offs + 24);
                Flags = (SampleFlags)BitConverter.ToInt32(data.Array, offs + 28);
                if (NewVersion) {
                    ChunkSize = BitConverter.ToInt32(data.Array, offs + 32);
                }
            }

            public WAVHeader GetWaveHeader() {
                if (Flags.HasFlag(SampleFlags.PCM)) {
                    if (ChunkSize != 0) {
                        throw new InvalidDataException();
                    }
                    return new PCMHeader(this);
                } else if (Flags.HasFlag(SampleFlags.ADPCM)) {
                    return new ADPCMHeader(this);
                } else {
                    throw new InvalidDataException();
                }
            }
        };

        public IDXHeader Header = new IDXHeader();

        public Dictionary<String, SampleHeader> Samples = new Dictionary<string, SampleHeader>();

        public IDX(String filename = null) : base(filename) {
        }

        public SampleHeader this[String Name] {
            get {
                if (Samples.ContainsKey(Name)) {
                    return Samples[Name];
                }
                return null;
            }
        }


        public override bool ReadFile(BinaryReader r, long length) {
            if (length < IDXHeader.ByteSize) {
                return false;
            }

            if (!Header.ReadFile(r)) {
                return false;
            }

            if (Header.SampleCount > 0) {
                var oldVersion = Header.Version == 1;
                var offs = IDXHeader.ByteSize;
                int sampleSize = oldVersion ? 32 : 36;
                var dataSize = Header.SampleCount * sampleSize;

                if (offs + dataSize > length) {
                    return false;
                }

                var data = r.ReadBytes(dataSize);

                for (var i = 0; i < Header.SampleCount; ++i) {
                    var S = new SampleHeader();
                    var seg = new ArraySegment<byte>(data, i * sampleSize, sampleSize);
                    S.ReadFile(seg, !oldVersion);
                    Samples.Add(S.Name, S);
                }
            }

            return true;
        }
    }
}
