using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CCClasses.Helpers;

namespace CCClasses.FileFormats.Binary {
    public class WAV {
        class ADPCMChunkHeader {
            internal const int HeaderSize = 4;
            internal Int16 Sample;
            internal byte Index;
            internal byte Reserved;

            internal void Read(ArraySegment<byte> data) {
                var offs = data.Offset;
                Sample = BitConverter.ToInt16(data.Array, offs);
                Index = data.Array[offs + 2];
                Reserved = data.Array[offs + 3];
            }

            internal AUD_Decoder GetDecoder() {
                return new AUD_Decoder(Index, Sample);
            }
        };


        public static byte[] ADPCM2PCM(byte[] Input, int ChannelCount, int ChunkSize) {
            var InputSize = Input.Length;
            int ChunkCount = (InputSize + ChunkSize - 1) / ChunkSize;
            int SampleCount = (InputSize - ADPCMChunkHeader.HeaderSize * ChannelCount * ChunkCount << 1) + ChunkCount * ChannelCount;

            var Output = new byte[SampleCount * 2];

            int wOffset = 0;
            int rOffset = 0;

            int RemainingSamples = SampleCount;
            while (RemainingSamples > 0) {
                if (ChannelCount == 1) {
                    var chunkHeader = new ADPCMChunkHeader();
                    var seg = new ArraySegment<byte>(Input, rOffset, ADPCMChunkHeader.HeaderSize);
                    chunkHeader.Read(seg);
                    rOffset += ADPCMChunkHeader.HeaderSize;

                    var oseg = new ArraySegment<byte>(Output, wOffset * 2, 2);
                    Util.WriteInt(oseg, chunkHeader.Sample);

                    ++wOffset;
                    --RemainingSamples;
                    var szChunk = Math.Min(RemainingSamples, (ChunkSize - ADPCMChunkHeader.HeaderSize << 1));

                    // aud_decode
                    var AUD = chunkHeader.GetDecoder();
                    var segIn = new ArraySegment<byte>(Input, rOffset, 0);
                    var segOut = new ArraySegment<byte>(Output, wOffset * 2, 0);
                    AUD.Decode(segIn, segOut, szChunk);

                    rOffset += szChunk >> 1;
                    wOffset += szChunk;
                    RemainingSamples -= szChunk;
                } else if (ChannelCount == 2) {
                    var leftHeader = new ADPCMChunkHeader();
                    var seg = new ArraySegment<byte>(Input, rOffset, ADPCMChunkHeader.HeaderSize);
                    leftHeader.Read(seg);
                    rOffset += ADPCMChunkHeader.HeaderSize;
                    var oseg = new ArraySegment<byte>(Output, wOffset * 2, 2);
                    Util.WriteInt(oseg, leftHeader.Sample);
                    ++wOffset;
                    --RemainingSamples;

                    var rightHeader = new ADPCMChunkHeader();
                    seg = new ArraySegment<byte>(Input, rOffset, ADPCMChunkHeader.HeaderSize);
                    rightHeader.Read(seg);
                    rOffset += ADPCMChunkHeader.HeaderSize;
                    oseg = new ArraySegment<byte>(Output, wOffset * 2, 2);
                    Util.WriteInt(oseg, rightHeader.Sample);
                    ++wOffset;
                    --RemainingSamples;

                    var szChunk = Math.Min(RemainingSamples, ChunkSize - (ADPCMChunkHeader.HeaderSize << 1) << 1);

                    var lAUD = leftHeader.GetDecoder();
                    var rAUD = rightHeader.GetDecoder();

                    while (szChunk >= 16) {
                        var lTemp = new byte[16];
                        var rTemp = new byte[16];

                        var segIn = new ArraySegment<byte>(Input, rOffset, 0);
                        var segOut = new ArraySegment<byte>(lTemp, 0, 0);
                        lAUD.Decode(segIn, segOut, 8);

                        rOffset += 4;

                        segIn = new ArraySegment<byte>(Input, rOffset, 0);
                        segOut = new ArraySegment<byte>(rTemp, 0, 0);
                        rAUD.Decode(segIn, segOut, 8);

                        rOffset += 4;

                        for (var i = 0; i < 8; ++i) {
                            oseg = new ArraySegment<byte>(Output, wOffset * 2, 2);
                            Util.WriteInt(oseg, BitConverter.ToInt16(lTemp, i * 2));
                            ++wOffset;

                            oseg = new ArraySegment<byte>(Output, wOffset * 2, 2);
                            Util.WriteInt(oseg, BitConverter.ToInt16(rTemp, i * 2));
                            ++wOffset;
                        }

                        szChunk -= 16;
                        RemainingSamples -= 16;
                    }
                    if (RemainingSamples < 16) {
                        RemainingSamples = 0;
                    }

                } else {
                    throw new InvalidDataException();
                }
            }

            return Output;
        }

    }

