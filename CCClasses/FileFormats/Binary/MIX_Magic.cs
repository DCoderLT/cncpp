using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCClasses.Libraries;
using System.Diagnostics;

namespace CCClasses.FileFormats {

    /**
     * 
     * Here be dragons, courtesy of XCC.
     * If you touch this, you will be responsible for maintaining it.
     * 
     */
    public class MIX_Magic {
        static UInt16 GetWord(UInt32[] data, uint key) {
            var ixDw = key >> 1;
            var ixW = (byte)(key & 1);

            var dw = data[ixDw];
            var res = (dw >> (16 * ixW));
            return (UInt16)(res & 0xFFFF);
        }
        static void SetWord(ref UInt32[] data, uint key, UInt16 val) {
            var ixDw = key >> 1;
            var ixW = (byte)(key & 1) * 16;

            var dw = data[ixDw];

            var unmask = (UInt32)(0xFFFF << ixW);
            var mask = 0xFFFFFFFF ^ unmask;

            dw &= mask;
            UInt32 shval = (UInt32)(val << ixW);
            dw |= shval;
            data[ixDw] = dw;
        }

        static byte GetByte(UInt32[] data, uint key) {
            var ixDw = key >> 2;
            var ixB = (byte)(key & 3);

            var dw = data[ixDw];
            var res = (dw >> (8 * ixB));
            return (byte)(res & 0xFF);
        }

        static void SetByte(ref UInt32[] data, uint key, byte val) {
            var ixDw = key >> 2;
            var ixB = (byte)(key & 3) * 8;
            var unmask = (UInt32)(0xFF << ixB);
            var mask = 0xFFFFFFFF ^ unmask;

            var dw = data[ixDw];
            dw &= mask;
            UInt32 shval = (UInt32)(val << ixB);
            dw |= shval;
            data[ixDw] = dw;
        }

        static readonly String pubkey_str = "AihRvNoIbTn85FZRYNZRcT+i6KpU+maCsEqr3Q5q+LDB5tH7Tz2qQ38V";

        static readonly sbyte[] char2num = new sbyte[] {
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,
	        52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,
	        -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
	        15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,
	        -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
	        41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };

        class Tpubkey {
            public UInt32[] key1 = new UInt32[64];
            public UInt32[] key2 = new UInt32[64];
            public UInt32 len;
        };

        static Tpubkey pubkey = new Tpubkey();
        static UInt32[] glob1 = new UInt32[64];
        static UInt32 glob1_bitlen, glob1_len_x2;
        static UInt32[] glob2 = new UInt32[130];
        static UInt32[] glob1_hi = new UInt32[4], glob1_hi_inv = new UInt32[4];
        static UInt32 glob1_hi_bitlen;
        static UInt32 glob1_hi_inv_lo, glob1_hi_inv_hi;


        static void init_bignum(ref UInt32[] n, UInt32 val, UInt32 len) {
            ////Debug.WriteLine("Initializing bignum");
            for (UInt32 i = 1; i < len; ++i) {
                n[i] = 0;
            }
            n[0] = val;
        }

        static void move_key_to_big(ref UInt32[] n, ArraySegment<byte> keya, UInt32 klen, UInt32 blen) {
            ////Debug.WriteLine("Moving key to bignum");
            byte sign = 0;

            var key = keya.Array;
            var ko = keya.Offset;

            if ((key[ko] & 0x80) != 0) {
                sign = 0xFF;
            }
            uint i;
            for (i = blen * 4; i > klen; i--) {
                SetByte(ref n, i - 1, sign);
            }
            for (; i > 0; i--) {
                SetByte(ref n, i - 1, key[ko + klen - i]);
            }

        }

        static void key_to_bignum(ref UInt32[] n, byte[] key, UInt32 len) {
            ////Debug.WriteLine("Transferring key to bignum");
            if (key[0] != 2) {
                return;
            }

            UInt32 keylen;
            int i;

            int koffs = 1;
            if ((key[koffs] & 0x80) != 0) {
                keylen = 0;
                for (i = 0; i < (key[koffs] & 0x7f); i++) {
                    keylen = (keylen << 8) | key[koffs + i + 1];
                }
                koffs += 1 + (key[koffs] & 0x7f);
            } else {
                keylen = key[koffs];
                ++koffs;
            }
            if (keylen <= len * 4) {
                var seg = new ArraySegment<byte>(key, koffs, 0);
                move_key_to_big(ref n, seg, keylen, len);
            }
        }

