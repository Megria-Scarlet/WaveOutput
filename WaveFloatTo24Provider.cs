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

            byte[] sourceBuffer = EnsureSourceBuffer(readByteSize); // sourceBuffer の確保

            // readByteSize = sourceProvider.Read(sourceBuffer, 0, readByteSize);
            readByteSize = sourceProvider.Read(sourceBuffer.AsSpan(0, readByteSize));

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
        private static void Write24Bit(scoped Span<byte> destination, System.Runtime.Intrinsics.Vector128<int> source)
        {
            ref byte dst = ref MemoryMarshal.GetReference(destination);
            ref byte src = ref Unsafe.As<System.Runtime.Intrinsics.Vector128<int>, byte>(ref source);
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.CopyBlockUnaligned(ref dst, ref src, ByteSize);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AddByteOffset(ref dst, ByteSize), ref Unsafe.AddByteOffset(ref src, sizeof(int)), ByteSize);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AddByteOffset(ref dst, ByteSize * 2), ref Unsafe.AddByteOffset(ref src, sizeof(int) * 2), ByteSize);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AddByteOffset(ref dst, ByteSize * 3), ref Unsafe.AddByteOffset(ref src, sizeof(int) * 3), ByteSize);
            }
            else
            {
                WriteFromBigEndian(ref src, ref dst);
            }

            static void WriteFromBigEndian(scoped ref byte src, scoped ref byte dst)
            {
                dst = ref Unsafe.AddByteOffset(ref src, 3);
                Unsafe.AddByteOffset(ref dst, 1) = Unsafe.AddByteOffset(ref src, 2);
                Unsafe.AddByteOffset(ref dst, 2) = Unsafe.AddByteOffset(ref src, 1);

                Unsafe.AddByteOffset(ref dst, 3) = Unsafe.AddByteOffset(ref src, 7);
                Unsafe.AddByteOffset(ref dst, 4) = Unsafe.AddByteOffset(ref src, 6);
                Unsafe.AddByteOffset(ref dst, 5) = Unsafe.AddByteOffset(ref src, 5);

                Unsafe.AddByteOffset(ref dst, 6) = Unsafe.AddByteOffset(ref src, 11);
                Unsafe.AddByteOffset(ref dst, 7) = Unsafe.AddByteOffset(ref src, 10);
                Unsafe.AddByteOffset(ref dst, 8) = Unsafe.AddByteOffset(ref src, 9);

                Unsafe.AddByteOffset(ref dst, 9) = Unsafe.AddByteOffset(ref src, 15);
                Unsafe.AddByteOffset(ref dst, 10) = Unsafe.AddByteOffset(ref src, 14);
                Unsafe.AddByteOffset(ref dst, 11) = Unsafe.AddByteOffset(ref src, 13);
            }
        }
    }
}
