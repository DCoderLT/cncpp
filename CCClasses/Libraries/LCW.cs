using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CCClasses.Libraries {
    public class LCW {
        private static int Decompress(byte[] Input, ref byte[] Output) {
            var out4 = 0;
            var out46 = 0;
            var in3 = 0;
            var out5 = 0;

            int i, j;

            int v6;
            uint v8;
            int v9;
            int v10;
            int v11;
            int v12;
            int v16;
            int v45;
            int v14;
            int v15;
            uint v17;
            int v18;
            int v21;
            int v22;
            int v25;
            int v27;
            int v29;
            int v30;
            int v31;
            int v33;
            int v34;
            int v35;
            int v37;
            int v38;
            int v40;
            int v42;
            int v43;
            int v48;


            int in47;
            int out13;
            int out7;
            int out20;
            int out23;
            int out24;
            int out28;
            int out32;
            int out36;
            int out39;
            int out41;


            while (true) {
                while (true) {
                    while (true) {
                        v6 = Input[in3++];
                        if ((v6 & 0x80) != 0) {
                            break;
                        }
                        out7 = out5 - Input[in3++] - 256 * (v6 & 0xF);
                        for (v8 = ((uint)v6 >> 4) + 3; v8 != 0; --v8) {
                            Output[out5++] = Output[out7++];
                        }
                    }
                    if ((v6 & 0x40) == 0) {
                        break;
                    }
                    if (v6 == 0xFE) {
                        v25 = 0;
                        v12 = Input[in3 + 2];
                        v45 = Input[in3 + 2];
                        v16 = Input[in3] + (Input[in3 + 1] << 8);
                        in3 += 3;
                        in47 = in3;
                        v14 = 0x1010101 * v45;
                        out13 = out5 + 4 - (out5 & 3);
                        v15 = out5 - out13 + v16;
                        v48 = v15;
                        if (out5 < out13) {
                            in3 = v12;
                            v21 = out13 - out5;
                            in3 = (in3 << 8) + v12;
                            v18 = v21;
                            v25 = v21;
                            v17 = (uint)in3 << 16;
                            v17 &= 0xFFFF0000;
                            v17 |= ((uint)in3 & 0xFFFF);
                            v21 >>= 2;
                            for (var len = 0; len < v21; ++len) {
                                var o = out5 + len * 4;
                                Output[o] = (byte)(v17 & 0xFF);
                                Output[o + 1] = (byte)((v17 >> 8) & 0xFF);
                                Output[o + 2] = (byte)((v17 >> 16) & 0xFF);
                                Output[o + 3] = (byte)((v17 >> 24));
                            }
                            out20 = out5 + 4 * v21;
                            in3 = in47;
                            for (i = v18 & 3; i != 0; --i) {
                                Output[out20++] = (byte)(v17 & 0xFF);
                            }
                            v15 = v48;
                            out5 += v18;
                            v12 = v45;
                        }
                        out23 = out5;
                        for (out5 += (int)(v15 & ~3); out23 < out5; out23 += 8) {
                            var o = out23;
                            Output[o] = (byte)(v14 & 0xFF);
                            Output[o + 1] = (byte)((v14 >> 8) & 0xFF);
                            Output[o + 2] = (byte)((v14 >> 16) & 0xFF);
                            Output[o + 3] = (byte)((v14 >> 24));
                            Output[o + 4] = (byte)(v14 & 0xFF);
                            Output[o + 5] = (byte)((v14 >> 8) & 0xFF);
                            Output[o + 6] = (byte)((v14 >> 16) & 0xFF);
                            Output[o + 7] = (byte)((v14 >> 24));
                        }
                        out24 = out5 + (v15 & 3);
                        if (out5 < out24) {
                            v22 = 0x101 * v12;
                            v27 = out24 - out5;
                            v29 = 0x10001 * v22;
                            v30 = (out24 - out5) >> 2;
                            for (var len = 0; len < v30; ++len) {
                                var o = out5 + len * 4;
                                Output[o] = (byte)(v29 & 0xFF);
                                Output[o + 1] = (byte)((v29 >> 8) & 0xFF);
                                Output[o + 2] = (byte)((v29 >> 16) & 0xFF);
                                Output[o + 3] = (byte)((v29 >> 24));
                            }
                            out28 = out5 + (4 * v30);
                            for (j = v27 & 3; j != 0; --j) {
                                Output[out28++] = (byte)(v25 & 0xFF);
                            }
                            out5 += v27;
                        }
                        out4 = out46;
                    } else {
                        if (v6 == 0xFF) {
                            v33 = Input[in3 + 1];
                            v34 = Input[in3];
                            v35 = Input[in3 + 3];
                            in3 += 4;
                            out36 = out4 + (256 * v35);
                            v31 = v34 + (v33 << 8);
                            out32 = out36 + (Input[in3 - 2]);
                            if (v31 != 0) {
                                v37 = v31;
                                do {
                                    Output[out5++] = Output[out32++];
                                    --v37;
                                }
                                while (v37 != 0);
                            }
                        } else {
                            v40 = Input[in3];
                            out41 = out4 + (256 * Input[in3 + 1]);
                            in3 += 2;
                            out39 = out41 + v40;
                            v42 = (v6 & 0x3F) + 3;
                            v38 = (v6 & 0x3F) + 2;
                            if (v42 != 0) {
                                v43 = v38 + 1;
                                do {
                                    Output[out5++] = Output[out39++];
                                    --v43;
                                }
                                while (v43 != 0);
                            }
                        }
                    }
                }
                if (v6 == 0x80)
                    break;
                v10 = v6 & 0x3F;
                v9 = (v6 & 0x3F) - 1;
                if (v10 != 0) {
                    v11 = v9 + 1;
                    do {
                        Output[out5++] = Input[in3++];
                        --v11;
                    }
                    while (v11 != 0);
                }
            }
            return out5 - out4;
        }

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

                    var Output = new byte[OutputSize];

                    Decompress(Input, ref Output);
 
                    unpacked.AddRange(Output.Take(OutputSize));
                }
                offs += InputSize;
            }

            return unpacked.ToArray();
        }
    }
}
