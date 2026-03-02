using System.Collections;
using Lauren.Physics.Platforms;

namespace Lauren.Physics.Utility;

public static class StabilizerExtensions
{
    extension(Stabilizer stabilizer)
    {
        public bool CommutesWith(Stabilizer other)
        {
            ValidateCompatible(stabilizer, other);

            bool parity = false;

            for (int i = 0; i < stabilizer.Qubits.Length; i += 2)
            {
                if (stabilizer.Qubits[i] && other.Qubits[i + 1]) parity = !parity;
                if (stabilizer.Qubits[i + 1] && other.Qubits[i]) parity = !parity;
            }

            bool stabilizerFermiWeightOdd = false;
            bool otherFermiWeightOdd = false;
            for (int i = 0; i < stabilizer.FermiSites.Length; i++)
            {
                if (stabilizer.FermiSites[i] && other.FermiSites[i]) parity = !parity;
                if (stabilizer.FermiSites[i]) stabilizerFermiWeightOdd = !stabilizerFermiWeightOdd;
                if (other.FermiSites[i]) otherFermiWeightOdd = !otherFermiWeightOdd;
            }

            if (stabilizerFermiWeightOdd && otherFermiWeightOdd) parity = !parity;
            return !parity;
        }

        public void MultiplyInPlace(Stabilizer source)
        {
            ValidateCompatible(stabilizer, source);

            var coefficient = stabilizer.Coefficient * source.Coefficient;
            if (stabilizer.FermiSites.ExchangeParityWith(source.FermiSites))
            {
                coefficient *= Coefficient.MinusOne;
            }

            stabilizer.Qubits.Xor(source.Qubits);
            stabilizer.FermiSites.Xor(source.FermiSites);
            stabilizer.Coefficient = coefficient;
        }

        public void OverwriteWith(Stabilizer source)
        {
            ValidateCompatible(stabilizer, source);

            stabilizer.Coefficient = source.Coefficient;
            stabilizer.Qubits.SetAll(false);
            stabilizer.Qubits.Or(source.Qubits);
            stabilizer.FermiSites.SetAll(false);
            stabilizer.FermiSites.Or(source.FermiSites);
        }

        public BitArray Flatten()
        {
            var flattened = new BitArray(stabilizer.FermiSites.Length + stabilizer.Qubits.Length);
            for (int i = 0; i < stabilizer.FermiSites.Length; i++)
            {
                flattened[i] = stabilizer.FermiSites[i];
            }

            for (int i = 0; i < stabilizer.Qubits.Length; i++)
            {
                flattened[stabilizer.FermiSites.Length + i] = stabilizer.Qubits[i];
            }

            return flattened;
        }
    }

    private static void ValidateCompatible(Stabilizer left, Stabilizer right)
    {
        if (left.Qubits.Length != right.Qubits.Length || left.FermiSites.Length != right.FermiSites.Length)
        {
            throw new ArgumentException("Stabilizers must have matching qubit and fermi-site dimensions.");
        }
    }
}
