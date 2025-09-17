using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Vortice.DirectWrite;

namespace MegriaCore.YMM4.WaveOutput
{
    public class WaveFileWriterEx : Stream, IDisposable, ISampleFileWriter
    {
        protected Stream outStream;

        protected readonly BinaryWriter writer;

        protected long dataSizePos;

        protected long factSampleCountPos;

        protected long dataChunkSize;

        protected readonly WaveFormat format;

        protected readonly string filename;

        protected readonly byte[] value24 = new byte[3];

        public string Filename => filename;

        public override long Length => dataChunkSize;

        public TimeSpan TotalTime => TimeSpan.FromSeconds((double)Length / (double)WaveFormat.AverageBytesPerSecond);

        public WaveFormat WaveFormat => format;

        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override bool CanSeek => false;

        public override long Position
        {
            get
            {
                return dataChunkSize;
            }
            set
            {
                throw new InvalidOperationException("Repositioning a WaveFileWriter is not supported");
            }
        }

        protected bool isLeaveOpen;

        protected bool disposedValue;
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => disposedValue;
        }


        public WaveFileWriterEx(Stream outStream, WaveFormat format, bool leaveOpen = false)
        {
            this.outStream = outStream;
            this.format = format;
            this.isLeaveOpen = leaveOpen;

            writer = new BinaryWriter(outStream, Encoding.UTF8, leaveOpen);
            writer.Write("RIFF"u8);
            writer.Write(0);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            format.Serialize(writer);
            CreateFactChunk();
            WriteDataChunkHeader();

            this.filename = string.Empty;
        }

