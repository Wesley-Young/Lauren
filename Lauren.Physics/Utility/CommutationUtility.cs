using System.Collections;

namespace Lauren.Physics.Utility;

internal static class CommutationUtility
{
    public static bool Commutes(BitArray lhsQubits, BitArray lhsFermiSites, BitArray rhsQubits, BitArray rhsFermiSites)
    {
        if (lhsQubits.Length != rhsQubits.Length || lhsFermiSites.Length != rhsFermiSites.Length)
        {
            throw new ArgumentException("Operator dimensions do not match.");
        }

        bool parity = false;
        for (int i = 0; i < lhsQubits.Length; i += 2)
        {
            if (lhsQubits[i] && rhsQubits[i + 1]) parity = !parity;
            if (lhsQubits[i + 1] && rhsQubits[i]) parity = !parity;
        }

        bool lhsFermiWeightOdd = false;
        bool rhsFermiWeightOdd = false;
        for (int i = 0; i < lhsFermiSites.Length; i++)
        {
            if (lhsFermiSites[i] && rhsFermiSites[i]) parity = !parity;
            if (lhsFermiSites[i]) lhsFermiWeightOdd = !lhsFermiWeightOdd;
            if (rhsFermiSites[i]) rhsFermiWeightOdd = !rhsFermiWeightOdd;
        }

        if (lhsFermiWeightOdd && rhsFermiWeightOdd) parity = !parity;
        return !parity;
    }
}
