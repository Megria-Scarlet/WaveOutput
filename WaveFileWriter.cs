using NAudio.Wave;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Plugin.FileWriter;
using YukkuriMovieMaker.Project;

namespace MegriaCore.YMM4.WaveOutput
{
    public class WaveFileWriter : IVideoFileWriter, IVideoFileWriter2
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

        public VideoFileWriterSupportedStreams SupportedStreams => VideoFileWriterSupportedStreams.Audio;

        protected string? tempPath; // 一時ファイルパス
        protected FileStream fileStream; // 一時ファイルストリーム
        protected NAudio.Wave.WaveFileWriter fileWriter; // 一時ファイルライター

        public WaveFileWriter()
        {
            this.filePath = null!;
            this.videoInfo = null!;
            this.outputOption = null!;
            fileStream = null!;
            this.fileWriter = null!;
        }
        public WaveFileWriter(string filePath, VideoInfo videoInfo, OutputOption option)
        {
            this.filePath = filePath;
            this.videoInfo = videoInfo;
            this.outputOption = option;

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
            fileWriter = new(fileStream, waveFormat);
        }

        public void WriteAudio(float[] samples)
        {
            try
            {
                fileWriter!.WriteSamples(samples, 0, samples.Length);
            }
            catch (OutOfMemoryException ex)
            {
                string message;
                if (fileStream.Length < 4 * 1024 * 1024 * 1024L)
                {
                    string drive = Path.GetPathRoot(tempPath) ?? string.Empty;
                    message = $"{ex.GetType().Name} エラーが発生しました。\n\"{drive}\" ドライブの容量が不足している可能性があります。\n\n{ex.Message}";
                }
                else
                {
                    message = $"{ex.GetType().Name} エラーが発生しました。\n4 GB を超える .wav ファイルは作成できません。\n\n{ex.Message}";
                }
                 
                MessageBox.Show(message, $"エラー {ex.GetType().Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public void WriteVideo(ID2D1Bitmap1 frame)
        {
            
        }

        protected virtual void Save()
        {
            // MessageBox.Show(outputOption.BitsAndChannel?.Label ?? "null");
            if (fileWriter is not null)
            {
                var tmpFormat = fileWriter.WaveFormat;
                int hertz;
                int bits;
                int channel;
                {
                    var sample = outputOption.Samples.ElementAtOrDefault(outputOption.SampleIndex);
                    hertz = sample is null ? 44100 : Math.Max(sample.Sample, 8000);
                    var bitsAndChannel = outputOption.BitsAndChannel!;
                    bits = bitsAndChannel.Bits;
                    channel = bitsAndChannel.Channel;
                }
                bool isIeeeFloat = bits == -1;
                // 出力フォーマットが一時ファイルフォーマットと同一の場合はファイルを移動させる
                if (tmpFormat.SampleRate == hertz && isIeeeFloat && channel == 2)
                {
                    fileWriter.Dispose();
                    fileStream.Dispose();

                    FileInfo fileInfo = new FileInfo(tempPath!);
                    fileInfo.MoveTo(filePath);

                    tempPath = null; // 解放処理中に不正なファイルを削除しないように null を代入
                    return;
                }

                WaveFormat outputFormat = isIeeeFloat ? WaveFormat.CreateIeeeFloatWaveFormat(hertz, channel) : new WaveFormat(hertz, bits, channel);

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
                    // using WaveStream waveStream = new RawSourceWaveStream(fileStream, tmpFormat);
                    using WaveStream waveStream = new WaveFileReader(fileStream);

                    if (tmpFormat.SampleRate != outputFormat.SampleRate)
                    {
                        using MediaFoundationResampler resampler = new(waveStream, hertz);
                        Save(resampler, filePath, outputFormat);
                    }
                    else
                    {
                        Save(waveStream, filePath, outputFormat);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("ファイル出力でエラーが発生しました。", ex);
                }
                return;
            }
        }

        private static void Save(NAudio.Wave.IWaveProvider waveProvider, string filePath, WaveFormat outFormat)
        {

            switch (outFormat.Channels)
            {
                case 1:
                    NAudio.Wave.SampleProviders.WaveToSampleProvider sampleProvider = new(waveProvider);
                    WriteSampleToMonaural(sampleProvider, filePath, outFormat);
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

        /// <summary>
        /// <paramref name="provider"/> をモノラル化してファイルを保存します。
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="filePath"></param>
        /// <param name="format"></param>
        private static void WriteSampleToMonaural(ISampleProvider provider, string filePath, WaveFormat format)
        {
            using NAudio.Wave.WaveFileWriter writer = new(filePath, format);
            provider = new NAudio.Wave.SampleProviders.StereoToMonoSampleProvider(provider);

            ArrayPool<float> floatPool = ArrayPool<float>.Shared;

            const int oneM = 1024 * 1024;

            float[] readBuffer = floatPool.Rent(oneM); // 4 MB

            int i;
            do
            {
                i = provider.Read(readBuffer, 0, oneM);
                if (i <= 0)
                    break;
                writer.WriteSamples(readBuffer, 0, i);
            }
            while (true);

            floatPool.Return(readBuffer);
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
                        Save();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("出力時にエラーが発生しました。\n" + ex.Message, $"エラー {ex.GetType().Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        if (fileStream is not null)
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
        public static void CreateWaveFile(string filename, IWaveProvider sourceProvider)
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
    }
}
