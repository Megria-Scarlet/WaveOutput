using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MegriaCore.YMM4.WaveOutput
{
    internal static class Resource
    {
        /// <summary>
        /// dll ファイルのパス。
        /// </summary>
        internal static string dllPath;

        /// <summary>
        /// サンプリングプリセットファイルのパス
        /// </summary>
        internal static string wavePresetFilePath;
        /// <summary>
        /// サンプリングプリセット
        /// </summary>
        internal static SamplePreset waveSamplePreset;

        /// <summary>
        /// サンプリングプリセットファイルのパス
        /// </summary>
        internal static string mp3PresetFilePath;
        /// <summary>
        /// サンプリングプリセット
        /// </summary>
        internal static Mp3SamplePreset mp3SamplePreset;

        /// <summary>
        /// 既定の Json シリアライズオプション
        /// </summary>
        internal static readonly System.Text.Json.JsonSerializerOptions jsonSerializerOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
            WriteIndented = true,
            IndentSize = 4
        };

        static Resource()
        {

            #region string 定数
            const string pluginDirectoryName = "plugin";
            const string wavePresetFileName = "WavePreset.json";
            const string mp3PresetFileName = "Mp3Preset.json";
            #endregion

            // アセンブリが Unload された時の処理を登録
            {
                var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(Resource).Assembly);
                if (context is not null)
                {
                    context.Unloading += Unload;
                }
            }

            string? dllPath;
            try
            {
                // この dll ファイルの Assembly を取得。
                var myAssembly = System.Reflection.Assembly.GetExecutingAssembly()!;
                dllPath = myAssembly.Location;
            }
            catch
            {
                dllPath = null;
            }

            string? wavePresetFilePath = null;
            SamplePreset? wavePreset = null;

            string? mp3PresetFilePath = null;
            Mp3SamplePreset? mp3Preset = null;
            if (dllPath is not null)
            {
                System.IO.DirectoryInfo directoryInfo = new(System.IO.Path.GetDirectoryName(dllPath) ?? string.Empty);
                try
                {
                    if (directoryInfo.Name == pluginDirectoryName) // plugin フォルダー直下に dll がある場合
                    {
                        // dll ファイル名と同名のフォルダーを作成する
                        directoryInfo = new(System.IO.Path.Combine(directoryInfo.FullName, System.IO.Path.GetFileNameWithoutExtension(dllPath)));
                        if (!directoryInfo.Exists)
                        {
                            directoryInfo.Create();

                            // Debug
                            // _ = ShowMessage($"{directoryInfo.FullName} を作成しました。");
                        }
                    }
                    wavePresetFilePath = System.IO.Path.Combine(directoryInfo.FullName, wavePresetFileName);

                    #region WaveSamplePreset インスタンス作成処理

                    System.IO.FileInfo fileInfo = new(wavePresetFilePath);
                    if (!fileInfo.Exists)
                    {
                        wavePreset = SamplePreset.GetDefaultSamplePreset();
                        SaveSampleFile(fileInfo, wavePreset);

                        // Debug
                        // _ = ShowMessage($"{fileInfo.FullName} を作成しました。");
                    }
                    else
                    {
                        wavePreset = LoadSampleFile<SamplePreset>(fileInfo);
                    }

                    #endregion

                    mp3PresetFilePath = System.IO.Path.Combine(directoryInfo.FullName, mp3PresetFileName);

                    #region WaveSamplePreset インスタンス作成処理

                    fileInfo = new(mp3PresetFilePath);
                    if (!fileInfo.Exists)
                    {
                        mp3Preset = Mp3SamplePreset.GetDefaultSamplePreset();
                        SaveSampleFile(fileInfo, mp3Preset);

                        // Debug
                        // _ = ShowMessage($"{fileInfo.FullName} を作成しました。");
                    }
                    else
                    {
                        mp3Preset = LoadSampleFile<Mp3SamplePreset>(fileInfo);
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    // Debug
                    _ = ShowErrer(ex);
                }
            }

            Resource.dllPath = dllPath!;
            Resource.wavePresetFilePath = wavePresetFilePath!;
            Resource.waveSamplePreset = wavePreset ?? SamplePreset.GetDefaultSamplePreset();
            Resource.mp3PresetFilePath = mp3PresetFilePath!;
            Resource.mp3SamplePreset = mp3Preset ?? Mp3SamplePreset.GetDefaultSamplePreset();
        }

        internal static T LoadSampleFile<T>(FileInfo fileInfo) where T : SamplePreset
        {

            using FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(fileStream, jsonSerializerOptions)!;

            // Debug
            // _ = ShowMessage($"{fileInfo.FullName} を読み取りました。");

            return result;
        }
        public static void ReloadSampleFile()
        {
            try
            {
                waveSamplePreset = LoadSampleFile<SamplePreset>(new System.IO.FileInfo(wavePresetFilePath));
            }
            catch (Exception ex)
            {
                // Debug
                _ = ShowErrer(ex);
            }
            try
            {
                mp3SamplePreset = LoadSampleFile<Mp3SamplePreset>(new System.IO.FileInfo(mp3PresetFilePath));
            }
            catch (Exception ex)
            {
                // Debug
                _ = ShowErrer(ex);
            }
        }
        internal static void SaveSampleFile(FileInfo fileInfo, SamplePreset sample)
        {
            using FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);
            System.Text.Json.JsonSerializer.Serialize(fileStream, sample, jsonSerializerOptions);
        }
        internal static void SaveSampleFile(FileInfo fileInfo, Mp3SamplePreset sample)
        {
            using FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);
            System.Text.Json.JsonSerializer.Serialize(fileStream, sample, jsonSerializerOptions);
        }

        internal static MessageBoxResult ShowErrer(Exception exception)
        {
            string message = $"エラーが発生しました。\n{exception.Message}";
            // Debug
            return MessageBox.Show(message, $"デバッグ情報 - エラー [{exception.GetType().Name}]", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        internal static MessageBoxResult ShowMessage(string message)
        {
            // Debug
            return MessageBox.Show(message, "デバッグ情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void Unload(System.Runtime.Loader.AssemblyLoadContext context)
        {
            /*
            try
            {
                FileInfo fileInfo = new FileInfo(waveSamplePresetFilePath);
                if (fileInfo.Exists)
                {
                    SaveSampleFile(fileInfo, waveSamplePreset);
                }
            }
            catch
            {

            }
            */
        }
    }
}