        static UInt32 len_bignum(ref UInt32[] n, UInt32 len) {
            ////Debug.WriteLine("Getting length of bignum");
            int i = (int)(len - 1);

            while ((i >= 0) && (n[i] == 0)) {
                --i;
            }
            return (UInt32)(i + 1);
        }

        static UInt32 bitlen_bignum(ref UInt32[] n, UInt32 len) {
            ////Debug.WriteLine("Getting bitlen of bignum");
            UInt32 ddlen = len_bignum(ref n, len);

            if (ddlen == 0) {
                return 0;
            }

            UInt32 bitlen = ddlen * 32, mask = 0x80000000;

            while ((mask & n[ddlen - 1]) == 0) {
                mask >>= 1;
                bitlen--;
            }
            return bitlen;
        }

        static long cmp_bignum(ref UInt32[] n1, ref UInt32[] n2, UInt32 len) {
            ////Debug.WriteLine("Comparing bignums");
            while (len > 0) {
                var v1 = n1[len - 1];
                var v2 = n2[len - 1];
                if (v1 == v2) {
                    --len;
                } else {
                    return v1 < v2 ? -1 : 1;
                }
            }
            return 0;
        }

        static void mov_bignum(ref UInt32[] dest, ref UInt32[] src, UInt32 len) {
            ////Debug.WriteLine("Copying bignum");
            for (uint i = 0; i < len; ++i) {
                dest[i] = src[i];
            }
        }

        static void shr_bignum(ref UInt32[] n, UInt32 bits, long len) {
            ////Debug.WriteLine("SHR'ing bignum");
            UInt32 i, i2, ilen = (uint)len;

            i2 = bits / 32;
            if (i2 > 0) {
                for (i = 0; i < ilen - i2; i++) {
                    n[i] = n[i + i2];
                }
                for (; i < ilen; i++) {
                    n[i] = 0;
                }
                bits = bits % 32;
            }
            if (bits == 0) {
                return;
            }

            var bitsToShift = (int)bits;
            for (i = 0; i < ilen - 1; i++) {
                n[i] = (n[i] >> bitsToShift) | (n[i + 1] << (32 - bitsToShift));
            }
            n[i] = n[i] >> bitsToShift;
        }

        static void shl_bignum(ref UInt32[] n, UInt32 bits, UInt32 len) {
            ////Debug.WriteLine("SHL'ing bignum");
            UInt32 i, i2 = bits / 32;
            if (i2 > 0) {
                for (i = len - 1; i > i2; i--) {
                    n[i] = n[i - i2];
                }
                for (; i > 0; i--) {
                    n[i] = 0;
                }
                bits = bits % 32;
            }
            if (bits == 0) {
                return;
            }

            var bitsToShift = (int)bits;
            for (i = len - 1; i > 0; i--) {
                n[i] = (n[i] << bitsToShift) | (n[i - 1] >> (32 - bitsToShift));
            }
            n[0] <<= bitsToShift;
        }

        static UInt32 sub_bignum(ArraySegment<UInt32> desta, ArraySegment<UInt32> src1a, ref UInt32[] src2, UInt32 carry, UInt32 len) {
            ////Debug.WriteLine("Subtracting bignum");
            UInt32 i1, i2;

            len += len;
            uint ix = 0;
            var d = desta.Array;

            while (--len != 0xFFFFFFFF) {
                i1 = GetWord(src1a.Array, (uint)(ix + src1a.Offset));
                i2 = GetWord(src2, ix);
                var delta = (UInt32)(i1 - i2 - carry);
                SetWord(ref d, (uint)(desta.Offset + ix), (UInt16)delta);
                ////Debug.WriteLine("i1 = {0:X}, i2 = {1:X}, delta = {2:X}, carry = {3:X}", i1, i2, delta, carry);
                carry = ((delta & 0x10000) == 0) ? 0 : 1u;
                ++ix;
            }

            return carry;
        }

