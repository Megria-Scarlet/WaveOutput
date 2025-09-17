using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Plugin.FileWriter;
using YukkuriMovieMaker.Project;

namespace MegriaCore.YMM4.WaveOutput
{
    public class Mp3Output : WaveOutput
    {
        public Mp3Output() { }
        public Mp3Output(string filePath, VideoInfo videoInfo, OutputOption option) : base(filePath, videoInfo, option)
        {

        }

        protected override void StreamInit()
        {
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

        protected override void Save()
        {
            if (fileWriter is not null)
            {
                var tmpFormat = fileWriter.WaveFormat;
                int hertz;
                int bits;
                int channel;
                {
                    var sample = outputOption.Samples.ElementAtOrDefault(outputOption.SampleIndex);
                    hertz = sample is null ? 44100 : Math.Max(sample.Sample, 44100);
                    var bitsAndChannel = outputOption.BitsAndChannel!;
                    bits = Math.Max(bitsAndChannel.Bits, 96000);
                    channel = bitsAndChannel.Channel;
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

                using WaveStream waveStream = new WaveFileReader(fileStream);

                if (tmpFormat.SampleRate != hertz)
                {
                    using MediaFoundationResampler resampler = new(waveStream, hertz);
                    Save(resampler, filePath, channel, bits);
                }
                else
                {
                    Save(waveStream, filePath, channel, bits);
                }
            }
        }
        private static void Save(NAudio.Wave.IWaveProvider waveProvider, string filePath, int channels, int bitRate)
        {
            switch (channels)
            {
                case 1:
                    /*
                    NAudio.Wave.SampleProviders.WaveToSampleProvider sampleProvider = new(waveProvider);
                    NAudio.Wave.SampleProviders.StereoToMonoSampleProvider monoProvider = new(sampleProvider);
                    waveProvider = new NAudio.Wave.SampleProviders.SampleToWaveProvider(monoProvider);
                    */
                    using (WaveFloatStereoToMonoWaveProvider provider = new(waveProvider))
                    {
                        MediaFoundationEncoder.EncodeToMp3(provider, filePath, bitRate);
                    }
                    return;
                case 2:
                    MediaFoundationEncoder.EncodeToMp3(waveProvider, filePath, bitRate);
                    return;
                default:
                    //「出力フォーマットは無効なチャンネル数です。」
                    throw new FormatException("Output format has an invalid number of channels.");
            }
        }
    }
}