        public WaveFileWriterEx(string filename, WaveFormat format)
        : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format, false)
        {
            this.filename = filename;
        }
        private void WriteDataChunkHeader()
        {
            writer.Write("data"u8);
            dataSizePos = outStream.Position;
            writer.Write(0);
        }

        private void CreateFactChunk()
        {
            if (HasFactChunk())
            {
                writer.Write("fact"u8);
                writer.Write(4);
                factSampleCountPos = outStream.Position;
                writer.Write(0);
            }
        }

        private bool HasFactChunk()
        {
            if (format.Encoding != WaveFormatEncoding.Pcm)
            {
                return format.BitsPerSample != 0;
            }

            return false;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriter");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within a WaveFileWriter");
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of a WaveFileWriter");
        }
        public override void Write(byte[] data, int offset, int count)
        {
            if (outStream.Length + count > uint.MaxValue)
            {
                throw new ArgumentException("WAV file too large", nameof(count));
            }

            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (outStream.Length + buffer.Length > uint.MaxValue)
            {
                throw new ArgumentException("WAV file too large", nameof(buffer.Length));
            }

            outStream.Write(buffer);
            dataChunkSize += buffer.Length;
        }

        public virtual void WriteSample(float sample)
        {
            switch (WaveFormat.BitsPerSample)
            {
                case 16:
                    WriteSample16Bit(sample);
                    return;
                case 24:
                    WriteSample24Bit(sample);
                    return;
                case 32:
                    if (WaveFormat.Encoding == WaveFormatEncoding.Extensible)
                    {
                        WriteSample32Bit(sample);
                        return;
                    }
                    else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                    {
                        writer.Write(sample);
                        dataChunkSize += 4L;
                    }
                    throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        public virtual void WriteSamples(float[] samples, int offset, int count)
        {
            WriteSamples(samples.AsSpan(offset, count));
            /*
            for (int i = 0; i < count; i++)
            {
                WriteSample(samples[offset + i]);
            }
            */
        }

        public virtual void WriteSamples(ReadOnlySpan<float> samples)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                WriteSample(samples[i]);
            }
        }

        public override void Flush()
        {
            long position = writer.BaseStream.Position;
            UpdateHeader(writer);
            writer.BaseStream.Position = position;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (outStream is not null)
                    {
                        try
                        {
                            UpdateHeader(writer);
                        }
                        finally
                        {
                            writer.Dispose();

                            if (!isLeaveOpen)
                            {
                                outStream.Dispose();
                            }
                            outStream = null!;

                        }
                    }
                }
                disposedValue = true;
            }
        }

        protected virtual void UpdateHeader(BinaryWriter writer)
        {
            writer.Flush();
            UpdateRiffChunk(writer);
            UpdateFactChunk(writer);
            UpdateDataChunk(writer);
        }

        private void UpdateDataChunk(BinaryWriter writer)
        {
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write((uint)dataChunkSize);
        }

        private void UpdateRiffChunk(BinaryWriter writer)
        {
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((uint)(outStream.Length - 8));
        }

        private void UpdateFactChunk(BinaryWriter writer)
        {
            if (HasFactChunk())
            {
                int num = format.BitsPerSample * format.Channels;
                if (num != 0)
                {
                    writer.Seek((int)factSampleCountPos, SeekOrigin.Begin);
                    writer.Write((int)(dataChunkSize * 8 / num));
                }
            }
        }

        ~WaveFileWriterEx()
        {
            Dispose(disposing: false);
        }

        protected void WriteSample16Bit(float sample)
        {
            writer.Write((short)(32767f * sample));
            dataChunkSize += 2L;
        }
        protected void WriteSample24Bit(float sample)
        {
            Span<byte> stack = stackalloc byte[sizeof(int)];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(stack, (int)(2.14748365E+09f * sample));
            writer.Write(stack[1..]);
            /*
            byte[] bytes = BitConverter.GetBytes((int)(2.14748365E+09f * sample));
            value24[0] = bytes[1];
            value24[1] = bytes[2];
            value24[2] = bytes[3];
            writer.Write(value24);
            */
            dataChunkSize += 3L;
        }
        protected void WriteSample32Bit(float sample)
        {
            writer.Write(65535 * (int)sample);
            dataChunkSize += 4L;
        }
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        internal static void ThrowOutputFormatException(string formatType, string paramName)
        {
            throw new ArgumentException($"OutputFormat must be {formatType}", paramName);
        }

    }
    public class Wave16FileWriter : WaveFileWriterEx, ISampleFileWriter
    {
        public Wave16FileWriter(Stream outStream, WaveFormat format) : base(outStream, format)
        {
            if (format.BitsPerSample != 16)
            {
                ThrowOutputFormatException("16 bit", nameof(format));
            }
        }

        public Wave16FileWriter(string filename, WaveFormat format) : base(filename, format)
        {
            if (format.BitsPerSample != 16)
            {
                ThrowOutputFormatException("16 bit", nameof(format));
            }
        }

        public override void WriteSample(float sample)
        {
            WriteSample16Bit(sample);
        }

        public override void WriteSamples(float[] samples, int offset, int count)
        {
            WriteSamples(samples.AsSpan(offset, count));
        }

        public override unsafe void WriteSamples(ReadOnlySpan<float> samples)
        {
            BinaryWriter writer = this.writer;
            
            if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
            {
                int i = 0;
                var vectors = Helper.ToVector256(samples);

                var rate = System.Runtime.Intrinsics.Vector256.Create(32767f);

                const int count = 8;
                int* buf = stackalloc int[count];

                for (; i < vectors.Length; i++)
                {
                    {
                        var ptrFloat = (System.Runtime.Intrinsics.Vector256<float>*)buf;
                        *ptrFloat = vectors[i] * rate;
                        *(System.Runtime.Intrinsics.Vector256<int>*)buf = System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Int32(*ptrFloat);
                    }
                    foreach (ref var value in new Span<int>(buf, count))
                    {
                        writer.Write((short)value);
                    }
                }
                i *= count;
                samples = samples[i..];
                this.dataChunkSize += 2L * i;
            }
            else if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
            {
                int i = 0;
                var vectors = Helper.ToVector128(samples);

                var rate = System.Runtime.Intrinsics.Vector128.Create(32767f);

                const int count = 4;
                int* buf = stackalloc int[count];

                for (; i < vectors.Length; i++)
                {
                    {
                        var ptrFloat = (System.Runtime.Intrinsics.Vector128<float>*)buf;
                        *ptrFloat = vectors[i] * rate;
                        *(System.Runtime.Intrinsics.Vector128<int>*)buf = System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32(*ptrFloat);
                    }
                    foreach (ref var value in new Span<int>(buf, count))
                    {
                        writer.Write((short)value);
                    }
                }
                i *= count;
                samples = samples[i..];
                this.dataChunkSize += 2L * i;
            }
            foreach (var sample in samples)
            {
                WriteSample(sample);
            }
        }
    }
    public class Wave24FileWriter : WaveFileWriterEx, ISampleFileWriter
    {
        public Wave24FileWriter(Stream outStream, WaveFormat format) : base(outStream, format)
        {
            if (format.BitsPerSample != 24)
            {
                ThrowOutputFormatException("24 bit", nameof(format));
            }
        }

        public Wave24FileWriter(string filename, WaveFormat format) : base(filename, format)
        {
            if (format.BitsPerSample != 24)
            {
                ThrowOutputFormatException("24 bit", nameof(format));
            }
        }


        public override void WriteSample(float sample)
        {
            WriteSample24Bit(sample);
        }

        public override void WriteSamples(float[] samples, int offset, int count)
        {
            WriteSamples(samples.AsSpan(offset, count));
        }

        public override unsafe void WriteSamples(ReadOnlySpan<float> samples)
        {
            BinaryWriter writer = this.writer;

            if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
            {
                int i = 0;
                var vectors = Helper.ToVector256(samples);

                var rate = System.Runtime.Intrinsics.Vector256.Create(2.14748365E+09f);

                const int count = 8;
                byte* buf = stackalloc byte[count * sizeof(int)];

                for (; i < vectors.Length; i++)
                {
                    {
                        var ptrFloat = (System.Runtime.Intrinsics.Vector256<float>*)buf;
                        *ptrFloat = vectors[i] * rate;
                        *(System.Runtime.Intrinsics.Vector256<int>*)buf = System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Int32(*ptrFloat);
                    }
                    Write24Bit(writer, buf, count * sizeof(int));
                }
                i *= count;
                samples = samples[i..];
                this.dataChunkSize += 3L * i;
            }
            else if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
            {
                int i = 0;
                var vectors = Helper.ToVector128(samples);

                var rate = System.Runtime.Intrinsics.Vector128.Create(2.14748365E+09f);

                const int count = 4;
                byte* buf = stackalloc byte[count * sizeof(int)];

                for (; i < vectors.Length; i++)
                {
                    {
                        var ptrFloat = (System.Runtime.Intrinsics.Vector128<float>*)buf;
                        *ptrFloat = vectors[i] * rate;
                        *(System.Runtime.Intrinsics.Vector128<int>*)buf = System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32(*ptrFloat);
                    }
                    Write24Bit(writer, buf, count * sizeof(int));
                }
                i *= count;
                samples = samples[i..];
                this.dataChunkSize += 3L * i;
            }

            foreach (var sample in samples)
            {
                WriteSample(sample);
            }
        }
        private static unsafe void Write24Bit(BinaryWriter writer, byte* ptr, int byteLength)
        {
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < byteLength; i += 4)
                {
                    writer.Write(new ReadOnlySpan<byte>(ptr + (i + 1), 3));
                }
            }
            else
            {
                for (int i = 0; i < byteLength; i += 4)
                {
                    writer.Write(ptr[i + 2]);
                    writer.Write(ptr[i + 1]);
                    writer.Write(ptr[i + 0]);
                }
            }
        }
    }
    public class WaveFloatFileWriter : WaveFileWriterEx, ISampleFileWriter
    {
        public WaveFloatFileWriter(Stream outStream, WaveFormat format) : base(outStream, format)
        {
            if (format.BitsPerSample != 32 || format.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                ThrowOutputFormatException("IEEE float", nameof(format));
            }
        }

        public WaveFloatFileWriter(string filename, WaveFormat format) : base(filename, format)
        {
            if (format.BitsPerSample != 32 || format.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                ThrowOutputFormatException("IEEE float", nameof(format));
            }
        }

        public override void WriteSample(float sample)
        {
            writer.Write(sample);
            dataChunkSize += 4L;
        }

        public override void WriteSamples(float[] samples, int offset, int count)
        {
            WriteSamples(samples.AsSpan(offset, count));
        }

        public override unsafe void WriteSamples(ReadOnlySpan<float> samples)
        {
            if (BitConverter.IsLittleEndian)
            {
                var span = System.Runtime.InteropServices.MemoryMarshal.AsBytes(samples);
                Write(span);
                // dataChunkSize += span.Length;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[sizeof(float)];
                foreach (var sample in samples)
                {
                    System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(buffer, sample);
                    Write(buffer);
                }
                // dataChunkSize += (long)samples.Length * sizeof(float);
            }
        }
    }
    public interface ISampleFileWriter
    {
        public void WriteSample(float sample);
        public void WriteSamples(float[] samples, int offset, int count) => WriteSamples(samples.AsSpan(offset, count));
        public void WriteSamples(ReadOnlySpan<float> samples);
    }
}
