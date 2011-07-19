using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.FileFormats.Binary {
    public class TMP : BinaryFileFormat {
        public class FileHeader {
            const int ByteSize = 16;

            public UInt32 XBlocks;
            public UInt32 YBlocks;
            public UInt32 BlockWidth;
            public UInt32 BlockHeight;

            internal void ReadFile(ArraySegment<byte> data) {
                var offs = data.Offset;
                XBlocks = BitConverter.ToUInt32(data.Array, offs);
                YBlocks = BitConverter.ToUInt32(data.Array, offs + 4);
                BlockWidth = BitConverter.ToUInt32(data.Array, offs + 8);
                BlockHeight = BitConverter.ToUInt32(data.Array, offs + 12);
            }
        };

        public class TileHeader {
            const int ByteSize = 52 + 576 + 576;

            public Int32 X;
            public Int32 Y;
            public Int32 Unk1, Unk2, Unk3;
            public Int32 ExtraX;
            public Int32 ExtraY;
            public Int32 ExtraWidth;
            public Int32 ExtraHeight;
            public Int32 Unk4, Unk5, Unk6, Unk7;

            public byte[] Graphics;
            public byte[] HeightData;
            public byte[] Extras;

            internal void ReadFile(ArraySegment<byte> data) {
                var offs = data.Offset;
                X = BitConverter.ToInt32(data.Array, offs);
                Y = BitConverter.ToInt32(data.Array, offs + 4);
                Unk1 = BitConverter.ToInt32(data.Array, offs + 8);
                Unk2 = BitConverter.ToInt32(data.Array, offs + 12);
                Unk3 = BitConverter.ToInt32(data.Array, offs + 16);
                ExtraX = BitConverter.ToInt32(data.Array, offs + 20);
                ExtraY = BitConverter.ToInt32(data.Array, offs + 24);
                ExtraWidth = BitConverter.ToInt32(data.Array, offs + 28);
                ExtraHeight = BitConverter.ToInt32(data.Array, offs + 32);
                Unk4 = BitConverter.ToInt32(data.Array, offs + 36);
                Unk5 = BitConverter.ToInt32(data.Array, offs + 40);
                Unk6 = BitConverter.ToInt32(data.Array, offs + 44);
                Unk7 = BitConverter.ToInt32(data.Array, offs + 48);

                Graphics = new byte[576];
                Buffer.BlockCopy(data.Array, offs + 52, Graphics, 0, 576);

                HeightData = new byte[576];
                Buffer.BlockCopy(data.Array, offs + 52 + 576, HeightData, 0, 576);

                if (ExtraWidth > 0) {
                    var extraArea = ExtraWidth * ExtraHeight;

                    if (data.Array.Length - offs - ByteSize < extraArea) {
                        throw new IndexOutOfRangeException();
                    }

                    Extras = new byte[extraArea];
                    Buffer.BlockCopy(data.Array, offs + 52 + 576 + 576, Extras, 0, extraArea);
                }
            }
        };

        protected Int32[] TileIndices;

        public override bool ReadFile(System.IO.BinaryReader r, long length) {
            

            return true;
        }
    }
}
