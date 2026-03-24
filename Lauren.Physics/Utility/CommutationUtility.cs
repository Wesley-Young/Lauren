using System.Numerics;

namespace Lauren.Physics.Utility;

internal static class CommutationUtility
{
    private const ulong EvenBitMask = 0x5555555555555555UL;

    public static bool CommutesPauli(PackedBits lhsQubits, PackedBits rhsQubits)
    {
        if (lhsQubits.Length != rhsQubits.Length)
        {
            throw new ArgumentException("Operator dimensions do not match.");
        }

        int parity = 0;
        var lhsWords = lhsQubits.Words;
        var rhsWords = rhsQubits.Words;
        for (int i = 0; i < lhsWords.Length; i++)
        {
            ulong lhsEven = lhsWords[i] & EvenBitMask;
            ulong lhsOddShifted = (lhsWords[i] >> 1) & EvenBitMask;
            ulong rhsEven = rhsWords[i] & EvenBitMask;
            ulong rhsOddShifted = (rhsWords[i] >> 1) & EvenBitMask;

            parity ^= BitOperations.PopCount(lhsEven & rhsOddShifted);
            parity ^= BitOperations.PopCount(lhsOddShifted & rhsEven);
        }

        return (parity & 1) == 0;
    }
}