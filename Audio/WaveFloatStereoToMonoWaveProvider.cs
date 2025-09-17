using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace MegriaCore.YMM4.WaveOutput
{
    /// <summary>
    /// IEEE float 形式の <see cref="IWaveProvider"/> のデータをモノラル化する <see cref="IWaveProvider"/> です。
    /// </summary>
    /// <remarks>
    /// <see cref="NAudio.Wave.SampleProviders.WaveToSampleProvider"/>
    /// 、 <see cref="NAudio.Wave.SampleProviders.StereoToMonoSampleProvider"/> 、
    /// <see cref="NAudio.Wave.SampleProviders.SampleToWaveProvider"/> を介するのと同等です。
    /// </remarks>
    public class WaveFloatStereoToMonoWaveProvider : ArrayPoolWaveProvider, IWaveProvider, ISampleProvider
    {
        public float LeftVolume { get; set; }

        public float RightVolume { get; set; }
        public WaveFormat WaveFormat { get; }

        public WaveFloatStereoToMonoWaveProvider(IWaveProvider sourceProvider) : this(sourceProvider, System.Buffers.ArrayPool<byte>.Shared)
        {

        }

        public WaveFloatStereoToMonoWaveProvider(IWaveProvider sourceProvider, System.Buffers.ArrayPool<byte> arrayPool) : base(sourceProvider, arrayPool)
        {
            LeftVolume = 0.5f;
            RightVolume = 0.5f;

            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Must be already floating point");
            }
            if (sourceProvider.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source must be stereo");
            }
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceProvider.WaveFormat.SampleRate, 1);
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            return ReadIntarnal(buffer.AsSpan(offset, count));
        }

        public int Read(float[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            return ReadIntarnal(buffer.AsSpan(offset, count));
        }

        private int ReadIntarnal(Span<byte> destination)
        {
            // 書き込み先 byte 配列を float に再解釈
            Span<float> dst = Helper.ToFloatSpan(destination);

            return ReadIntarnal(dst) * sizeof(float);
        }
        private int ReadIntarnal(Span<float> destination)
        {
            if (destination.IsEmpty)
                return 0;

            int dstIndex = 0;

            // sourceProvider から読み取る byte 数
            // int readByteSize = destination.Length * 2 * sizeof(float);

            Span<float> samples = ReadData(destination.Length << 3);

            float lv = LeftVolume;
            float rv = RightVolume;

            int i = 0;

            if (System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated)
            {
                var volumeVector = System.Runtime.Intrinsics.Vector256.Create(lv, rv, lv, rv, lv, rv, lv, rv);
                var vectors = Helper.ToVector256(samples);
                for (; i < vectors.Length; i++)
                {
                    ref var vector = ref vectors[i];
                    vector *= volumeVector;

                    destination[dstIndex++] = vector[0] + vector[1];
                    destination[dstIndex++] = vector[2] + vector[3];
                    destination[dstIndex++] = vector[4] + vector[5];
                    destination[dstIndex++] = vector[6] + vector[7];
                }
                i *= 8;
            }
            else if (System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated)
            {
                var volumeVector = System.Runtime.Intrinsics.Vector128.Create(lv, rv, lv, rv);
                var vectors = Helper.ToVector128(samples);
                for (; i < vectors.Length; i++)
                {
                    ref var vector = ref vectors[i];
                    vector *= volumeVector;

                    destination[dstIndex++] = vector[0] + vector[1];
                    destination[dstIndex++] = vector[2] + vector[3];
                }
                i *= 4;
            }

            for (; i < samples.Length; i += 2)
            {
                float leftSample = samples[i];
                float rightSample = samples[i + 1];
                float monoSample = leftSample * lv + rightSample * rv;
                destination[dstIndex++] = monoSample;
            }

            return dstIndex;
        }

        /// <summary>
        /// <see cref="sourceProvider"/> から <paramref name="readByteSize"/> <see cref="byte"/> のデータをバッファーに読み取り、
        /// データを格納するバッファーのスパンを取得します。
        /// </summary>
        /// <param name="readByteSize"><see cref="sourceProvider"/> から読み取る <see cref="byte"/> 数。</param>
        /// <returns>読み取ったデータを格納するバッファーのスパン。</returns>
        private Span<float> ReadData(int readByteSize)
        {
            byte[] sourceBuffer = EnsureSourceBuffer(readByteSize); // sourceBuffer の確保

            readByteSize = sourceProvider.Read(sourceBuffer, 0, readByteSize); // 読み取り byte 数

            // 読み取った byte データを float に再解釈
            return Helper.CreateSpan<byte, float>(sourceBuffer, readByteSize >> 2);
        }
    }
}
