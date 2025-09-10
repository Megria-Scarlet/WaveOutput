using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MegriaCore.YMM4.WaveOutput
{
    public class OutputOption : INotifyPropertyChanged, IDisposable
    {
        protected bool disposedValue;
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => disposedValue;
        }

        protected int sampleIndex = -1;
        public int SampleIndex
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return sampleIndex;
            }
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                if (value != sampleIndex)
                {
                    sampleIndex = value;
                    NotifyPropertyChanged(nameof(SampleIndex));
                }
            }
        }
        protected IEnumerable<SamplePresetValue> samples;
        public IEnumerable<SamplePresetValue> Samples
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return samples;
            }
            protected internal set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                if (value != samples)
                {
                    samples = value;
                    NotifyPropertyChanged(nameof(Samples));
                }
            }
        }

        protected BitsAndChannel? bitsAndChannel = bitsAndChannels[1]; //new(16, 2);

        public BitsAndChannel? BitsAndChannel
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return bitsAndChannel;
            }
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                if (bitsAndChannel != value)
                {
                    bitsAndChannel = value;
                }
            }
        }

        #region NotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void NotifyPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(sender, args);
        }
        #endregion

        #region コンストラクタ
        public OutputOption() : this(SamplePreset.GetDefaultSamplePreset())
        {

        }
        public OutputOption(IEnumerable<SamplePresetValue> samples, int sampleIndex)
        {
            this.samples = samples;
            this.sampleIndex = sampleIndex;
        }
        public OutputOption(SamplePreset preset) : this(preset.Samples, preset.DefaultSampleIndex)
        {

        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                    PropertyChanged = null;
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~OutputOption()
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

        /// <summary>
        /// 全ての <see cref="PropertyChanged"/> ハンドラーを削除します。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ClearPropertyChanged()
        {
            this.PropertyChanged = null;
        }

        public virtual void ReloadSamplePresetFile()
        {

        }
        public virtual void OpenSamplePresetFile()
        {

        }

        private static readonly BitsAndChannel[] bitsAndChannels = [
            new BitsAndChannel(16, 1),
            new BitsAndChannel(16, 2),
            new BitsAndChannel(24, 1),
            new BitsAndChannel(24, 2),
            new BitsAndChannel(-1, 1),
            new BitsAndChannel(-1, 2),
            ];

        private static System.Collections.ObjectModel.ReadOnlyCollection<BitsAndChannel> bitsAndChannelsList = new(bitsAndChannels);
        public virtual IList<BitsAndChannel> BitsAndChannels { get => bitsAndChannelsList; }
    }

    public class StaticWaveOutputOption : OutputOption
    {
        public StaticWaveOutputOption() : base(Resource.waveSamplePreset)
        {

        }
        public override void OpenSamplePresetFile()
        {
            if (System.IO.File.Exists(Resource.wavePresetFilePath))
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", Resource.wavePresetFilePath);
            }
        }
        public override void ReloadSamplePresetFile()
        {
            Resource.ReloadSampleFile();

            var item = samples.ElementAtOrDefault(sampleIndex);

            var preset = Resource.waveSamplePreset;
            var values = preset.Samples;
            if (samples != values)
            {
                samples = values;
                NotifyPropertyChanged(nameof(Samples));
            }

            if (item is null)
            {
                sampleIndex = preset.DefaultSampleIndex;
            }
            else
            {
                int index = values.IndexOf(item);
                if (sampleIndex != index)
                {
                    sampleIndex = index;
                    NotifyPropertyChanged(nameof(SampleIndex));
                }
            }
        }
    }

    public class StaticMp3OutputOption : OutputOption
    {
        public StaticMp3OutputOption() : base(Resource.mp3SamplePreset)
        {
            var mp3Preset = Resource.mp3SamplePreset;
            List<BitsAndChannel> channels = new(mp3Preset.BitRates.Count * 2);

            foreach (var bitRate in mp3Preset.BitRates)
            {
                channels.Add(new Mp3BitsAndChannel(bitRate.BitRate, 1, bitRate.Label));
                channels.Add(new Mp3BitsAndChannel(bitRate.BitRate, 2, bitRate.Label));
            }
            bitsAndChannels = channels;
            bitsAndChannel = channels.ElementAtOrDefault(mp3Preset.DefaultBitRateIndex * 2 + 1);
        }
        public override void OpenSamplePresetFile()
        {
            if (System.IO.File.Exists(Resource.mp3PresetFilePath))
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", Resource.mp3PresetFilePath);
            }
        }
        public override void ReloadSamplePresetFile()
        {
            Resource.ReloadSampleFile();

            var item = samples.ElementAtOrDefault(sampleIndex);

            var preset = Resource.mp3SamplePreset;
            var values = preset.Samples;
            if (samples != values)
            {
                samples = values;
                NotifyPropertyChanged(nameof(Samples));
            }

            if (item is null)
            {
                sampleIndex = preset.DefaultSampleIndex;
            }
            else
            {
                int index = values.IndexOf(item);
                if (sampleIndex != index)
                {
                    sampleIndex = index;
                    NotifyPropertyChanged(nameof(SampleIndex));
                }
            }

            List<BitsAndChannel> channels = new(preset.BitRates.Count * 2);

            foreach (var bitRate in preset.BitRates)
            {
                channels.Add(new Mp3BitsAndChannel(bitRate.BitRate, 1, bitRate.Label));
                channels.Add(new Mp3BitsAndChannel(bitRate.BitRate, 2, bitRate.Label));
            }
            bitsAndChannels = channels;
            bitsAndChannel = channels.ElementAtOrDefault(preset.DefaultBitRateIndex * 2 + 1);
            NotifyPropertyChanged(nameof(BitsAndChannels));
        }

        private static readonly BitsAndChannel[] defaultBitsAndChannels = [
            new Mp3BitsAndChannel(128000, 1),
            new Mp3BitsAndChannel(128000, 2),
            new Mp3BitsAndChannel(192000, 1),
            new Mp3BitsAndChannel(192000, 2),
            new Mp3BitsAndChannel(256000, 1),
            new Mp3BitsAndChannel(256000, 2),
            ];

        private static System.Collections.ObjectModel.ReadOnlyCollection<BitsAndChannel> bitsAndChannelsList = new(defaultBitsAndChannels);

        protected IList<BitsAndChannel> bitsAndChannels;
        public override IList<BitsAndChannel> BitsAndChannels { get => bitsAndChannels; }
    }

    public class BitsAndChannel : IEquatable<BitsAndChannel>
    {
        protected int bits;
        protected int channel;

        public int Bits
        {
            get => bits;
            set => bits = value;
        }
        public int Channel
        {
            get => channel;
            set => channel = value;
        }

        public virtual string Label
        {
            get
            {
                if (bits == -1)
                {
                    return $"Float - {(channel == 2 ? "ステレオ" : "モノラル")}";
                }
                else
                {
                    return $"{bits} bit - {(channel == 2 ? "ステレオ" : "モノラル")}";
                }
            }
        }
        public BitsAndChannel() : this(16, 2) { }
        public BitsAndChannel(int bits, int channel)
        {
            this.bits = bits;
            this.channel = channel;
        }

        public override bool Equals(object? obj) => obj is BitsAndChannel other && Equals(other);

        public bool Equals(BitsAndChannel? other)
        {
            if (ReferenceEquals(other, this))
                return true;
            if (other is null || other.GetType() != typeof(BitsAndChannel))
                return false;
            return other.bits == this.bits && other.channel == channel;
        }

        public override int GetHashCode() => HashCode.Combine(bits, channel);

        public static bool operator ==(BitsAndChannel? left, BitsAndChannel? right)
        {
            return EqualityComparer<BitsAndChannel>.Default.Equals(left, right);
        }

        public static bool operator !=(BitsAndChannel? left, BitsAndChannel? right) => !(left == right);
    }
    public class Mp3BitsAndChannel : BitsAndChannel, IEquatable<BitsAndChannel>
    {
        protected string? label;
        public Mp3BitsAndChannel() : this(196000, 2) { }
        public Mp3BitsAndChannel(int bits, int channel) : base(bits, channel)
        {

        }
        public Mp3BitsAndChannel(int bits, int channel, string? label) : base(bits, channel)
        {
            this.label = label;
        }
        public override string Label
        {
            get
            {
                string c = channel == 2 ? "ステレオ" : "モノラル";
                if (label is not null)
                {
                    return $"{label} - {c}";
                }
                if (bits >= 1000)
                {
                    return $"{(bits / 1000):N0} kbps - {c}";
                }
                else
                {
                    return $"{bits:N0} bps - {c}";
                }
            }
        }
    }
}
