using NAudio.Wave;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Windows.Globalization.DateTimeFormatting;
using YukkuriMovieMaker.Plugin.FileWriter;
using YukkuriMovieMaker.Project;

namespace MegriaCore.YMM4.WaveOutput
{
    public class WaveOutput : IVideoFileWriter, IVideoFileWriter2
    {
        protected bool disposedValue;

        public bool IsDisposed => disposedValue;

        protected string filePath;
        public string FilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return filePath;
            }
        }
        protected VideoInfo videoInfo;
        public VideoInfo VideoInfo
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return videoInfo;
            }
        }
        protected OutputOption outputOption;
        public OutputOption OutputOption
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return outputOption;
            }
        }

        protected WaveFormat outputFormat;
        public WaveFormat OutputFormat
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return outputFormat;
            }
        }

        public VideoFileWriterSupportedStreams SupportedStreams => VideoFileWriterSupportedStreams.Audio;

        protected string? tempPath; // 一時ファイルパス

        protected FileStream fileStream; // 書き込み先のファイルストリーム
        // protected NAudio.Wave.WaveFileWriter fileWriter; // 書き込み先のファイルライター
        protected WaveFileWriterEx fileWriter; // 書き込み先のファイルライター

        public WaveOutput()
        {
            this.filePath = null!;
            this.videoInfo = null!;
            this.outputOption = null!;
            this.outputFormat = null!;
            fileStream = null!;
            this.fileWriter = null!;
        }
        public WaveOutput(string filePath, VideoInfo videoInfo, OutputOption option)
        {
            this.filePath = filePath;
            this.videoInfo = videoInfo;
            this.outputOption = option;
            this.outputFormat = ToWaveFormat(option);

            StreamInit();
        }

        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(fileStream), nameof(fileWriter))]
        protected virtual void StreamInit()
        {
            // 出力形式のサンプリング数が入力の形式と等しい場合、直接ファイルを作成する
            if (videoInfo.Hz == this.outputFormat.SampleRate && this.outputFormat.Channels == 2)
            {
                try
                {
                    fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    fileStream.SetLength(0);
                }
                catch
                {
                    fileStream?.Dispose(); // 何らかの影響で作成できなかった場合は throw する
                    throw;
                }
                if (outputFormat.BitsPerSample == 16)
                {
                    fileWriter = new Wave16FileWriter(fileStream, outputFormat);
                }
                else if (outputFormat.BitsPerSample == 24)
                {
                    fileWriter = new Wave24FileWriter(fileStream, outputFormat);
                }
                else if (outputFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    fileWriter = new WaveFloatFileWriter(fileStream, outputFormat);
                }
                else
                {
                    fileWriter = new(fileStream, outputFormat);
                }
            }
            // 出力形式のサンプリング数が入力の形式と異なる場合、一時ファイルを作成する
            else
            {
                // tempPath = Path.ChangeExtension(this.filePath, "tmp");
                do
                {
                    // 重複しない一時フォルダー内のファイルパスを作成
                    tempPath = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), "tmp");
                }
                while (File.Exists(tempPath));
                try
                {
                    // 一時フォルダーのファイルストリームを作成
                    fileStream = new FileStream(tempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                }
                catch
                {
                    fileStream?.Dispose(); // 何らかの影響で作成できなかった場合は throw する
                    throw;
                }
                WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(videoInfo.Hz, 2); //new(videoInfo.Hz, 2);

                fileWriter = new WaveFloatFileWriter(fileStream, waveFormat);
            }
        }

        public void WriteAudio(float[] samples)
        {
            try
            {
                fileWriter.WriteSamples(samples, 0, samples.Length);
            }
            catch (OutOfMemoryException ex)
            {
                string drive = Path.GetPathRoot(tempPath) ?? string.Empty;
                string message = $"{ex.GetType().Name} エラーが発生しました。\n\"{drive}\" ドライブの容量が不足している可能性があります。\n\n{ex.Message}";

                MessageBox.Show(message, $"エラー {ex.GetType().Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (ObjectDisposedException ex)
            {
                Helper.ShowErrer(ex);
                throw;
            }
        }

        public void WriteVideo(ID2D1Bitmap1 frame)
        {
            
        }

        protected virtual void Save()
        {
            // MessageBox.Show(outputOption.BitsAndChannel?.Label ?? "null");
            if (fileWriter is not null && tempPath is not null)
            {
                var tmpFormat = fileWriter.WaveFormat;
                var outputFormat = this.outputFormat;

                // 出力フォーマットが一時ファイルフォーマットと同一の場合はファイルを移動させる
                if (Equals(tmpFormat, outputFormat))
                {
                    fileWriter.Dispose();
                    fileStream.Dispose();
                    fileStream = null!;

                    FileInfo fileInfo = new(tempPath);
                    fileInfo.MoveTo(filePath);

                    tempPath = null; // 解放処理中に不正なファイルを削除しないように null を代入
                    return;
                }

                try
                {
                    fileWriter.Flush();
                    fileStream.Position = 0;
                }
                catch (IOException ex)
                {
                    throw new IOException("Stream のシークでエラーが発生しました。\n" + ex.Message, ex);
                }

                try
                {
                    using WaveStream waveStream = new WaveFileReader(fileStream);

                    if (tmpFormat.SampleRate != outputFormat.SampleRate)
                    {
                        using MediaFoundationResampler resampler = new(waveStream, outputFormat);
                        CreateWaveFile(filePath, resampler);
                    }
                    else
                    {
                        switch (outputFormat.Channels)
                        {
                            case 1:
                                using (WaveFloatStereoToMonoWaveProvider monoProvider = new(waveStream))
                                {
                                    CreateWaveFile(filePath, monoProvider, outputFormat);
                                }
                                return;
                            case 2:
                                throw new NotImplementedException();
                            default:
                                //「出力フォーマットは無効なチャンネル数です。」
                                throw new FormatException("Output format has an invalid number of channels.");
                        }
                        // Save(waveStream, filePath, outputFormat);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("ファイル出力でエラーが発生しました。", ex);
                }
            }
        }
        [Obsolete]
        private static void Save(NAudio.Wave.IWaveProvider waveProvider, string filePath, WaveFormat outFormat)
        {

            switch (outFormat.Channels)
            {
                case 1:
                    using (WaveFloatStereoToMonoWaveProvider monoProvider = new(waveProvider))
                    {
                        CreateWaveFile(filePath, monoProvider, outFormat);
                    }
                    // NAudio.Wave.SampleProviders.WaveToSampleProvider sampleProvider = new(waveProvider);
                    // WriteSampleToMonaural(sampleProvider, filePath, outFormat);
                    return;
                case 2:
                    break;
                default:
                    //「出力フォーマットは無効なチャンネル数です。」
                    throw new FormatException("Output format has an invalid number of channels.");
            }

            if (outFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                CreateWaveFile(filePath, waveProvider);
                return;
            }

            switch (outFormat.BitsPerSample)
            {
                case 16:
                    {
                        WaveFloatTo16Provider provider = new(waveProvider);
                        CreateWaveFile(filePath, provider);
                    }
                    return;
                case 24:
                    {
                        using WaveFloatTo24Provider provider = new(waveProvider);
                        CreateWaveFile(filePath, provider);
                    }
                    return;
            }
            //「出力フォーマットは無効なビット数です。」
            throw new FormatException("Output format has an invalid number of bits.");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)

                    try
                    {
                        if (!string.IsNullOrEmpty(tempPath))
                            Save();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("出力時にエラーが発生しました。\n" + ex.Message, $"エラー {ex.GetType().Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        if (fileStream is not null && !string.IsNullOrEmpty(tempPath))
                        {
                            // 安全にファイルを削除するためにファイルサイズを比較する
                            long byteSize = fileStream.Length;

                            fileWriter.Dispose();
                            fileStream.Dispose();

                            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                            {
                                FileInfo fileInfo = new FileInfo(tempPath);
                                if (fileInfo.Length == byteSize)
                                {
                                    /*
                                    string s = Path.ChangeExtension(filePath, ".tmp");
                                    if (!File.Exists(s) ||
                                        MessageBox.Show($"{s} が存在するため、\n一時ファイルを移動できませんでした。\n上書きしますか？", "エラー", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
                                    {
                                        fileInfo.MoveTo(s, true);
                                    }
                                    */
                                    fileInfo.Delete();
                                }
                            }
                        }
                        else
                        {
                            fileWriter?.Dispose();
                        }
                    }
                }

                filePath = null!;
                videoInfo = null!;

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~WaveFileWriter()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /*
        
        YMM4側でチェックするので使用せず。
        
        /// <summary>
        /// ファイルパスの重複を避けたパスを返します。
        /// </summary>
        /// <param name="path">ファイルパス。</param>
        /// <returns>重複しないファイルパス。</returns>
        public static string EffectivePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            if (!File.Exists(path))
                return path;
            int i = 1;
            do
            {
                string s = $"{path}({i++})";
                if (!File.Exists(s))
                    return s;
            }
            while (true);
        }
        */

        /// <summary>
        /// 指定した <see cref="IWaveProvider"/> オブジェクトのデータを、指定したファイルパスに保存します。
        /// </summary>
        /// <remarks>
        /// <see cref="ArrayPool{T}"/> を使用した、 <see cref="NAudio.Wave.WaveFileWriter.CreateWaveFile(string, IWaveProvider)"/> と同等なメソッドです。
        /// </remarks>
        /// <param name="filename">保存先のファイルパス。</param>
        /// <param name="sourceProvider">保存する <see cref="IWaveProvider"/> オブジェクト。</param>
        public static void CreateWaveFile<TWave>(string filename, TWave sourceProvider)
            where TWave : IWaveProvider
        {
            using NAudio.Wave.WaveFileWriter waveFileWriter = new(filename, sourceProvider.WaveFormat);

            var pool = ArrayPool<byte>.Shared;

            int length = sourceProvider.WaveFormat.AverageBytesPerSecond * 4;
            byte[] array = pool.Rent(length);
            try
            {
                while (true)
                {
                    int num = sourceProvider.Read(array, 0, length);
                    if (num == 0)
                    {
                        break;
                    }

                    waveFileWriter.Write(array, 0, num);
                }
            }
            finally
            {
                pool.Return(array);
            }
        }
        /// <summary>
        /// 指定した <see cref="ISampleProvider"/> オブジェクトのデータを、指定したファイルパスに保存します。
        /// </summary>
        /// <remarks>
        /// <see cref="ArrayPool{T}"/> を使用した、 <see cref="NAudio.Wave.WaveFileWriter.CreateWaveFile(string, IWaveProvider)"/> と同等なメソッドです。
        /// </remarks>
        /// <param name="filename">保存先のファイルパス。</param>
        /// <param name="sampleProvider">保存する <see cref="ISampleProvider"/> オブジェクト。</param>
        public static void CreateWaveFile<TSample>(string filename, TSample sampleProvider, WaveFormat outputFormat)
            where TSample : ISampleProvider
        {
            using NAudio.Wave.WaveFileWriter waveFileWriter = new(filename, outputFormat);
            ArrayPool<float> pool = ArrayPool<float>.Shared;

            int readSize = sampleProvider.WaveFormat.AverageBytesPerSecond * 4;
            float[] readBuffer = pool.Rent(readSize);

            try
            {
                int i;
                do
                {
                    i = sampleProvider.Read(readBuffer, 0, readSize);
                    if (i == 0)
                        break;
                    waveFileWriter.WriteSamples(readBuffer, 0, i);
                }
                while (true);
            }
            finally
            {
                pool.Return(readBuffer);
            }
        }

        private static WaveFormat ToWaveFormat(OutputOption option)
        {
            int hertz;
            int bits;
            int channel;
            {
                var sample = option.Samples.ElementAtOrDefault(option.SampleIndex);
                hertz = sample is null ? 44100 : Math.Max(sample.Sample, 8000);
                var bitsAndChannel = option.BitsAndChannel!;
                bits = bitsAndChannel.Bits;
                channel = bitsAndChannel.Channel;
            }
            bool isIeeeFloat = bits == -1;
            return isIeeeFloat ? WaveFormat.CreateIeeeFloatWaveFormat(hertz, channel) : new WaveFormat(hertz, bits, channel);
        }

        /// <summary>
        /// 2 つの <see cref="WaveFormat"/> の値が等しいかどうかを判定します。
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>2 つの <see cref="WaveFormat"/> の値が等しい場合は <see langword="true"/> 、それ以外の場合は <see langword="false"/> 。</returns>
        public static bool Equals(WaveFormat? left, WaveFormat? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            if (left.Encoding != right.Encoding)
                return false;
            if (left.Channels != right.Channels)
                return false;
            if (left.SampleRate != right.SampleRate)
                return false;
            if (left.AverageBytesPerSecond != right.AverageBytesPerSecond)
                return false;
            return true;
        }
    }
}
