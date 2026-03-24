using System.Buffers;
using System.Collections;
using System.Numerics;

namespace Lauren.Physics.Utility;

internal sealed class PackedBits : IEquatable<PackedBits>
{
    private readonly ulong[] _words;

    public PackedBits(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        Length = length;
        _words = new ulong[GetWordCount(length)];
    }

    public PackedBits(BitArray bits)
    {
        ArgumentNullException.ThrowIfNull(bits);

        Length = bits.Length;
        _words = new ulong[GetWordCount(Length)];
        if (Length == 0)
        {
            return;
        }

        int intCount = (Length + 31) >> 5;
        int[] buffer = ArrayPool<int>.Shared.Rent(intCount);

        try
        {
            bits.CopyTo(buffer, 0);
            for (int i = 0; i < _words.Length; i++)
            {
                uint lower = (uint)buffer[i * 2];
                uint upper = (i * 2) + 1 < intCount
                    ? (uint)buffer[(i * 2) + 1]
                    : 0u;
                _words[i] = lower | ((ulong)upper << 32);
            }

            MaskUnusedBits();
        }
        finally
        {
            ArrayPool<int>.Shared.Return(buffer);
        }
    }

    private PackedBits(ulong[] words, int length)
    {
        Length = length;
        _words = words;
        MaskUnusedBits();
    }

    public int Length { get; }

    public bool this[int index]
    {
        get
        {
            ValidateIndex(index);
            int wordIndex = index >> 6;
            int bitIndex = index & 63;
            return ((_words[wordIndex] >> bitIndex) & 1UL) != 0;
        }
        set
        {
            ValidateIndex(index);
            int wordIndex = index >> 6;
            int bitIndex = index & 63;
            ulong mask = 1UL << bitIndex;
            if (value)
            {
                _words[wordIndex] |= mask;
            }
            else
            {
                _words[wordIndex] &= ~mask;
            }
        }
    }

    public PackedBits Clone() => new((ulong[])_words.Clone(), Length);

    public BitArray ToBitArray()
    {
        if (Length == 0)
        {
            return new BitArray(0);
        }

        int[] ints = new int[(Length + 31) >> 5];
        for (int i = 0; i < _words.Length; i++)
        {
            ints[i * 2] = (int)_words[i];
            if ((i * 2) + 1 < ints.Length)
            {
                ints[(i * 2) + 1] = (int)(_words[i] >> 32);
            }
        }

        var bitArray = new BitArray(ints)
        {
            Length = Length
        };
        return bitArray;
    }

    public void XorInPlace(PackedBits other)
    {
        ValidateCompatible(other);
        for (int i = 0; i < _words.Length; i++)
        {
            _words[i] ^= other._words[i];
        }

        MaskUnusedBits();
    }

    public void OrInPlace(PackedBits other)
    {
        ValidateCompatible(other);
        for (int i = 0; i < _words.Length; i++)
        {
            _words[i] |= other._words[i];
        }

        MaskUnusedBits();
    }

    public void Clear()
    {
        Array.Clear(_words);
    }

    public int Weight()
    {
        int sum = 0;
        foreach (ulong word in _words)
        {
            sum += BitOperations.PopCount(word);
        }

        return sum;
    }

    public bool Equals(PackedBits? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Length != other.Length)
        {
            return false;
        }

        for (int i = 0; i < _words.Length; i++)
        {
            if (_words[i] != other._words[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is PackedBits other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Length);
        foreach (ulong word in _words)
        {
            hash.Add(word);
        }

        return hash.ToHashCode();
    }

    public static int AndWeight(PackedBits left, PackedBits right)
    {
        left.ValidateCompatible(right);

        int sum = 0;
        for (int i = 0; i < left._words.Length; i++)
        {
            sum += BitOperations.PopCount(left._words[i] & right._words[i]);
        }

        return sum;
    }

    public static int OrWeight(PackedBits left, PackedBits right)
    {
        left.ValidateCompatible(right);

        int sum = 0;
        for (int i = 0; i < left._words.Length; i++)
        {
            sum += BitOperations.PopCount(left._words[i] | right._words[i]);
        }

        return sum;
    }

    public static PackedBits ZipPauli(PackedBits occupiedX, PackedBits occupiedZ)
    {
        occupiedX.ValidateCompatible(occupiedZ);

        var zipped = new PackedBits(occupiedX.Length * 2);
        for (int i = 0; i < occupiedX.Length; i++)
        {
            zipped[i * 2] = occupiedX[i];
            zipped[(i * 2) + 1] = occupiedZ[i];
        }

        return zipped;
    }

    internal ReadOnlySpan<ulong> Words => _words;

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void ValidateCompatible(PackedBits other)
    {
        if (Length != other.Length)
        {
            throw new ArgumentException("Bit collections must have the same length.", nameof(other));
        }
    }

    private void MaskUnusedBits()
    {
        int lastBits = Length & 63;
        if (lastBits == 0 || _words.Length == 0)
        {
            return;
        }

        ulong mask = (1UL << lastBits) - 1UL;
        _words[^1] &= mask;
    }

    private static int GetWordCount(int bitCount) => (bitCount + 63) >> 6;
}