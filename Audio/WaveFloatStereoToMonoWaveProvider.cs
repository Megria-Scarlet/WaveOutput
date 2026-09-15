using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Vortice.DirectWrite;
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
        public int Read(scoped Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            return ReadIntarnal(buffer);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            return ReadIntarnal(buffer.AsSpan(offset, count));
        }

        public int Read(scoped Span<float> buffer)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return ReadIntarnal(buffer);
        }

        private int ReadIntarnal(scoped Span<byte> destination)
        {
            // 書き込み先 byte 配列を float に再解釈
            Span<float> dst = Helper.ToFloatSpan(destination);

            return ReadIntarnal(dst) * sizeof(float);
        }
        private int ReadIntarnal(scoped Span<float> destination)
        {
            if (destination.IsEmpty)
                return 0;

            // sourceProvider から読み取る byte 数
            // int readByteSize = destination.Length * 2 * sizeof(float);

            Span<float> samples = ReadData(destination.Length << 3);

            if (System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated)
            {
                MonoConverter256 converter256 = new(LeftVolume, RightVolume);
                converter256.Convert(samples, destination);
            }
            else if (System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated)
            {
                MonoConverter128 converter128 = new(LeftVolume, RightVolume);
                converter128.Convert(samples, destination);
            }
            else
            {
                Convert(samples, destination, LeftVolume, RightVolume, 0);
            }
            return samples.Length >> 1;
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

            // readByteSize = sourceProvider.Read(sourceBuffer, 0, readByteSize); // 読み取り byte 数
            readByteSize = sourceProvider.Read(sourceBuffer.AsSpan(0, readByteSize)); // 読み取り byte 数

            // 読み取った byte データを float に再解釈
            return Helper.CreateSpan<byte, float>(sourceBuffer, readByteSize >> 2);
        }

        private static void Convert(scoped ReadOnlySpan<float> samples, scoped Span<float> destination, float leftVolume, float rightVolume, int start)
        {
            int dstIndex = 0;
            for (; start < samples.Length; start += 2)
            {
                float leftSample = samples[start];
                float rightSample = samples[start + 1];
                float monoSample = leftSample * leftVolume + rightSample * rightVolume;
                destination[dstIndex++] = monoSample;
            }
        }

        private struct MonoConverter256
        {
            private System.Runtime.Intrinsics.Vector256<float> volumeVector;

            public MonoConverter256(float leftVolume, float rightVolume)
            {
                volumeVector = System.Runtime.Intrinsics.Vector256.Create(leftVolume, rightVolume, leftVolume, rightVolume, leftVolume, rightVolume, leftVolume, rightVolume);
            }

            public readonly void Convert(scoped ReadOnlySpan<float> samples, scoped Span<float> destination)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, samples.Length >> 1);

                ReferenceWriter<float> writer = new(ref MemoryMarshal.GetReference(destination));
                ConvertIntrinsics(Helper.ToVector256(samples), ref writer);

                // (samples.Length >> 3) == (samples.Length / 8)
                ConvertRemaining(samples, ref writer, samples.Length >> 3, volumeVector[0], volumeVector[1]);
            }
            private readonly void ConvertIntrinsics(scoped ReadOnlySpan<System.Runtime.Intrinsics.Vector256<float>> vectors, scoped ref ReferenceWriter<float> destination)
            {
                if (System.Runtime.Intrinsics.X86.Sse3.IsSupported)
                {
                    for (int i = 0; i < vectors.Length; i++)
                    {
                        var vector = vectors[i] * volumeVector;

                        ref System.Runtime.Intrinsics.Vector128<float> dstVector = ref destination.CurrentAs<System.Runtime.Intrinsics.Vector128<float>>();

                        /*
                         * [A0 + A1, A2 + A3, B0 + B1, B2 + B3] = HorizontalAdd([A0, A1, A2, A3], [B0, B1, B2, B3]);
                         */
                        dstVector = System.Runtime.Intrinsics.X86.Sse3.HorizontalAdd(System.Runtime.Intrinsics.Vector256.GetLower(vector), System.Runtime.Intrinsics.Vector256.GetUpper(vector));

                        destination.Position += 4;
                    }
                }
                else
                {
                    for (int i = 0; i < vectors.Length; i++)
                    {
                        var vector = vectors[i] * volumeVector;
                        vector *= volumeVector;

                        destination.Write(vector[0] + vector[1]);
                        destination.Write(vector[2] + vector[3]);
                        destination.Write(vector[4] + vector[5]);
                        destination.Write(vector[6] + vector[7]);
                    }
                }
            }
            internal static void ConvertRemaining(scoped ReadOnlySpan<float> samples, scoped ref ReferenceWriter<float> writer, int startIndex, float leftVolume, float rightVolume)
            {
                for (int i = startIndex; i < samples.Length; i += 2)
                {
                    float leftSample = samples[i];
                    float rightSample = samples[i + 1];
                    float monoSample = leftSample * leftVolume + rightSample * rightVolume;
                    writer.Write(monoSample);
                }
            }
        }

        private struct MonoConverter128
        {
            private System.Runtime.Intrinsics.Vector128<float> volumeVector;

            public MonoConverter128(float leftVolume, float rightVolume)
            {
                volumeVector = System.Runtime.Intrinsics.Vector128.Create(leftVolume, rightVolume, leftVolume, rightVolume);
            }

            public readonly void Convert(scoped ReadOnlySpan<float> samples, scoped Span<float> destination)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, samples.Length >> 1);

                ReferenceWriter<float> writer = new(ref MemoryMarshal.GetReference(destination));
                ConvertIntrinsics(Helper.ToVector128(samples), ref writer);

                // (samples.Length >> 2) == (samples.Length / 4)
                MonoConverter256.ConvertRemaining(samples, ref writer, samples.Length >> 2, volumeVector[0], volumeVector[1]);
            }
            private readonly void ConvertIntrinsics(scoped ReadOnlySpan<System.Runtime.Intrinsics.Vector128<float>> vectors, scoped ref ReferenceWriter<float> destination)
            {
                for (int i = 0; i < vectors.Length; i++)
                {
                    var vector = vectors[i] * volumeVector;

                    destination.Write(vector[0] + vector[1]);
                    destination.Write(vector[2] + vector[3]);
                }
            }
        }
    }
}
