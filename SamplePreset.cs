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
        protected int defaultIndex;

        protected List<SamplePresetValue> presetValues;

        public SamplePreset()
        {
            defaultIndex = 0;
            presetValues = [];
        }

        public SamplePreset(IEnumerable<SamplePresetValue> presetValues, int defaultIndex = 0)
        {
            this.presetValues = new(presetValues);
            this.defaultIndex = defaultIndex;
        }

        public SamplePreset(List<SamplePresetValue> presetValues, int defaultIndex = 0)
        {
            this.presetValues = presetValues;
            this.defaultIndex = defaultIndex;
        }

        public int DefaultIndex
        {
            get => defaultIndex;
            set => defaultIndex = value;
        }

        public List<SamplePresetValue> PresetValues
        {
            get => presetValues;
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

    /// <summary>
    /// サンプリング数とラベル名を管理するクラス
    /// </summary>
    [JsonConverter(typeof(SamplePresetValueJsonConverter))]
    public class SamplePresetValue : IEquatable<SamplePresetValue>
    {
        #region フィールド変数
        /// <summary>
        /// ラベル名
        /// </summary>
        protected string? label;
        /// <summary>
        /// サンプリング数
        /// </summary>
        protected int sample;
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
        public virtual string ActualLabel
        {
            get
            {
                return label is null ? sample.ToString("N0") : label;
            }
        }
        public string? Unit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unit;
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
            if (other is null || other.GetType() != typeof(SamplePresetValue))
                return false;
            return other.label == this.label && other.sample == this.sample;
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

                if (nameof(SamplePreset.DefaultIndex).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!reader.TryGetInt32(out defaultIndex))
                        defaultIndex = 0;
                }
                else if (nameof(SamplePreset.PresetValues).Equals(propertyName, StringComparison.OrdinalIgnoreCase))
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
            writer.WriteNumber(nameof(SamplePreset.DefaultIndex), value.DefaultIndex);

            writer.WritePropertyName(nameof(SamplePreset.PresetValues));
            JsonSerializer.Serialize(writer, value.PresetValues, options);


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
}
