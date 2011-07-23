using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace CCClasses.Libraries {
    class LZODLL {

    		[DllImport("lzo.dll")]
        public static extern int __lzo_init3();
		[DllImport("lzo.dll")]
        public static extern string lzo_version_string();
		[DllImport("lzo.dll")]
        public static extern string lzo_version_date();
		[DllImport("lzo.dll", CallingConvention=CallingConvention.Cdecl)]
		private static extern int lzo1x_1_compress(
			byte[] src,
			int src_len,
			byte[] dst,
			ref int dst_len,
			byte[] wrkmem
			);
		[DllImport("lzo.dll", CallingConvention=CallingConvention.Cdecl)]
		public static extern int lzo1x_decompress(
			byte[] src,
			int src_len,
			byte[] dst,
			ref int dst_len,
			byte[] wrkmem);
		
		private byte[] _workMemory = new byte[16384L * 4];

		static LZODLL() {
			int init = __lzo_init3();
			if(init != 0) {
				throw new Exception("Initialization of LZO-Compressor failed !");
			}
		}

		/// <summary>
		/// Version string of the compression library.
		/// </summary>
		public static string Version {
			get {
				return lzo_version_string();
			}
		}

		/// <summary>
		/// Version date of the compression library
		/// </summary>
		public static string VersionDate {
			get {
				return lzo_version_date();
			}
		}

    };


    public class LZO {
        public static byte[] Slurp(byte[] packed) {
            var unpacked = new List<byte>();

            var offs = 0;
            while (offs < packed.Length) {
                int InputSize = BitConverter.ToInt16(packed, offs);
                int OutputSize = BitConverter.ToInt16(packed, offs + 2);
                offs += 4;
                if (offs + InputSize < packed.Length) {
                    var Input = new byte[InputSize];
                    Buffer.BlockCopy(packed, offs, Input, 0, InputSize);

                    var Output = new byte[OutputSize * 2];

                    var wmem = new byte[0x80000];

                    int decompressedSize = 0;

                    LZODLL.lzo1x_decompress(Input, InputSize, Output, ref decompressedSize, wmem);

                    if (decompressedSize != OutputSize) {
                        throw new InvalidDataException();
                    }

                    unpacked.AddRange(Output.Take(OutputSize));
                }
                offs += InputSize;
            }

            return unpacked.ToArray();
        }
    }
}
