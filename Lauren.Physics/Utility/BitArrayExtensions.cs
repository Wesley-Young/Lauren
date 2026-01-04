using System.Buffers;
using System.Collections;
using System.Numerics;

namespace Lauren.Physics.Utility;

public static class BitArrayExtensions
{
    extension(BitArray bitArray)
    {
        /// <summary>
        ///     Calculate the weight (number of `1` bits) of the BitArray.
        /// </summary>
        public int Weight()
        {
            if (bitArray is null) throw new ArgumentNullException(nameof(bitArray));
            var length = bitArray.Length;
            if (length == 0) return 0;

            var ints = (length + 31) >> 5; // ceil(length / 32)
            var buf = ArrayPool<int>.Shared.Rent(ints);

            try
            {
                bitArray.CopyTo(buf, 0);

                var lastBits = length & 31; // length % 32
                var lastIndex = ints - 1;

                // Mask out unused bits in the last int
                if (lastBits != 0)
                {
                    var mask = (1u << lastBits) - 1u;
                    buf[lastIndex] = (int)((uint)buf[lastIndex] & mask);
                }

                var sum = 0;
                for (var i = 0; i < ints; i++) sum += BitOperations.PopCount((uint)buf[i]);
                return sum;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buf);
            }
        }

        /// <summary>
        ///     Calculate the weight of the bitwise OR of two BitArrays
        ///     without allocating a new BitArray or modifying the inputs.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Thrown when a and b have different lengths.
        /// </exception>
        public static int OrWeight(BitArray a, BitArray b)
        {
            var length = a.Length;
            if (length != b.Length) throw new ArgumentException("BitArrays must have the same Length.");
            if (length == 0) return 0;

            var ints = (length + 31) >> 5; // ceil(length / 32)

            var bufA = ArrayPool<int>.Shared.Rent(ints);
            var bufB = ArrayPool<int>.Shared.Rent(ints);

            try
            {
                a.CopyTo(bufA, 0);
                b.CopyTo(bufB, 0);

                var lastBits = length & 31; // length % 32
                var lastIndex = ints - 1;

                // Mask out unused bits in the last int, both for A and B
                if (lastBits != 0)
                {
                    var mask = (1u << lastBits) - 1u;
                    bufA[lastIndex] = (int)((uint)bufA[lastIndex] & mask);
                    bufB[lastIndex] = (int)((uint)bufB[lastIndex] & mask);
                }

                var sum = 0;
                for (var i = 0; i < ints; i++)
                {
                    var orBlock = (uint)bufA[i] | (uint)bufB[i];
                    sum += BitOperations.PopCount(orBlock);
                }

                return sum;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(bufA);
                ArrayPool<int>.Shared.Return(bufB);
            }
        }

        /// <summary>
        ///     Compare two BitArrays for value equality.
        /// </summary>
        public static bool ValueEquals(BitArray? a, BitArray? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;

            var length = a.Length;
            if (length != b.Length) return false;
            if (length == 0) return true;

            var ints = (length + 31) >> 5; // ceil(length / 32)

            var poolA = ArrayPool<int>.Shared.Rent(ints);
            var poolB = ArrayPool<int>.Shared.Rent(ints);

            try
            {
                a.CopyTo(poolA, 0);
                b.CopyTo(poolB, 0);

                var lastBits = length & 31; // length % 32
                var lastIndex = ints - 1;

                for (var i = 0; i < lastIndex; i++)
                    if (poolA[i] != poolB[i])
                        return false;

                if (lastBits == 0)
                {
                    return poolA[lastIndex] == poolB[lastIndex];
                }
                else
                {
                    var mask = (1u << lastBits) - 1u;
                    return ((uint)poolA[lastIndex] & mask) == ((uint)poolB[lastIndex] & mask);
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(poolA);
                ArrayPool<int>.Shared.Return(poolB);
            }
        }
    }
}