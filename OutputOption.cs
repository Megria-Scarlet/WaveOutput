using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MegriaCore.YMM4.WaveOutput
{
    public class OutputOption : IEquatable<OutputOption>, INotifyPropertyChanged, IDisposable
    {
        private bool disposedValue;
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => disposedValue;
        }


        private int hertz = 44100;
        public int Hertz
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return hertz;
            }
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                if (value != hertz)
                {
                    hertz = value;
                    NotifyPropertyChanged(nameof(Hertz));
                }
            }
        }
        private int bits = 16;

        public int Bits
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return bits;
            }
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                if (value != bits)
                {
                    bits = value;
                    NotifyPropertyChanged(nameof(Bits));
                }
            }
        }

        private int channel = 2;

        public int Channel
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                return channel;
            }
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                if (value != channel)
                {
                    channel = value;
                    NotifyPropertyChanged(nameof(Channel));
                }
            }
        }
        private BitsAndChannel? bitsAndChannel = bitsAndChannels[2]; //new(16, 2);

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
                    NotifyPropertyChanged(nameof(Channel));
                    if (value is not null)
                    {
                        channel = value.Channel;
                        NotifyPropertyChanged(nameof(Channel));
                        bits = value.Bits;
                        NotifyPropertyChanged(nameof(Bits));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public OutputOption() { }

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

        public bool Equals(OutputOption? other)
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            if (ReferenceEquals(this, other))
                return true;
            if (other is null || other.GetType() != typeof(OutputOption))
                return false;
            return other.bits == bits && other.hertz == hertz;
        }
        /// <summary>
        /// 全ての <see cref="PropertyChanged"/> ハンドラーを削除します。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ClearPropertyChanged()
        {
            this.PropertyChanged = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => Equals(obj as OutputOption);

        public override int GetHashCode()
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            return HashCode.Combine(hertz, bits);
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
        public static System.Collections.ObjectModel.ReadOnlyCollection<BitsAndChannel> BitsAndChannels { get => bitsAndChannelsList; }
    }
    public class BitsAndChannel : IEquatable<BitsAndChannel>
    {
        private int bits;
        private int channel;

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

        public string Label
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
}
