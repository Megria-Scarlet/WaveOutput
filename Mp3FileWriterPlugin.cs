using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin.FileWriter;
using YukkuriMovieMaker.Project;

namespace MegriaCore.YMM4.WaveOutput
{
    public class Mp3FileWriterPlugin : IVideoFileWriterPlugin
    {

        /// <summary>
        /// プラグインがサポートする出力パスの形式（ファイル or フォルダ or なし）
        /// </summary>
        public VideoFileWriterOutputPath OutputPathMode => VideoFileWriterOutputPath.File;

        private OutputOption? outputOption;

        /// <summary>
        /// プラグインの名前
        /// </summary>
        public string Name => name;
        private const string name = "MP3 出力";

        /// <summary>
        /// 動画出力クラスを作成する
        /// </summary>
        /// <param name="path">出力パス</param>
        /// <param name="videoInfo">動画サイズ等の情報</param>
        /// <returns>動画出力クラス</returns>
        public IVideoFileWriter CreateVideoFileWriter(string path, VideoInfo videoInfo)
        {
            return new Mp3FileWriter(path, videoInfo, outputOption!);
        }

        /// <summary>
        /// 動画の出力に必要なリソースファイルをダウンロードする
        /// </summary>
        /// <param name="progress">進捗状況の通知クラス</param>
        /// <param name="token">キャンセル用トークン</param>
        /// <returns></returns>
        public Task DownloadResources(ProgressMessage progress, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// ファイルの拡張子
        /// <see cref="OutputPathMode"/> が <see cref="VideoFileWriterOutputPath.File"/> の場合に呼ばれる
        /// </summary>
        /// <returns></returns>
        public string GetFileExtention()
        {
            return ".mp3";
        }

        /// <summary>
        /// 設定コントロールを取得する
        /// </summary>
        /// <param name="projectName">プロジェクトの名前</param>
        /// <param name="videoInfo">動画サイズ等の情報</param>
        /// <param name="length">動画の長さ（フレーム数）</param>
        /// <returns></returns>
        public UIElement GetVideoConfigView(string projectName, VideoInfo videoInfo, int length)
        {
            outputOption = new StaticMp3OutputOption();
            WaveOptionControl optionControl = new(outputOption);

            return optionControl;
        }

        /// <summary>
        /// 動画の出力に必要なリソースファイルをダウンロードする必要があるかどうか
        /// </summary>
        /// <returns></returns>
        public bool NeedDownloadResources()
        {
            return false;
        }
    }
}
