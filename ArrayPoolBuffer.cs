using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MegriaCore.Buffers
{
    /// <summary>
    /// <see cref="System.Buffers.ArrayPool{T}"/> を利用した <typeparamref name="T"/> 型のバッファーを管理する構造体。
    /// </summary>
    /// <typeparam name="T">任意の型。</typeparam>
    public ref struct ArrayPoolBuffer<T> : IDisposable, System.Buffers.IMemoryOwner<T> , IEquatable<ArrayPoolBuffer<T>>
    {
        /// <summary>
        /// バッファーサイズを指定して、新しい <see cref="ArrayPoolBuffer{T}"/> 型のオブジェクトを作成します。
        /// </summary>
        /// <param name="capacity">バッファーサイズ。</param>
        public ArrayPoolBuffer(int capacity) : this(System.Buffers.ArrayPool<T>.Shared, capacity) { }
        /// <summary>
        /// 内部で使用する <see cref="System.Buffers.ArrayPool{T}"/> 型のオブジェクトとバッファーサイズを指定して、新しい
        /// <see cref="ArrayPoolBuffer{T}"/> 型のオブジェクトを作成します。
        /// </summary>
        /// <param name="arrayPool">内部で使用する <see cref="System.Buffers.ArrayPool{T}"/> 型のオブジェクト。</param>
        /// <param name="capacity">バッファーサイズ。</param>
        public ArrayPoolBuffer(System.Buffers.ArrayPool<T> arrayPool, int capacity)
        {
            this.arrayPool = arrayPool;
            this.array = arrayPool.Rent(capacity);
            this.buffer = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(this.array), capacity);
        }


        private System.Buffers.ArrayPool<T> arrayPool;
        /// <summary>
        /// このオブジェクトが保持する <see cref="System.Buffers.ArrayPool{T}"/> 型のオブジェクトを取得します。
        /// </summary>
        /// <returns>
        /// このオブジェクトが保持する <see cref="System.Buffers.ArrayPool{T}"/> 型のオブジェクト。<br></br>
        /// このオブジェクトが破棄されている場合は <see langword="null"/> を返します。
        /// </returns>
        public readonly System.Buffers.ArrayPool<T> ArrayPool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => arrayPool;
        }
        private Span<T> buffer;
        /// <summary>
        /// バッファーを取得します。
        /// </summary>
        /// <returns>
        /// バッファーとして利用可能な <see cref="Span{T}"/> 。<br></br>
        /// このオブジェクトを破棄した場合は、この <see cref="Span{T}"/> を使用してはいけません。
        /// </returns>
        public readonly Span<T> Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => buffer;
        }
        private T[] array;
        /// <summary>
        /// バッファーとして利用する <typeparamref name="T"/> 型の配列を取得します。
        /// </summary>
        /// <returns>
        /// バッファーとして利用可能な <typeparamref name="T"/> 型の配列。<br></br>
        /// このオブジェクトを破棄した場合は、この配列を使用してはいけません。
        /// </returns>
        public readonly T[] Array
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => array;
        }

        /// <summary>
        /// このオブジェクトを破棄します。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            arrayPool.Return(array);
        }

        /// <summary>
        /// バッファーサイズを取得します。
        /// </summary>
        /// <returns>バッファーサイズを示す 32 ビット符号付き整数。</returns>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => buffer.Length;
        }
        /// <summary>
        /// 利用可能な内部容量を取得します。
        /// </summary>
        /// <returns>内部容量を示す 32 ビット符号付き整数。</returns>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => array.Length;
        }

        /// <summary>
        /// バッファーを示す <see cref="Memory{T}"/> 型のオブジェクトを取得します。
        /// </summary>
        /// <returns>
        /// バッファーとして利用可能な <see cref="Memory{T}"/> 型のオブジェクト。<br></br>
        /// このオブジェクトを破棄した場合は、この <see cref="Memory{T}"/> を使用してはいけません。
        /// </returns>
        public readonly Memory<T> Memory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Memory<T>(array, 0, Length);
        }

        /// <summary>
        /// バッファーの設定位置を示す参照を取得します。
        /// </summary>
        /// <returns>バッファーの設定位置を示す <typeparamref name="T"/> 型のオブジェクトの参照。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T GetReference() => ref MemoryMarshal.GetReference(this.buffer);
        /// <summary>
        /// バッファーの 0 番目の要素への参照を返します。バッファーが空の場合は、 <see langword="null"/> 参照を返します。<br></br>
        /// これはピン留めに使用でき、 <see langword="fixed"/> ステートメント内で <see cref="ArrayPoolBuffer{T}"/> を使用するために必要です。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public readonly ref T GetPinnableReference() => ref GetReference();

#pragma warning disable CS0809 // 旧形式のメンバーが、旧形式でないメンバーをオーバーライドします
        /// <summary>
        /// This method is not supported as <see cref="ArrayPoolBuffer{T}"/> cannot be boxed.
        /// To compare two <see cref="ArrayPoolBuffer{T}"/> , use <see langword="operator"/>==.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        [Obsolete("Equals() on ArrayPoolBuffer will always throw an exception. Use the equality operator instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override readonly bool Equals(object? obj)
        {
            throw new NotSupportedException();
        }
#pragma warning restore CS0809 // 旧形式のメンバーが、旧形式でないメンバーをオーバーライドします
        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(scoped ArrayPoolBuffer<T> other)
        {
            return ReferenceEquals(array, other.array) &&
                   buffer == other.buffer &&
                   EqualityComparer<System.Buffers.ArrayPool<T>>.Default.Equals(arrayPool, other.arrayPool);
        }
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode() => HashCode.Combine(arrayPool, buffer.Length, array);

        /// <summary>
        /// バッファーを示す <see cref="Span{T}"/> に変換します。
        /// </summary>
        /// <remarks>
        /// <paramref name="arrayPoolBuffer"/> が破棄されている場合は変換してはいけません。
        /// </remarks>
        /// <param name="arrayPoolBuffer"></param>
        /// <returns>
        /// <paramref name="arrayPoolBuffer"/> のバッファーを示す <see cref="Span{T}"/> 。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(ArrayPoolBuffer<T> arrayPoolBuffer) => arrayPoolBuffer.buffer;

#pragma warning disable CS1591 // 公開されている型またはメンバーの XML コメントがありません
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(scoped ArrayPoolBuffer<T> left, scoped ArrayPoolBuffer<T> right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(scoped ArrayPoolBuffer<T> left, scoped ArrayPoolBuffer<T> right) => !left.Equals(right);
#pragma warning restore CS1591 // 公開されている型またはメンバーの XML コメントがありません
    }
}