    class AUD_Decoder {
        int _Index;
        int _Sample;

        public AUD_Decoder(int Index = 0, int Sample = 0) {
            _Index = Index;
            _Sample = Sample;
        }

        static readonly int[] aud_ima_index_adjust_table = new int[]{-1, -1, -1, -1, 2, 4, 6, 8};

        static readonly int[] aud_ima_step_table = new int[] {
	        7,     8,     9,     10,    11,    12,     13,    14,    16,
            17,    19,    21,    23,    25,    28,     31,    34,    37,
            41,    45,    50,    55,    60,    66,     73,    80,    88,
            97,    107,   118,   130,   143,   157,    173,   190,   209,
            230,   253,   279,   307,   337,   371,    408,   449,   494,
            544,   598,   658,   724,   796,   876,    963,   1060,  1166,
            1282,  1411,  1552,  1707,  1878,  2066,   2272,  2499,  2749,
            3024,  3327,  3660,  4026,  4428,  4871,   5358,  5894,  6484,
            7132,  7845,  8630,  9493,  10442, 11487,  12635, 13899, 15289,
            16818, 18500, 20350, 22385, 24623, 27086,  29794, 32767
        };

        static readonly int[] aud_ws_step_table2 = new int[] { -2, -1, 0, 1 };

        static readonly int[] aud_ws_step_table4 = new int[] {
            -9, -8, -6, -5, -4, -3, -2, -1,
             0,  1,  2,  3,  4,  5,  6,  8
        };

        static int clip8(int v) {
            if (v < 0)
                return 0;
            return v > 0xff ? 0xff : v;
        }
        
        public void Decode(ArraySegment<byte> audio_in, ArraySegment<byte> audio_out, int szChunk) {
	        int code;
	        int delta;
	        int step;

            int rOffset = audio_in.Offset;
            int wOffset = audio_out.Offset;

	        for (int sample_index = 0; sample_index < szChunk; sample_index++) {
		        code = audio_in.Array[rOffset + (sample_index >> 1)];
		        code = ((sample_index & 1) != 0) ? code >> 4 : code & 0xf;
		        step = aud_ima_step_table[_Index];
		        delta = step >> 3;
		        if ((code & 1) != 0) {
			        delta += step >> 2;
                }
		        if ((code & 2) != 0) {
			        delta += step >> 1;
                }
		        if ((code & 4) != 0) {
			        delta += step;
                }
		        if ((code & 8) != 0) {
			        _Sample -= delta;
			        if (_Sample < -32768) {
				        _Sample = -32768;
                    }
		        } else {
			        _Sample += delta;
			        if (_Sample > 32767) {
				        _Sample = 32767;
                    }
		        }

                var oseg = new ArraySegment<byte>(audio_out.Array, wOffset + sample_index * 2, 2);
                Util.WriteInt(oseg, (short)_Sample);
                
                _Index += aud_ima_index_adjust_table[code & 7];
		        if (_Index < 0) {
			        _Index = 0;
                }
		        else if (_Index > 88) {
			        _Index = 88;  
                }
	        }
        }
    };
}
