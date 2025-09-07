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
        internal static string samplePresetFilePath;
        /// <summary>
        /// サンプリングプリセット
        /// </summary>
        internal static SamplePreset samplePreset;

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
            const string samplePresetFileName = "SamplePreset.json";
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

            string? samplePresetFilePath = null;
            SamplePreset? samplePreset = null;
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
                    samplePresetFilePath = System.IO.Path.Combine(directoryInfo.FullName, samplePresetFileName);

                    #region SamplePreset インスタンス作成処理

                    System.IO.FileInfo fileInfo = new(samplePresetFilePath);
                    if (!fileInfo.Exists)
                    {
                        samplePreset = SamplePreset.GetDefaultSamplePreset();
                        SaveSampleFile(fileInfo, samplePreset);

                        // Debug
                        // _ = ShowMessage($"{fileInfo.FullName} を作成しました。");
                    }
                    else
                    {
                        samplePreset = LoadSampleFile(fileInfo);
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
            Resource.samplePresetFilePath = samplePresetFilePath!;
            Resource.samplePreset = samplePreset ?? SamplePreset.GetDefaultSamplePreset();
        }

        internal static SamplePreset LoadSampleFile(FileInfo fileInfo)
        {

            using FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);
            var result = System.Text.Json.JsonSerializer.Deserialize<SamplePreset>(fileStream, jsonSerializerOptions)!;

            // Debug
            // _ = ShowMessage($"{fileInfo.FullName} を読み取りました。");

            return result;
        }
        public static void ReloadSampleFile()
        {
            try
            {
                samplePreset = LoadSampleFile(new System.IO.FileInfo(samplePresetFilePath));
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
                FileInfo fileInfo = new FileInfo(samplePresetFilePath);
                if (fileInfo.Exists)
                {
                    SaveSampleFile(fileInfo, samplePreset);
                }
            }
            catch
            {

            }
            */
        }
    }
}