        static void inv_bignum(ref UInt32[] n1, ref UInt32[] n2, UInt32 len) {
            ////Debug.WriteLine("Inverting bignum");
            UInt32[] n_tmp = new UInt32[64];
            UInt32 n2_bytelen, bit;
            long n2_bitlen;

            init_bignum(ref n_tmp, 0, len);
            init_bignum(ref n1, 0, len);
            n2_bitlen = bitlen_bignum(ref n2, len);
            bit = ((UInt32)1) << ((int)(n2_bitlen % 32));

            uint n1offs = (uint)((n2_bitlen + 32) / 32) - 1;

            n2_bytelen = (uint)((n2_bitlen - 1) / 32) * 4;

            n_tmp[n2_bytelen / 4] |= ((UInt32)1) << (byte)((n2_bitlen - 1) & 0x1f);

            while (n2_bitlen > 0) {
                n2_bitlen--;
                shl_bignum(ref n_tmp, 1, len);
                if (cmp_bignum(ref n_tmp, ref n2, len) != -1) {
                    var seg = new ArraySegment<UInt32>(n_tmp);
                    sub_bignum(seg, seg, ref n2, 0, len);
                    n1[n1offs] |= bit;
                }
                bit >>= 1;
                if (bit == 0) {
                    n1offs--;
                    bit = 0x80000000;
                }
            }
            init_bignum(ref n_tmp, 0, len);
        }

        static void inc_bignum(ref UInt32[] n, UInt32 len) {
            ////Debug.WriteLine("Incrementing bignum");
            uint noffs = 0;
            while ((++n[noffs] == 0) && (--len > 0)) {
                noffs++;
            }
        }



        static void init_two_dw(ref UInt32[] n, UInt32 len) {
            ////Debug.WriteLine("Initing glob1 with bignum");
            mov_bignum(ref glob1, ref n, len);
            glob1_bitlen = bitlen_bignum(ref glob1, len);
            glob1_len_x2 = (glob1_bitlen + 15) / 16;

            UInt32[] tmp = new UInt32[2];
            var g1offs = len_bignum(ref glob1, len) - 2;
            tmp[0] = glob1[g1offs];
            tmp[1] = glob1[g1offs + 1];

            mov_bignum(ref glob1_hi, ref tmp, 2);
            glob1_hi_bitlen = bitlen_bignum(ref glob1_hi, 2) - 32;
            shr_bignum(ref glob1_hi, glob1_hi_bitlen, 2);
            inv_bignum(ref glob1_hi_inv, ref glob1_hi, 2);
            shr_bignum(ref glob1_hi_inv, 1, 2);
            glob1_hi_bitlen = (glob1_hi_bitlen + 15) % 16 + 1;
            inc_bignum(ref glob1_hi_inv, 2);
            if (bitlen_bignum(ref glob1_hi_inv, 2) > 32) {
                shr_bignum(ref glob1_hi_inv, 1, 2);
                glob1_hi_bitlen--;
            }
            glob1_hi_inv_lo = GetWord(glob1_hi_inv, 0);
            glob1_hi_inv_hi = GetWord(glob1_hi_inv, 1);
        }

        static void mul_bignum_word(ArraySegment<UInt32> n1a, ref UInt32[] n2, UInt32 mul, UInt32 len) {
            ////Debug.WriteLine("Multiplying bignum by word");
            UInt32 i, tmp = 0;

            UInt32[] n1 = n1a.Array;
            uint n1of = (uint)n1a.Offset;

            for (i = 0; i < len; i++) {
                tmp = mul * GetWord(n2, i) + GetWord(n1, n1of + i) + tmp;
                SetWord(ref n1, n1of + i, (UInt16)tmp);
                ////Debug.WriteLine("tmp = {0:X}, idx = {1:X}", tmp, i);
                tmp >>= 16;
            }
            SetWord(ref n1, n1of + i, (UInt16)(GetWord(n1, n1of + i) + tmp));
        }

        static void mul_bignum(ref UInt32[] dest, ref UInt32[] src1, ref UInt32[] src2, UInt32 len) {
            ////Debug.WriteLine("Multiplying bignums");
            init_bignum(ref dest, 0, len * 2);
            for (UInt32 i = 0; i < len * 2; i++) {
                var seg = new ArraySegment<UInt32>(dest, (int)i, 0);
                mul_bignum_word(seg, ref src1, GetWord(src2, i), len * 2);
            }
        }

        static void not_bignum(ref UInt32[] n, UInt32 len) {
            ////Debug.WriteLine("NOT'ing bignum");
            for (UInt32 i = 0; i < len; i++) {
                n[i] = ~n[i];
            }
        }

        static void neg_bignum(ref UInt32[] n, UInt32 len) {
            ////Debug.WriteLine("Negating bignum");
            not_bignum(ref n, len);
            inc_bignum(ref n, len);
        }

