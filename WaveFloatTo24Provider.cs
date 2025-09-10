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
    public class WaveFloatTo24Provider : IWaveProvider, IDisposable
    {
        private const int ByteSize = 3;


        protected bool disposedValue;
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => disposedValue;
        }
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
        protected IWaveProvider sourceProvider;

        protected WaveFormat waveFormat;

        public WaveFormat WaveFormat
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return waveFormat;
            }
        }

        protected byte[]? sourceBuffer;
        protected System.Buffers.ArrayPool<byte> arrayPool;


        public WaveFloatTo24Provider(IWaveProvider sourceProvider) : this(sourceProvider, System.Buffers.ArrayPool<byte>.Shared)
        {

        }
        public WaveFloatTo24Provider(IWaveProvider sourceProvider, System.Buffers.ArrayPool<byte> arrayPool)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Input wave provider must be IEEE float", nameof(sourceProvider));
            }

            if (sourceProvider.WaveFormat.BitsPerSample != 32)
            {
                throw new ArgumentException("Input wave provider must be 32 bit", nameof(sourceProvider));
            }

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 24, sourceProvider.WaveFormat.Channels);
            this.sourceProvider = sourceProvider;
            volume = 1f;
            this.arrayPool = arrayPool;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            // sourceProvider から読み取る byte 数
            int readByteSize = count / ByteSize * sizeof(float);

            #region sourceBuffer の初期化
            ref byte[]? sourceBuffer = ref this.sourceBuffer;
            if (sourceBuffer is null)
            {
                sourceBuffer = arrayPool.Rent(readByteSize);
            }
            else if (sourceBuffer.Length < readByteSize)
            {
                arrayPool.Return(sourceBuffer);
                sourceBuffer = arrayPool.Rent(readByteSize);
            }
            #endregion

            readByteSize = sourceProvider.Read(sourceBuffer, 0, readByteSize);

            // 読み取った byte データを float に再解釈
            Span<float> samples = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref MemoryMarshal.GetArrayDataReference(sourceBuffer)), readByteSize >> 2);

            Span<byte> dst = buffer.AsSpan(offset); // 書き込み先の byte スパン
            int dstIndex = 0; // 書き込み先の現在の位置


            for (int i = 0; i < samples.Length; i++)
            {
                float sample = Math.Clamp(samples[i] * volume, -1f, 1f);

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                    sourceProvider = null!;
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します

                if (this.sourceBuffer is not null)
                {
                    arrayPool.Return(this.sourceBuffer);
                    this.sourceBuffer = null!;
                }
                this.arrayPool = null!;

                disposedValue = true;
            }
        }

        // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        ~WaveFloatTo24Provider()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
