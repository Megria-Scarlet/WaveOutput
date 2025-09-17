using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MegriaCore.YMM4.WaveOutput
{
    public static class Helper
    {
        /// <summary>
        /// <typeparamref name="TFrom"/> 型の配列の先頭の参照を、 <typeparamref name="TTo"/> 型に再解釈して取得します。
        /// <code>
        /// <see langword="return"/> <see langword="ref"/> <see cref="Unsafe"/>.As(<see langword="ref"/> <see cref="MemoryMarshal"/>.GetArrayDataReference(<paramref name="array"/>));
        /// </code>
        /// </summary>
        /// <typeparam name="TFrom">配列の要素の型。</typeparam>
        /// <typeparam name="TTo">取得する参照の型。</typeparam>
        /// <param name="array">先頭の参照を取得する配列。</param>
        /// <returns><paramref name="array"/> の先頭を示す <typeparamref name="TTo"/> 型の参照。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo GetReference<TFrom, TTo>(TFrom[] array)
        {
            return ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetArrayDataReference(array));
        }

        internal static void CheckIndex(int sourceLength, int start, int length)
        {
            if (System.Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)sourceLength)
                    throw new ArgumentOutOfRangeException();
            }
            else
            {
                if ((uint)start > (uint)sourceLength || (uint)length > (uint)(sourceLength - start))
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Span<float> ToFloatSpan(byte[] array, int start, int length)
        {
            CheckIndex(array.Length, start, length);
            ref byte _reference = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */);
            return MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref _reference), length >> 2);
        }

        public static Span<float> ToFloatSpan(Span<byte> span)
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref MemoryMarshal.GetReference(span)), span.Length >> 2);
        }

        public static Span<byte> ToByteSpan(float[] array, int start, int length)
        {
            CheckIndex(array.Length, start, length);
            ref float _reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */);
            return MemoryMarshal.CreateSpan(ref Unsafe.As<float, byte>(ref _reference), length << 2);
        }

        #region ToVector256
        public static Span<System.Runtime.Intrinsics.Vector256<T>> ToVector256<T>(Span<T> span)
            where T : unmanaged
        {
            return Cast<T, System.Runtime.Intrinsics.Vector256<T>>(span, span.Length / System.Runtime.Intrinsics.Vector256<T>.Count);
        }
        public static Span<System.Runtime.Intrinsics.Vector256<float>> ToVector256(Span<float> span)
        {
            return Cast<float, System.Runtime.Intrinsics.Vector256<float>>(span, span.Length >> 3);
        }
        public static Span<System.Runtime.Intrinsics.Vector256<T>> ToVector256<T>(ReadOnlySpan<T> span)
            where T : unmanaged
        {
            return Cast<T, System.Runtime.Intrinsics.Vector256<T>>(span, span.Length / System.Runtime.Intrinsics.Vector256<T>.Count);
        }
        public static Span<System.Runtime.Intrinsics.Vector256<float>> ToVector256(ReadOnlySpan<float> span)
        {
            return Cast<float, System.Runtime.Intrinsics.Vector256<float>>(span, span.Length >> 3);
        }
        #endregion
        #region ToVector128
        public static Span<System.Runtime.Intrinsics.Vector128<T>> ToVector128<T>(Span<T> span)
            where T : unmanaged
        {
            return Cast<T, System.Runtime.Intrinsics.Vector128<T>>(span, span.Length / System.Runtime.Intrinsics.Vector128<T>.Count);
        }
        public static Span<System.Runtime.Intrinsics.Vector128<float>> ToVector128(Span<float> span)
        {
            return Cast<float, System.Runtime.Intrinsics.Vector128<float>>(span, span.Length >> 2);
        }
        public static Span<System.Runtime.Intrinsics.Vector128<T>> ToVector128<T>(ReadOnlySpan<T> span)
            where T : unmanaged
        {
            return Cast<T, System.Runtime.Intrinsics.Vector128<T>>(span, span.Length / System.Runtime.Intrinsics.Vector128<T>.Count);
        }
        public static Span<System.Runtime.Intrinsics.Vector128<float>> ToVector128(ReadOnlySpan<float> span)
        {
            return Cast<float, System.Runtime.Intrinsics.Vector128<float>>(span, span.Length >> 3);
        }
        #endregion

        #region CreateSpan
        /// <summary>
        /// 指定した配列の先頭から始まる、 <paramref name="length"/> の長さを持つ <typeparamref name="TTo"/> 型のスパンを作成します。
        /// </summary>
        /// <typeparam name="TFrom">配列の型。</typeparam>
        /// <typeparam name="TTo">スパンの要素の型。</typeparam>
        /// <param name="array">スパンの元となる <typeparamref name="TFrom"/> 型の配列。</param>
        /// <param name="length">新しいスパンの長さ。</param>
        /// <returns>指定した配列の先頭から指定した長さを示す新しいスパン。</returns>
        public static Span<TTo> CreateSpan<TFrom, TTo>(TFrom[] array, int length)
        {
            ref TFrom reference = ref MemoryMarshal.GetArrayDataReference(array);
            return CreateSpan<TFrom, TTo>(ref reference, length);
        }
        /// <summary>
        /// 指定した参照の位置から始まる、 <paramref name="length"/> の長さを持つ <typeparamref name="TTo"/> 型のスパンを作成します。
        /// </summary>
        /// <typeparam name="TFrom">参照の型。</typeparam>
        /// <typeparam name="TTo">スパンの要素の型。</typeparam>
        /// <param name="reference">スパンの先頭の位置となる参照。</param>
        /// <param name="length">新しいスパンの長さ。</param>
        /// <returns>指定した参照の位置から指定した長さを示す新しいスパン。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> CreateSpan<TFrom, TTo>(ref TFrom reference, int length)
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref reference), length);
        }
        #endregion

        #region Cast<TFrom, TTo>
        private static Span<TTo> Cast<TFrom, TTo>(Span<TFrom> span, int length)
        {
            if (length == 0)
            {
                return [];
            }
            else
            {
                ref TFrom reference = ref MemoryMarshal.GetReference(span);
                return CreateSpan<TFrom, TTo>(ref reference, length);
            }
        }
        private static Span<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> span, int length)
        {
            if (length == 0)
            {
                return [];
            }
            else
            {
                ref TFrom reference = ref MemoryMarshal.GetReference(span);
                return CreateSpan<TFrom, TTo>(ref reference, length);
            }
        }
        #endregion


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
    }
}
