# **WaveOutput**

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/license/mit)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![OS](https://img.shields.io/badge/OS-Windows-blue.svg)
![ver.2.1.0](https://img.shields.io/badge/Release-ver.2.1-red.svg)

制作: Megria  
GitHub: <https://github.com/Megria-Scarlet/WaveOutput>  

## 概要

[YMM4][link-ymm4] の動画出力の項目に [**WAV 出力**] と [**MP3 出力**] を追加する [YMM4][link-ymm4] 向けのプラグインです。  
[**WAV 出力**] は [**連番 PNG + WAV 出力**] とは異なり、 WAV の出力形式をある程度決められます。

## 出力フォーマット

### サンプリング数

ver.1.1 以降では出力サンプリング数を `.json` ファイルで編集できるようになりました。  
`"WaveOutput.dll"` と同じ位置(`"plugin"` フォルダー直下の場合は `"WaveOutput"` フォルダー下)にある、下記の通りの `.json` ファイルを読み取ります。  

|       ver       |          wav          |         mp3        |
|:---------------:|:---------------------:|:------------------:|
| ![ver.1.1.0][]  | `"SamplePreset.json"` |          -         |
| ![ver.2.0.0~][] |  `"WavePreset.json"`  | `"Mp3Preset.json"` |

> ファイルが存在しない場合は自動生成されます。  
> .json ファイルの読み取り時にエラーが発生した場合は既定値を読み取ります。  
> サンプリングレートの最低値は 8,000 Hz です。

### ビット数 ([**WAV 出力**] のみ)

* 16 bit
* 24 bit
* 32 bit (Float)

### ビットレート ([**MP3 出力**] のみ)

`"Mp3Preset.json"` ファイルから値を読み取ります。  
> ビットレートの最低値は 96 kbps です。

### チャンネル数

* モノラル (1 ch)
* ステレオ (2 ch)

## 免責事項

> 警告  
**このプラグインを使用して発生した損害について、制作者はいっさい保証しません。**  

## サポート

不具合・ご要望などの連絡は [Twitter(現:X)][link-twitter] にてお願いします。  
※対応できないことの方が多いと思います。  

## 更新履歴

* ver.1.0 (2025/09/07)

  * 公開

* ver.1.1 (2025/09/07)

  * エンコード設定の初期項目の値を変更
  * エンコード設定の UI を調整
  * Readme ファイルの内容を修正
  * サンプリング数のプリセットに対応

* ver.2.0 (2025/09/10)
  * wav 出力の処理を修正
  * 24 bit 形式 wav 出力の処理を最適化
  * プリセットファイルを GUI から開ける機能を追加
  * プリセットファイルを GUI から再読み込みする機能を追加
  * [**MP3 出力**] 機能を追加

* ver.2.1 - beta
  * 編集時のサンプリングレートと出力時のサンプリングが同じ場合は直接ファイルを作成するように最適化
  * モノラル化の処理を最適化

[link-ymm4]:https://manjubox.net/ymm4/
[link-twitter]:https://x.com/Megria1201

[ver.1.1.0]: https://img.shields.io/badge/ver.1.1-red.svg
[ver.2.0.0~]: https://img.shields.io/badge/ver.2.0_~-red.svg
