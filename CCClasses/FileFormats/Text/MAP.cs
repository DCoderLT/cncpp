using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CCClasses.Libraries;

namespace CCClasses.FileFormats.Text {
    public class MAP : TextFileFormat {
        public class TilePacked {
            public const int ByteSize = 11;
            public Int16 X;
            public Int16 Y;
            public UInt32 TileTypeIndex;
            public byte Height;
            public byte Level;
            public byte Unknown;

            public void ReadFile(ArraySegment<byte> data) {
                var offs = data.Offset;
                X = BitConverter.ToInt16(data.Array, offs);
                Y = BitConverter.ToInt16(data.Array, offs + 2);
                TileTypeIndex = BitConverter.ToUInt32(data.Array, offs + 4);
                Height = data.Array[offs + 8];
                Level = data.Array[offs + 9];
                Unknown = data.Array[offs + 10];
            }
        };

        public MAP(String filename = null) : base(filename) {
        }

        private INI MapINI;

        private byte[] IsoMapPack;

        public List<TilePacked> Tiles = new List<TilePacked>();


        public override bool ReadFile(String filename) {
            MapINI = new INI(filename);

            if (!MapINI.SectionExists("IsoMapPack5")) {
                return false;
            }

            var MappackCompressed = MapINI.ReadSection("IsoMapPack5");

            var MappackUn64 = Convert.FromBase64String(MappackCompressed);

            var unpacked = new List<byte>();

            var offs = 0;
            while (offs < MappackUn64.Length) {
                int InputSize = BitConverter.ToInt16(MappackUn64, offs);
                int OutputSize = BitConverter.ToInt16(MappackUn64, offs + 2);
                offs += 4;
                if (offs + InputSize < MappackUn64.Length) {
                    var Input = new byte[InputSize];
                    Buffer.BlockCopy(MappackUn64, offs, Input, 0, InputSize);

                    var Output = new byte[OutputSize * 2];

                    //if (LZO.Decompress_XCC(Input, ref Output) != 0) {
                    //    throw new InvalidDataException();
                    //}

                    var wmem = new byte[0x80000];

                    int decompressedSize = 0;

                    Simplicit.Net.Lzo.LZOCompressor.lzo1x_decompress(Input, InputSize, Output, ref decompressedSize, wmem);

                    if (decompressedSize != OutputSize) {
                        throw new InvalidDataException();
                    }

                    unpacked.AddRange(Output.Take(OutputSize));
                }
                offs += InputSize;
            }
            
            IsoMapPack = unpacked.ToArray();

            var TileCount = IsoMapPack.Length / TilePacked.ByteSize;

            Tiles.Clear();

            for (var i = 0; i < TileCount; ++i) {
                var seg = new ArraySegment<byte>(IsoMapPack, i * TilePacked.ByteSize, TilePacked.ByteSize);

                var T = new TilePacked();
                T.ReadFile(seg);
                Tiles.Add(T);
            }

            return true;
        }
    }
}
