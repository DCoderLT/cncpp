using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.Helpers {
    public class Util {
        public static UInt32 ReverseEndian(UInt32 input) {
            UInt32 output = 0;

            var bytes = Bytes((int)input);
            output = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | (bytes[3]));

            return output;
        }

        public static byte[] Bytes(int input) {
            var result = new byte[4];

            result[0] = (byte)(input & 0xFF);
            result[1] = (byte)((input >> 8) & 0xFF);
            result[2] = (byte)((input >> 16) & 0xFF);
            result[3] = (byte)(input >> 24);

            return result;
        }

        public static void WriteInt(ArraySegment<byte> data, Int16 val) {
            var bytes = Bytes(val);
            var o = data.Offset;

            data.Array[o] = bytes[0];
            data.Array[o + 1] = bytes[1];
        }

        public static void WriteInt(ArraySegment<byte> data, Int32 val) {
            var bytes = Bytes(val);
            var o = data.Offset;

            data.Array[o] = bytes[0];
            data.Array[o + 1] = bytes[1];
            data.Array[o + 2] = bytes[2];
            data.Array[o + 3] = bytes[3];
        }

    }
}
