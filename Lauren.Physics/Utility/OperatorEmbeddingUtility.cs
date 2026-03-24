using System.Collections;
using Lauren.Physics.Operators;

namespace Lauren.Physics.Utility;

internal readonly record struct EmbeddedQuantumOperator(
    Coefficient Coefficient,
    BitArray Qubits);

internal static class OperatorEmbeddingUtility
{
    public static EmbeddedQuantumOperator EmbedPauli(QuantumOperator op, int pauliCount)
    {
        return op switch
        {
            PauliOperator pauli when pauli.OccupiedX.Length == pauliCount => new EmbeddedQuantumOperator(
                pauli.Coefficient,
                pauli.ZippedOccupations()),
            PauliOperator => throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count.", nameof(op)),
            _ => throw new ArgumentException("Measurement operator must be a PauliOperator.", nameof(op))
        };
    }
}
