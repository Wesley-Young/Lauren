using System.Collections;
using Lauren.Physics.Operators;

namespace Lauren.Physics.Utility;

internal readonly record struct EmbeddedQuantumOperator(
    Coefficient Coefficient,
    BitArray Qubits,
    BitArray FermiSites);

internal static class OperatorEmbeddingUtility
{
    public static EmbeddedQuantumOperator Embed(QuantumOperator op, int pauliCount, int majoranaCount)
    {
        return op switch
        {
            PauliOperator pauli when pauli.OccupiedX.Length == pauliCount => new EmbeddedQuantumOperator(
                pauli.Coefficient,
                pauli.ZippedOccupations(),
                new BitArray(2 * majoranaCount)),
            PauliOperator => throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count.", nameof(op)),
            MajoranaOperator majorana when majorana.OccupiedX.Length == majoranaCount => new EmbeddedQuantumOperator(
                majorana.Coefficient,
                new BitArray(2 * pauliCount),
                majorana.ZippedOccupations()),
            MajoranaOperator => throw new ArgumentException("Majorana operator size does not match platform Majorana site count.", nameof(op)),
            _ => throw new ArgumentException(
                "Measurement operator must be either PauliOperator or MajoranaOperator.",
                nameof(op))
        };
    }
}
