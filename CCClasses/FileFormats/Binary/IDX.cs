using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

        public class PCMHeader {
            public const int BytesPerSample = 2;
            public const int BitsPerSample = 16;
            public const int ByteSize = 44;
            public int Format;
            public int ChannelCount;
            public int SampleRate;

            private SampleHeader _Sample;
            public SampleHeader IDXSample {
                get {
                    return _Sample;
                }
                set {
                    _Sample = value;
                    Format = 1;
                    ChannelCount = (value.Flags & 1) == 0 ? 1 : 2;
                    SampleRate = value.SampleRate;
                }
            }

            public byte[] Compile(ArraySegment<byte> bag) {
                Int32 dataLen = ByteSize + IDXSample.Size;
                var data = new byte[dataLen];

                data[0] = 0x52;
                data[1] = 0x49;
                data[2] = 0x46;
                data[3] = 0x46;

                var seg = new ArraySegment<byte>(data, 4, 4);
                Util.WriteInt(seg, dataLen);

                data[8] = 0x57;
                data[9] = 0x41;
                data[10] = 0x56;
                data[11] = 0x45;
                
                data[12] = 0x66;
                data[13] = 0x6D;
                data[14] = 0x74;
                data[15] = 0x20;

                data[16] = 0x10;
                data[17] = 0;
                data[18] = 0;
                data[19] = 0;

                seg = new ArraySegment<byte>(data, 20, 2);
                Util.WriteInt(seg, (Int16)Format);

                seg = new ArraySegment<byte>(data, 22, 2);
                Util.WriteInt(seg, (Int16)ChannelCount);

                seg = new ArraySegment<byte>(data, 24, 4);
                Util.WriteInt(seg, SampleRate);

                var bpsChan = BitsPerSample * ChannelCount;
                var bpsSamples = bpsChan * SampleRate;

                seg = new ArraySegment<byte>(data, 28, 4);
                Util.WriteInt(seg, bpsSamples / 8);

                seg = new ArraySegment<byte>(data, 32, 2);
                Util.WriteInt(seg, bpsChan / 8);

                seg = new ArraySegment<byte>(data, 34, 2);
                Util.WriteInt(seg, (Int16)BitsPerSample);

                data[36] = 0x64;
                data[37] = 0x61;
                data[38] = 0x74;
                data[39] = 0x61;

                seg = new ArraySegment<byte>(data, 40, 4);
                Util.WriteInt(seg, IDXSample.Size);

                Buffer.BlockCopy(bag.Array, IDXSample.Offset, data, ByteSize, IDXSample.Size);

                return data;
            }
        };

        public class SampleHeader {
            public const int ByteSize = 36;

            public String Name;
            public int Offset;
            public int Size;
            public int SampleRate;
            public int Flags;
            public int ChunkSize;

            public void ReadFile(ArraySegment<byte> data, bool NewVersion) {
                var offs = data.Offset;
                Name = IDX.ReadCString(data, 16);
                Offset = BitConverter.ToInt32(data.Array, offs + 16);
                Size = BitConverter.ToInt32(data.Array, offs + 20);
                SampleRate = BitConverter.ToInt32(data.Array, offs + 24);
                Flags = BitConverter.ToInt32(data.Array, offs + 28);
                if (NewVersion) {
                    ChunkSize = BitConverter.ToInt32(data.Array, offs + 32);
                }
            }

            public PCMHeader GetPCM() {
                var pcm = new PCMHeader();
                pcm.IDXSample = this;
                return pcm;
            }
        };

        public IDXHeader Header = new IDXHeader();

        public List<SampleHeader> Samples = new List<SampleHeader>();

        public IDX(String filename = null) : base(filename) {
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
                    Samples.Add(S);
                }
            }

            return true;
        }
    }
}