        static UInt32 get_mulword(ref UInt16[] n) {
            UInt32 i;
            i = (UInt32)((
                  ((
                    ((
                         ((((n[1] ^ 0xffff) & 0xffff) * glob1_hi_inv_lo + 0x10000) >> 1)
                        +
                         (((n[0] ^ 0xffff) * glob1_hi_inv_hi + glob1_hi_inv_hi) >> 1)
                        + 1
                      ) >> 16)
                   +
                    ((((n[1] ^ 0xffff) & 0xffff) * glob1_hi_inv_hi) >> 1)
                   +
                    (((n[2] ^ 0xffff) * glob1_hi_inv_lo) >> 1)
                   + 1
                  ) >> 14)
                 +
                  glob1_hi_inv_hi * (n[2] ^ 0xffff) * 2

                ) >> Convert.ToByte(glob1_hi_bitlen));


            if (i > 0xffff)
                i = 0xffff;
            return i & 0xffff;
        }

        static void dec_bignum(ref UInt32[] n, UInt32 len) {
            ////Debug.WriteLine("Decrementing bignum");
            uint idx = 0;
            while ((--n[idx] == 0xffffffff) && (--len > 0)) {
                idx++;
            }
        }

        static void calc_a_bignum(ref UInt32[] n1, ref UInt32[] n2, ref UInt32[] n3, UInt32 len) {
            ////Debug.WriteLine("Computing bignum");
            UInt32 g2_len_x2, len_diff;
            UInt16 tmp;

            uint xesi, xedi;

            mul_bignum(ref glob2, ref n2, ref n3, len);
            glob2[len * 2] = 0;
            g2_len_x2 = len_bignum(ref glob2, len * 2 + 1) * 2;
            if (g2_len_x2 >= glob1_len_x2) {
                inc_bignum(ref glob2, len * 2 + 1);
                neg_bignum(ref glob2, len * 2 + 1);
                len_diff = g2_len_x2 + 1 - glob1_len_x2;

                xesi = 1 + g2_len_x2 - glob1_len_x2;
                xedi = g2_len_x2 + 1;
                for (; len_diff != 0; len_diff--) {
                    xedi--;

                    var words = new UInt16[3];
                    var ediw = xedi * 1;
                    words[0] = GetWord(glob2, ediw - 2);
                    words[1] = GetWord(glob2, ediw - 1);
                    words[2] = GetWord(glob2, ediw);

                    ////Debug.WriteLine("XEDI = {0:X}, MULWORD({1:X}, {2:X}, {3:X})", xedi, words[0], words[1], words[2]);

                    tmp = (UInt16)get_mulword(ref words);

                    ////Debug.WriteLine("Got mulword {0:X}", tmp);

                    xesi--;
                    if (tmp > 0) {
                        ////Debug.WriteLine("tmp > 0");
                        var seg = new ArraySegment<UInt32>(glob2, (int)xesi, 0);
                        mul_bignum_word(seg, ref glob1, tmp, 2 * len);
                        if ((GetWord(glob2, xedi) & 0x8000) == 0) {
                            ////Debug.WriteLine("xedi & 0x8000 == 0");
                            ////Debug.WriteLine("xesi = {0:X}", xesi);
                            
                            if (sub_bignum(seg, seg, ref glob1, 0, len) != 0) {
                                ////Debug.WriteLine("sub != 0");
                                SetWord(ref glob2, xedi, (UInt16)(GetWord(glob2, xedi) - 1));
                            }
                        }
                    }
                }
                neg_bignum(ref glob2, len);
                dec_bignum(ref glob2, len);
            }
            mov_bignum(ref n1, ref glob2, len);
        }



        // ----------------------------------------


        static void clear_tmp_vars(UInt32 len) {
            init_bignum(ref glob1, 0, len);
            init_bignum(ref glob2, 0, len);
            init_bignum(ref glob1_hi_inv, 0, 4);
            init_bignum(ref glob1_hi, 0, 4);
            glob1_bitlen = 0;
            glob1_hi_bitlen = 0;
            glob1_len_x2 = 0;
            glob1_hi_inv_lo = 0;
            glob1_hi_inv_hi = 0;
        }

