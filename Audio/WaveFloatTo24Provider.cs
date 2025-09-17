using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MegriaCore.YMM4.WaveOutput
{
    /// <summary>
    /// IEEE float 形式の <see cref="IWaveProvider"/> のデータを、符号付き 24 bit 形式に変換する <see cref="IWaveProvider"/> です。
    /// </summary>
    /// <remarks>
    /// <see cref="NAudio.Wave.SampleProviders.WaveToSampleProvider"/> と
    /// <see cref="NAudio.Wave.SampleProviders.SampleToWaveProvider24"/> の機能を合併した、<br></br>
    /// <see cref="WaveFloatTo16Provider"/> の 24 bit 版です。
    /// </remarks>
    public class WaveFloatTo24Provider : ArrayPoolWaveProvider, IWaveProvider, IDisposable
    {
        private const int ByteSize = 3;

        protected volatile float volume;

        public float Volume
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return volume;
            }
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                volume = value;
            }
        }

        protected WaveFormat waveFormat;

        public WaveFormat WaveFormat
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return waveFormat;
            }
        }


        public WaveFloatTo24Provider(IWaveProvider sourceProvider) : this(sourceProvider, System.Buffers.ArrayPool<byte>.Shared)
        {

        }
        public WaveFloatTo24Provider(IWaveProvider sourceProvider, System.Buffers.ArrayPool<byte> arrayPool) : base(sourceProvider, arrayPool)
        {
            var sourceFormat = sourceProvider.WaveFormat;

            // 入力フォーマットが IEEE Float ではない場合は例外
            if (sourceFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Input wave provider must be IEEE float", nameof(sourceProvider));
            }

            // 入力フォーマットが 32 bit ではない場合は例外
            if (sourceFormat.BitsPerSample != 32)
            {
                throw new ArgumentException("Input wave provider must be 32 bit", nameof(sourceProvider));
            }

            waveFormat = new WaveFormat(sourceFormat.SampleRate, 24, sourceFormat.Channels);
            this.sourceProvider = sourceProvider;
            volume = 1f;
            this.arrayPool = arrayPool;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            // sourceProvider から読み取る byte 数
            int readByteSize = count / ByteSize * sizeof(float);

            byte[]? sourceBuffer = EnsureSourceBuffer(readByteSize); // sourceBuffer の確保

            readByteSize = sourceProvider.Read(sourceBuffer, 0, readByteSize);

            // 読み取った byte データを float に再解釈
            Span<float> samples = MemoryMarshal.CreateSpan(ref Helper.GetReference<byte, float>(sourceBuffer), readByteSize >> 2);

            Span<byte> dst = buffer.AsSpan(offset); // 書き込み先の byte スパン
            int dstIndex = 0; // 書き込み先の現在の位置

            int i = 0;

            // Avx をサポートかつ samples の長さが 16 以上の場合は SIMD 処理
            if (System.Runtime.Intrinsics.X86.Avx.IsSupported && samples.Length >= 16)
            {
                var vectors = Helper.ToVector128(samples); // Span<float> を Span<Vector128<float>> に再解釈

                var ngOne = System.Runtime.Intrinsics.Vector128.Create(-1f); // Clamp 用 -1.0f 定数
                var one = System.Runtime.Intrinsics.Vector128<float>.One; // Clamp 用 1.0f 定数

                for (; i < vectors.Length; i++)
                {
                    ref var vectorSingle = ref vectors[i];

                    // vector 要素の値を -1.0f ~ 1.0f の範囲にする
                    vectorSingle = System.Runtime.Intrinsics.Vector128.Clamp(vectorSingle, ngOne, one);

                    System.Runtime.Intrinsics.Vector128<int> vectorInt;
                    {
                        // Vector256<double> のスコープを明確にする

                        // Vector128<float> を Vector256<double> に変換
                        var vectorDouble = System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Double(vectorSingle);

                        vectorDouble *= 8388607.0; // 定数 8388607.0 を乗算

                        // Vector256<double> を Vector128<int> に変換
                        vectorInt = System.Runtime.Intrinsics.X86.Avx.ConvertToVector128Int32(vectorDouble);
                    }

                    Write24Bit(dst[dstIndex..], vectorInt);

                    dstIndex += ByteSize * 4;
                }

                i *= 4; // i *= System.Runtime.Intrinsics.Vector128<float>.Count;
            }

            for (; i < samples.Length; i++)
            {
                float sample = samples[i];
                sample = Math.Clamp(sample * volume, -1f, 1f);

                int sample24Bit = (int)((double)sample * 8388607.0);

                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.CopyBlockUnaligned(ref dst[dstIndex], ref Unsafe.As<int, byte>(ref sample24Bit), ByteSize);
                    dstIndex += ByteSize;
                }
                else
                {
                    dst[dstIndex++] = (byte)sample24Bit;
                    dst[dstIndex++] = (byte)(sample24Bit >> 8);
                    dst[dstIndex++] = (byte)(sample24Bit >> 16);
                }
            }

            return dstIndex;
        }

        /// <summary>
        /// <paramref name="source"/> の要素の値を 24 ビットの範囲に切り捨てて、リトルエンディアン形式で
        /// <paramref name="destination"/> に書き込みます。
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        private static unsafe void Write24Bit(Span<byte> destination, System.Runtime.Intrinsics.Vector128<int> source)
        {
            byte* srcPtr = (byte*)&source;
            fixed (byte* dstPtr = destination)
            {
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.CopyBlockUnaligned(dstPtr, srcPtr, ByteSize);
                    Unsafe.CopyBlockUnaligned(dstPtr + ByteSize, srcPtr + 4, ByteSize);
                    Unsafe.CopyBlockUnaligned(dstPtr + ByteSize * 2, srcPtr + 8, ByteSize);
                    Unsafe.CopyBlockUnaligned(dstPtr + ByteSize * 3, srcPtr + 12, ByteSize);
                }
                else
                {
                    dstPtr[0] = srcPtr[3];
                    dstPtr[1] = srcPtr[2];
                    dstPtr[2] = srcPtr[1];
                    dstPtr[3] = srcPtr[7];
                    dstPtr[4] = srcPtr[6];
                    dstPtr[5] = srcPtr[5];
                    dstPtr[6] = srcPtr[11];
                    dstPtr[7] = srcPtr[10];
                    dstPtr[8] = srcPtr[9];
                    dstPtr[9] = srcPtr[15];
                    dstPtr[10] = srcPtr[14];
                    dstPtr[11] = srcPtr[13];
                }
            }
        }
    }
}
