// ReSharper disable InconsistentNaming

using System.Collections.Immutable;
using Lauren.Physics.Operators;

namespace Lauren.Circuit;

public sealed record CircuitInstruction(
    CircuitInstructionKind Kind,
    ImmutableArray<int> Qubits,
    PauliOperator? PauliTarget = null,
    double Probability = 0.0,
    NoiseComponentKind? NoiseKind = null,
    PauliOperator? NoisePauli = null)
{
    public static CircuitInstruction Create(CircuitInstructionKind kind, params int[] qubits) =>
        new(kind, ImmutableArray.Create(qubits));

    public static CircuitInstruction Create(
        CircuitInstructionKind kind,
        double probability,
        params int[] qubits) =>
        new(kind, ImmutableArray.Create(qubits), Probability: probability);

    public static CircuitInstruction CreateMPP(PauliOperator target, double probability = 0.0) =>
        new(CircuitInstructionKind.MPP, ImmutableArray<int>.Empty, target, probability);

    public static CircuitInstruction CreatePauliError(
        PauliOperator noisePauli,
        double probability,
        NoiseComponentKind noiseKind) =>
        new(
            CircuitInstructionKind.PauliError,
            ImmutableArray<int>.Empty,
            Probability: probability,
            NoiseKind: noiseKind,
            NoisePauli: noisePauli);
}
