using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MegriaCore.YMM4.WaveOutput
{
    [JsonConverter(typeof(SamplePresetJsonConverter))]
    public class SamplePreset
    {
        protected int defaultSampleIndex;

        protected List<SamplePresetValue> samples;

        public SamplePreset()
        {
            defaultSampleIndex = 0;
            samples = [];
        }

        public SamplePreset(IEnumerable<SamplePresetValue> presetValues, int defaultIndex = 0)
        {
            this.samples = new(presetValues);
            this.defaultSampleIndex = defaultIndex;
        }

        public SamplePreset(List<SamplePresetValue> presetValues, int defaultIndex = 0)
        {
            this.samples = presetValues;
            this.defaultSampleIndex = defaultIndex;
        }

        public int DefaultSampleIndex
        {
            get => defaultSampleIndex;
            set => defaultSampleIndex = value;
        }

        public List<SamplePresetValue> Samples
        {
            get => samples;
        }


        /// <summary>
        /// 既定の <see cref="SamplePreset"/> を新しく作成して取得します。
        /// </summary>
        /// <returns>新しく作成した既定の <see cref="SamplePreset"/> 型のオブジェクト。</returns>
        internal static SamplePreset GetDefaultSamplePreset()
        {
            const string unit = "Hz";
            List<SamplePresetValue> values = [
                new(8000, null, unit),
                new(16000, null, unit),
                new(32000, null, unit),
                new(44100, null, unit),
                new(48000, null, unit)
                ];
            return new SamplePreset(values, 3);
        }
    }

    [JsonConverter(typeof(Mp3PresetJsonConverter))]
    public class Mp3SamplePreset : SamplePreset
    {
        protected internal int defaultBitRateIndex;

        protected internal List<BitRatePresetValue> bitRates;
        public Mp3SamplePreset()
        {
            defaultBitRateIndex = 0;
            bitRates = [];
        }

        public Mp3SamplePreset(IEnumerable<SamplePresetValue> samples, int defaultIndex = 0) : base(samples, defaultIndex)
        {
            defaultBitRateIndex = 0;
            bitRates = [];
        }

        public Mp3SamplePreset(List<SamplePresetValue> samples, int defaultIndex = 0) : base(samples, defaultIndex)
        {
            defaultBitRateIndex = 0;
            bitRates = [];
        }
        internal Mp3SamplePreset(SamplePreset preset) : base(preset.Samples, preset.DefaultSampleIndex)
        {
            defaultBitRateIndex = 0;
            bitRates = [];
        }
        public int DefaultBitRateIndex
        {
            get => defaultBitRateIndex;
            set => defaultBitRateIndex = value;
        }

        public List<BitRatePresetValue> BitRates
        {
            get => bitRates;
        }


        /// <summary>
        /// 既定の <see cref="SamplePreset"/> を新しく作成して取得します。
        /// </summary>
        /// <returns>新しく作成した既定の <see cref="SamplePreset"/> 型のオブジェクト。</returns>
        new internal static Mp3SamplePreset GetDefaultSamplePreset()
        {
            var re = new Mp3SamplePreset(SamplePreset.GetDefaultSamplePreset())
            {
                bitRates = [
                new(96000),
                new(128000),
                new(192000),
                new(256000)
                ],
                defaultBitRateIndex = 2
            };
            return re;
        }
    }

    public class LabelUnitValue : IEquatable<LabelUnitValue>
    {
        #region フィールド変数
        /// <summary>
        /// ラベル名
        /// </summary>
        protected string? label;
        /// <summary>
        /// 単位名
        /// </summary>
        protected string? unit;
        #endregion

        #region 公開プロパティ

        /// <summary>
        /// 生のラベル名を取得します。
        /// </summary>
        /// <returns>生のラベル名を示す <see cref="string"/> 型の読み取り専用な参照。</returns>
        public ref readonly string? Label
        {
            get => ref label;
        }
        /// <summary>
        /// 有効なラベル名を取得します。
        /// </summary>
        /// <returns>有効なラベル名を示す文字列。</returns>
        [JsonIgnore]
        public virtual string ActualLabel
        {
            get
            {
                return label ?? string.Empty;
            }
        }
        public string? Unit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unit;
        }

        #endregion

        public LabelUnitValue() { }
        public LabelUnitValue(string? label, string? unit)
        {
            this.label = label;
            this.unit = unit;
        }

        public bool Equals(LabelUnitValue? other)
        {
            if (ReferenceEquals(other, this))
                return true;
            if (other is null || other.GetType() != GetType())
                return false;
            return other.label == this.label && other.unit == this.unit;
        }
    }

    /// <summary>
    /// サンプリング数とラベル名を管理するクラス
    /// </summary>
    [JsonConverter(typeof(SamplePresetValueJsonConverter))]
    public class SamplePresetValue : LabelUnitValue, IEquatable<SamplePresetValue>
    {
        #region フィールド変数
        /// <summary>
        /// サンプリング数
        /// </summary>
        protected int sample;
        #endregion

        #region 公開プロパティ
        /// <summary>
        /// サンプリング数を取得します。
        /// </summary>
        /// <returns>サンプリング数を示す 32 ビット符号付き整数。</returns>
        public virtual int Sample
        {
            get => sample;
        }
        /// <summary>
        /// 有効なラベル名を取得します。
        /// </summary>
        /// <returns>有効なラベル名を示す文字列。</returns>
        [JsonIgnore]
        public override string ActualLabel
        {
            get
            {
                return label is null ? sample.ToString("N0") : label;
            }
        }

        #endregion

        #region コンストラクタ
        public SamplePresetValue(int sample)
        {
            this.sample = sample;
        }
        public SamplePresetValue(int sample, string? label, string? unit = null)
        {
            this.label = label;
            this.sample = sample;
            this.unit = unit;
        }
        #endregion

        public bool Equals(SamplePresetValue? other)
        {
            if (ReferenceEquals(other, this))
                return true;
            if (other is null || other.GetType() != GetType())
                return false;
            return other.label == this.label && other.sample == this.sample && other.unit == this.unit;
        }
        public override bool Equals(object? obj) => obj is SamplePresetValue value && Equals(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(label, sample);

        public virtual KeyValuePair<string, int> ToKeyValuePair() => new(ActualLabel, sample);

        public static bool operator ==(SamplePresetValue? left, SamplePresetValue? right)
        {
            return EqualityComparer<SamplePresetValue>.Default.Equals(left, right);
        }

        public static bool operator !=(SamplePresetValue? left, SamplePresetValue? right) => !(left == right);
    }

    [JsonConverter(typeof(BitRatePresetValueJsonConverter))]
    public class BitRatePresetValue : LabelUnitValue, IEquatable<BitRatePresetValue>
    {
        #region フィールド変数
        /// <summary>
        /// ビットレート
        /// </summary>
        protected int bitRate;
        #endregion

        #region 公開プロパティ
        /// <summary>
        /// ビットレート数を取得します。
        /// </summary>
        /// <returns>ビットレート数を示す 32 ビット符号付き整数。</returns>
        public virtual int BitRate
        {
            get => bitRate;
        }
        /// <summary>
        /// 有効なラベル名を取得します。
        /// </summary>
        /// <returns>有効なラベル名を示す文字列。</returns>
        [JsonIgnore]
        public override string ActualLabel
        {
            get
            {
                return label is null ? bitRate.ToString("N0") : label;
            }
        }

        #endregion

        #region コンストラクタ
        public BitRatePresetValue(int bitRate)
        {
            this.bitRate = bitRate;
        }
        public BitRatePresetValue(int bitRate, string? label, string? unit = null)
        {
            this.label = label;
            this.bitRate = bitRate;
            this.unit = unit;
        }
        #endregion

        public bool Equals(BitRatePresetValue? other)
        {
            if (ReferenceEquals(other, this))
                return true;
            if (other is null || other.GetType() != GetType())
                return false;
            return other.label == this.label && other.bitRate == this.bitRate && other.unit == this.unit;
        }
        public override bool Equals(object? obj) => obj is SamplePresetValue value && Equals(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(label, bitRate);
    }

    public class SamplePresetJsonConverter : JsonConverter<SamplePreset>
    {
        public override SamplePreset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // object スコープに入っていることをチェック
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Invalid TokenType");
            }

            int defaultIndex = 0;
            List<SamplePresetValue>? values = null;


            while (reader.Read())
            {
                // object スコープの末尾に達した場合
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                // reader の現在位置が PropertyName ではない場合は再び読み取る
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string propertyName = reader.GetString()!;

                if (!reader.Read())
                {
                    throw new JsonException("There is no value for the property.");
                }

                if (nameof(SamplePreset.DefaultSampleIndex).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.TryGetInt32(out defaultIndex))
                        defaultIndex = 0;
                }
                else if ("Samples".Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var array = JsonSerializer.Deserialize<List<SamplePresetValue>>(ref reader, options);
                        if (array is not null)
                        {
                            values = array;
                        }
                    }
                    catch
                    {

                    }
                }
                else
                {
                    // 互換性のために不明なプロパティのエラーを throw しない

                    // throw new JsonException("Unknown property name");
                }
            }
            return values is null ? new SamplePreset() : new SamplePreset(values, int.Clamp(defaultIndex, 0, values.Count - 1));
        }

        public override void Write(Utf8JsonWriter writer, SamplePreset value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(SamplePreset.DefaultSampleIndex), value.DefaultSampleIndex);

            writer.WritePropertyName("Samples");
            JsonSerializer.Serialize(writer, value.Samples, options);


            writer.WriteEndObject();
        }
    }


    public class Mp3PresetJsonConverter : JsonConverter<Mp3SamplePreset>
    {
        public override Mp3SamplePreset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // object スコープに入っていることをチェック
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Invalid TokenType");
            }

            int defaultSampleIndex = 0;
            int defaultBitRateIndex = 0;
            List<SamplePresetValue>? samples = null;
            List<BitRatePresetValue>? bitRates = null;


            while (reader.Read())
            {
                // object スコープの末尾に達した場合
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                // reader の現在位置が PropertyName ではない場合は再び読み取る
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string propertyName = reader.GetString()!;

                if (!reader.Read())
                {
                    throw new JsonException("There is no value for the property.");
                }

                if (nameof(SamplePreset.DefaultSampleIndex).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.TryGetInt32(out defaultSampleIndex))
                        defaultSampleIndex = 0;
                }
                else if(nameof(Mp3SamplePreset.DefaultBitRateIndex).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.TryGetInt32(out defaultBitRateIndex))
                        defaultBitRateIndex = 0;
                }
                else if ("Samples".Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var array = JsonSerializer.Deserialize<List<SamplePresetValue>>(ref reader, options);
                        if (array is not null)
                        {
                            samples = array;
                        }
                    }
                    catch
                    {

                    }
                }
                else if ("BitRates".Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var array = JsonSerializer.Deserialize<List<BitRatePresetValue>>(ref reader, options);
                        if (array is not null)
                        {
                            bitRates = array;
                        }
                    }
                    catch
                    {

                    }
                }
                else
                {
                    // 互換性のために不明なプロパティのエラーを throw しない

                    // throw new JsonException("Unknown property name");
                }
            }
            Mp3SamplePreset mp3SamplePreset = samples is null ? new Mp3SamplePreset() : new Mp3SamplePreset(samples, int.Clamp(defaultSampleIndex, 0, samples.Count - 1));
            if (bitRates is not null)
            {
                mp3SamplePreset.bitRates = bitRates;
                mp3SamplePreset.defaultBitRateIndex = int.Clamp(defaultBitRateIndex, 0, bitRates.Count - 1);
            }
            return mp3SamplePreset;
        }

        public override void Write(Utf8JsonWriter writer, Mp3SamplePreset value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber(nameof(SamplePreset.DefaultSampleIndex), value.DefaultSampleIndex);

            writer.WritePropertyName("Samples");
            JsonSerializer.Serialize(writer, value.Samples, options);

            writer.WriteNumber(nameof(Mp3SamplePreset.DefaultBitRateIndex), value.DefaultBitRateIndex);

            writer.WritePropertyName("BitRates");
            JsonSerializer.Serialize(writer, value.BitRates, options);

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// <see cref="SamplePresetValue"/> の Json コンバーター
    /// </summary>
    public class SamplePresetValueJsonConverter : JsonConverter<SamplePresetValue>
    {
        public override SamplePresetValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // object スコープに入っていることをチェック
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Invalid TokenType");
            }

            string? label = null;
            string? unit = null;
            int sample = 0;

            while (reader.Read())
            {
                // object スコープの末尾に達した場合
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                // reader の現在位置が PropertyName ではない場合は再び読み取る
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string propertyName = reader.GetString()!;

                if (!reader.Read())
                {
                    throw new JsonException("There is no value for the property.");
                }

                if (nameof(SamplePresetValue.Label).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    label = reader.GetString();
                }
                else if (nameof(SamplePresetValue.Unit).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    unit = reader.GetString();
                }
                else if (nameof(SamplePresetValue.Sample).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.TryGetInt32(out sample))
                        sample = 0;
                }
                else
                {
                    // 互換性のために不明なプロパティのエラーを throw しない

                    // throw new JsonException("Unknown property name");
                }
            }
            return new SamplePresetValue(sample, label, unit);
        }

        public override void Write(Utf8JsonWriter writer, SamplePresetValue value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(SamplePresetValue.Sample), value.Sample);
            if (value.Label is not null)
            {
                writer.WriteString(nameof(SamplePresetValue.Label), value.Label);
            }
            if (value.Unit is not null)
            {
                writer.WriteString(nameof(SamplePresetValue.Unit), value.Unit);
            }
            writer.WriteEndObject();
        }
    }

    public class BitRatePresetValueJsonConverter : JsonConverter<BitRatePresetValue>
    {
        public override BitRatePresetValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // object スコープに入っていることをチェック
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Invalid TokenType");
            }

            string? label = null;
            string? unit = null;
            int bitRate = 0;

            while (reader.Read())
            {
                // object スコープの末尾に達した場合
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                // reader の現在位置が PropertyName ではない場合は再び読み取る
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string propertyName = reader.GetString()!;

                if (!reader.Read())
                {
                    throw new JsonException("There is no value for the property.");
                }

                if (nameof(BitRatePresetValue.Label).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    label = reader.GetString();
                }
                else if (nameof(BitRatePresetValue.Unit).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    unit = reader.GetString();
                }
                else if (nameof(BitRatePresetValue.BitRate).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.TryGetInt32(out bitRate))
                        bitRate = 0;
                }
                else
                {
                    // 互換性のために不明なプロパティのエラーを throw しない

                    // throw new JsonException("Unknown property name");
                }
            }
            return new BitRatePresetValue(bitRate, label, unit);
        }

        public override void Write(Utf8JsonWriter writer, BitRatePresetValue value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(BitRatePresetValue.BitRate), value.BitRate);
            if (value.Label is not null)
            {
                writer.WriteString(nameof(BitRatePresetValue.Label), value.Label);
            }
            if (value.Unit is not null)
            {
                writer.WriteString(nameof(BitRatePresetValue.Unit), value.Unit);
            }
            writer.WriteEndObject();
        }
    }
}
