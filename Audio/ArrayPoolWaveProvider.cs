using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using WinRT;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MegriaCore.YMM4.WaveOutput
{
    /// <summary>
    /// バッファーに <see cref="byte"/> 型の <see cref="System.Buffers.ArrayPool{T}"/> を使用した <see cref="IWaveProvider"/> を提供します。
    /// </summary>
    public class ArrayPoolWaveProvider : IWaveProvider, IDisposable
    {
        protected bool disposedValue;
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => disposedValue;
        }

        protected IWaveProvider sourceProvider;
        WaveFormat IWaveProvider.WaveFormat
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return sourceProvider.WaveFormat;
            }
        }

        protected byte[]? sourceBuffer;
        protected System.Buffers.ArrayPool<byte> arrayPool;

        public ArrayPoolWaveProvider(IWaveProvider sourceProvider, System.Buffers.ArrayPool<byte> arrayPool)
        {
            this.sourceProvider = sourceProvider;
            this.arrayPool = arrayPool;
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
        ~ArrayPoolWaveProvider()
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

        int IWaveProvider.Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            return sourceProvider.Read(buffer, offset, count);
        }
        /// <summary>
        /// 指定した長さ以上の配列をバッファーに確保します。
        /// </summary>
        /// <param name="sourceBytesRequired">確保する配列の長さ。</param>
        /// <returns>指定した長さ以上のバッファー配列。</returns>
        protected byte[] EnsureSourceBuffer(int sourceBytesRequired)
        {
            ref byte[]? buffer = ref sourceBuffer;
            if (buffer is null)
            {
                buffer = arrayPool.Rent(sourceBytesRequired);
            }
            else if(buffer.Length < sourceBytesRequired)
            {
                arrayPool.Return(buffer);
                buffer = arrayPool.Rent(sourceBytesRequired);
            }
            return buffer;
        }
    }
}
