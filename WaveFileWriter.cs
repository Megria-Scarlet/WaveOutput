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
using Windows.Win32.System.Com;
using YukkuriMovieMaker.Plugin.FileWriter;
using YukkuriMovieMaker.Project;

namespace MegriaCore.YMM4.WaveOutput
{
    public class WaveFileWriter : IVideoFileWriter, IVideoFileWriter2
    {
        private bool disposedValue;

        public bool IsDisposed => disposedValue;

        private string filePath;
        public string FilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return filePath;
            }
        }
        private VideoInfo videoInfo;
        public VideoInfo VideoInfo
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return videoInfo;
            }
        }
        private OutputOption outputOption;
        public OutputOption OutputOption
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return outputOption;
            }
        }

        private string? tempPath; // 一時ファイルパス
        private FileStream fileStream; // 一時ファイルストリーム
        private NAudio.Wave.WaveFileWriter fileWriter; // 一時ファイルライター

        public VideoFileWriterSupportedStreams SupportedStreams => VideoFileWriterSupportedStreams.Audio;

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
            fileWriter!.WriteSamples(samples, 0, samples.Length);
        }

        public void WriteVideo(ID2D1Bitmap1 frame)
        {
            
        }

        private void Save()
        {
            // MessageBox.Show(outputOption.BitsAndChannel?.Label ?? "null");
            if (fileWriter is not null)
            {
                var tmpFormat = fileWriter.WaveFormat;
                int hertz = outputOption.Hertz;
                int bits;
                int channel;
                {
                    var bitsAndChannel = outputOption.BitsAndChannel!;
                    bits = bitsAndChannel.Bits;
                    channel = bitsAndChannel.Channel;
                }

                // 出力フォーマットが一時ファイルフォーマットと同一の場合はファイルを移動させる
                if (tmpFormat.SampleRate == hertz && tmpFormat.Encoding != WaveFormatEncoding.IeeeFloat && channel == 2)
                {
                    FileInfo fileInfo = new FileInfo(tempPath!);
                    fileInfo.MoveTo(filePath);

                    tempPath = null; // 解放処理中に不正なファイルを削除しないように null を代入
                    return;
                }

                WaveFormat outputFormat = bits == -1 ? WaveFormat.CreateIeeeFloatWaveFormat(hertz, channel) : new WaveFormat(hertz, bits, channel);

                fileWriter.Flush();
                fileStream.Position = 0;

                // using WaveStream waveStream = new RawSourceWaveStream(fileStream, tmpFormat);
                using WaveStream waveStream = new WaveFileReader(fileStream);

                ISampleProvider provider = new NAudio.Wave.SampleProviders.WaveToSampleProvider(waveStream);
                switch (bits)
                {
                    case 16:

                        if (channel == 1)
                        {
                            if (tmpFormat.SampleRate != outputFormat.SampleRate)
                            {
                                using MediaFoundationResampler resampler = new(waveStream, hertz);
                                NAudio.Wave.SampleProviders.WaveToSampleProvider provider1 = new(resampler);
                                WriteSampleToMonaural(provider1, filePath, outputFormat);
                            }
                            else
                            {
                                WriteSampleToMonaural(provider, filePath, outputFormat);
                            }
                            return;
                        }
                        if (channel == 2)
                        {
                            if (tmpFormat.SampleRate == outputFormat.SampleRate)
                            {
                                NAudio.Wave.WaveFileWriter.CreateWaveFile16(filePath, provider);
                            }
                            else
                            {
                                using MediaFoundationResampler resampler = new(waveStream, hertz);
                                WaveFloatTo16Provider provider16 = new(resampler);
                                NAudio.Wave.WaveFileWriter.CreateWaveFile(filePath, provider16);
                            }
                            return;
                        }

                        break;
                    case 24:
                        if (channel == 1)
                        {
                            if (tmpFormat.SampleRate == outputFormat.SampleRate)
                            {
                                // NAudio.Wave.SampleProviders.SampleToWaveProvider24 provider24 = new(provider);
                                WriteSampleToMonaural(provider, filePath, outputFormat);
                            }
                            else
                            {
                                using MediaFoundationResampler resampler = new(waveStream, hertz);
                                NAudio.Wave.SampleProviders.WaveToSampleProvider provider1 = new(resampler);
                                WriteSampleToMonaural(provider1, filePath, outputFormat);
                            }
                            return;
                        }
                        if (channel == 2)
                        {
                            if (tmpFormat.SampleRate == outputFormat.SampleRate)
                            {
                                Write24Bit(provider);
                            }
                            else
                            {
                                using MediaFoundationResampler resampler = new(waveStream, hertz);
                                NAudio.Wave.SampleProviders.WaveToSampleProvider provider1 = new(resampler);
                                Write24Bit(provider1);
                            }
                            void Write24Bit(ISampleProvider provider)
                            {
                                NAudio.Wave.SampleProviders.SampleToWaveProvider24 provider24 = new(provider);
                                NAudio.Wave.WaveFileWriter.CreateWaveFile(filePath, provider24);
                            }
                            return;
                        }
                        break;
                    case -1:
                        if (channel == 1)
                        {
                            if (tmpFormat.SampleRate == outputFormat.SampleRate)
                            {
                                WriteSampleToMonaural(provider, filePath, outputFormat);
                            }
                            else
                            {
                                using MediaFoundationResampler resampler = new(waveStream, hertz);
                                NAudio.Wave.SampleProviders.WaveToSampleProvider provider1 = new(resampler);
                                WriteSampleToMonaural(provider1, filePath, outputFormat);
                            }
                            return;
                        }
                        if (channel == 2)
                        {
                            using MediaFoundationResampler resampler = new(waveStream, hertz);
                            NAudio.Wave.WaveFileWriter.CreateWaveFile(filePath, resampler);
                            return;
                        }
                        break;
                }
                throw new FormatException("不正なフォーマットです。");
            }
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
                            fileWriter.Dispose();
                            fileStream.Dispose();
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
    }
}
