using System.Collections;

namespace Lauren.Physics.Utility;

internal static class CommutationUtility
{
    public static bool CommutesPauli(BitArray lhsQubits, BitArray rhsQubits)
    {
        if (lhsQubits.Length != rhsQubits.Length)
        {
            throw new ArgumentException("Operator dimensions do not match.");
        }

        bool parity = false;
        for (int i = 0; i < lhsQubits.Length; i += 2)
        {
            if (lhsQubits[i] && rhsQubits[i + 1]) parity = !parity;
            if (lhsQubits[i + 1] && rhsQubits[i]) parity = !parity;
        }

        return !parity;
    }
}