        static void calc_a_key(ref UInt32[] n1, ref UInt32[] n2, ref UInt32[] n3, ref UInt32[] n4, UInt32 len) {
            ////Debug.WriteLine("Calculating key from bignums");
            UInt32[] n_tmp = new UInt32[64];
            UInt32 n3_len, n4_len, n3_bitlen, bit_mask;

            init_bignum(ref n1, 1, len);
            n4_len = len_bignum(ref n4, len);
            init_two_dw(ref n4, n4_len);
            n3_bitlen = bitlen_bignum(ref n3, n4_len);
            n3_len = (n3_bitlen + 31) / 32;
            bit_mask = (((UInt32)1) << (int)((n3_bitlen - 1) % 32)) >> 1;

            var n3offs = n3_len - 1;
            n3_bitlen--;
            mov_bignum(ref n1, ref n2, n4_len);
            while (n3_bitlen-- != 0) {
                if (bit_mask == 0) {
                    bit_mask = 0x80000000;
                    n3offs--;
                }
                calc_a_bignum(ref n_tmp, ref n1, ref n1, n4_len);
                if ((n3[n3offs] & bit_mask) != 0) {
                    calc_a_bignum(ref n1, ref n_tmp, ref n2, n4_len);
                } else {
                    mov_bignum(ref n1, ref n_tmp, n4_len);
                }
                bit_mask >>= 1;
            }
            init_bignum(ref n_tmp, 0, n4_len);
            clear_tmp_vars(len);
        }


        static void init_pubkey() {
            UInt32 tmp;
            byte[] keytmp = new byte[256];

            init_bignum(ref pubkey.key2, 0x10001, 64);

            int i = 0;
            int i2 = 0;
            while (i < pubkey_str.Length) {
                tmp = (byte)char2num[pubkey_str[i++]];
                tmp <<= 6;
                tmp |= (byte)char2num[pubkey_str[i++]];
                tmp <<= 6;
                tmp |= (byte)char2num[pubkey_str[i++]];
                tmp <<= 6;
                tmp |= (byte)char2num[pubkey_str[i++]];
                keytmp[i2++] = (byte)((tmp >> 16) & 0xff);
                keytmp[i2++] = (byte)((tmp >> 8) & 0xff);
                keytmp[i2++] = (byte)(tmp & 0xff);
            }
            key_to_bignum(ref pubkey.key1, keytmp, 64);
            pubkey.len = bitlen_bignum(ref pubkey.key1, 64) - 1;
        }

        static void process_predata(byte[] pre, UInt32 pre_len, ref byte[] buf) {
            UInt32[] n2 = new UInt32[64], n3 = new UInt32[64];
            UInt32 a = (pubkey.len - 1) / 8;

            var preoff = 0u;
            var bufoff = 0u;

            while (a + 1 <= pre_len) {
                init_bignum(ref n2, 0, 64);

                for (var ia = 0u; ia < a + 1; ++ia) {
                    SetByte(ref n2, ia, pre[preoff + ia]);
                }
                calc_a_key(ref n3, ref n2, ref pubkey.key2, ref pubkey.key1, 64);

                for (var ib = 0u; ib < a; ++ib) {
                    buf[ib + bufoff] = GetByte(n3, ib);
                }

                pre_len -= a + 1;
                preoff += a + 1;
                bufoff += a;
            }
        }

        static UInt32 len_predata() {
            UInt32 a = (pubkey.len - 1) / 8;

            return (55 / a + 1) * (a + 1);
        }

        static bool public_key_initialized = false;
        public static void get_blowfish_key(byte[] s, ref byte[] d) {
            if (!public_key_initialized) {
                init_pubkey();
                public_key_initialized = true;
            }

            byte[] key = new byte[256];

            process_predata(s, len_predata(), ref key);
            for (var i = 0; i < 56; ++i) {
                d[i] = key[i];
            }
        }

        public static UInt32 getID(String name) {
            name = name.ToUpper();

            var nb = new List<byte>();
            for (var i = 0; i < name.Length; ++i) {
                nb.Add((byte)name[i]);
            }

            var l = name.Length;
            int a = l >> 2;
            if ((l & 3) != 0) {
                nb.Add((byte)(l - (a << 2)));
                int i = 3 - (l & 3);
                while (i-- != 0)
                    nb.Add(nb[a << 2]);
            }

            Crc32 crc = new Crc32();

            var hash = crc.ComputeHash(nb.ToArray());
            return BitConverter.ToUInt32(hash.Reverse().ToArray(), 0);
        }

        public static String getIDString(String name) {
            return String.Format("{0:8X}", getID(name));
        }
    }
}
