// ReSharper disable InconsistentNaming

using System.Collections;
using Lauren.Physics;
using Lauren.Physics.Operators;
using Xunit;

namespace Lauren.Circuit.Tests;

public class CircuitTests
{
    [Fact]
    public void Trap_InitializesPauliCount_AndCannotBeRepeated()
    {
        var circuit = new Circuit();

        circuit.Trap(pauliCount: 2);

        Assert.Equal(2, circuit.PauliCount);
        Assert.Single(circuit.Instructions);
        Assert.Equal(CircuitInstructionKind.Trap, circuit.Instructions[0].Kind);
        Assert.Throws<InvalidOperationException>(() => circuit.Trap(pauliCount: 1));
    }

    [Fact]
    public void OperationsBeforeTrap_Throw()
    {
        var circuit = new Circuit();

        Assert.Throws<InvalidOperationException>(() => circuit.X(0));
        Assert.Throws<InvalidOperationException>(() => circuit.CX(0, 1));
        Assert.Throws<InvalidOperationException>(() => circuit.MZ(0));
        Assert.Throws<InvalidOperationException>(() => circuit.MPP(PauliZ(1, 0)));
        Assert.Throws<InvalidOperationException>(() => circuit.Depolarize1(0, 0.1));
    }

    [Fact]
    public void MZ_NormalizesIntoMPPAndOptionalMeasurementError()
    {
        var circuit = new Circuit();
        circuit.Trap(pauliCount: 2);

        circuit.MZ(qubitIndex: 1, measurementErrorProbability: 0.125);

        Assert.Equal(2, circuit.Instructions.Count);
        Assert.Equal(CircuitInstructionKind.MZ, circuit.Instructions[1].Kind);
        Assert.Equal(0.125, circuit.Instructions[1].Probability);
        Assert.Equal(new[] { 1 }, circuit.Instructions[1].Qubits);

        Assert.Equal(3, circuit.NormalizedInstructions.Count);
        Assert.Equal(CircuitInstructionKind.Trap, circuit.NormalizedInstructions[0].Kind);
        Assert.Equal(CircuitInstructionKind.MPP, circuit.NormalizedInstructions[1].Kind);
        Assert.Equal(PauliZ(2, 1), circuit.NormalizedInstructions[1].PauliTarget);
        Assert.Equal(CircuitInstructionKind.MeasurementError, circuit.NormalizedInstructions[2].Kind);

        Assert.Equal(new[] { 1 }, circuit.MeasurementInstructionIndices);
        Assert.Equal(new[] { 2 }, circuit.NoiseInstructionIndices);
    }

    [Fact]
    public void MPP_RecordsMeasurementAndNoiseInstructionIndices()
    {
        var circuit = new Circuit();
        circuit.Trap(pauliCount: 2);

        circuit.MPP(PauliZ(2, 0), measurementErrorProbability: 0.2);

        Assert.Equal(3, circuit.NormalizedInstructions.Count);
        Assert.Equal(CircuitInstructionKind.MPP, circuit.NormalizedInstructions[1].Kind);
        Assert.Equal(CircuitInstructionKind.MeasurementError, circuit.NormalizedInstructions[2].Kind);
        Assert.Equal(new[] { 1 }, circuit.MeasurementInstructionIndices);
        Assert.Equal(new[] { 2 }, circuit.NoiseInstructionIndices);
    }

    [Fact]
    public void Depolarize1_ExpandsIntoThreePauliErrors()
    {
        var circuit = new Circuit();
        circuit.Trap(pauliCount: 1);

        circuit.Depolarize1(qubitIndex: 0, probability: 0.3);
        double expectedComponentProbability = (1d - Math.Sqrt(1d - 4d * 0.3d / 3d)) / 2d;

        Assert.Equal(2, circuit.Instructions.Count);
        Assert.Equal(CircuitInstructionKind.Depolarize1, circuit.Instructions[1].Kind);

        Assert.Equal(4, circuit.NormalizedInstructions.Count);
        Assert.All(circuit.NormalizedInstructions.Skip(1), instruction =>
        {
            Assert.Equal(CircuitInstructionKind.PauliError, instruction.Kind);
            Assert.Equal(NoiseComponentKind.Depolarize1Component, instruction.NoiseKind);
            Assert.Equal(expectedComponentProbability, instruction.Probability, precision: 12);
            Assert.NotNull(instruction.NoisePauli);
        });
        Assert.Equal(new[] { 1, 2, 3 }, circuit.NoiseInstructionIndices);
    }

    [Fact]
    public void Depolarize2_ExpandsIntoFifteenPauliErrors()
    {
        var circuit = new Circuit();
        circuit.Trap(pauliCount: 2);

        circuit.Depolarize2(firstQubitIndex: 0, secondQubitIndex: 1, probability: 0.4);
        double expectedComponentProbability = 0.5d * (1d - Math.Pow(1d - 16d * 0.4d / 15d, 1d / 8d));

        Assert.Equal(2, circuit.Instructions.Count);
        Assert.Equal(CircuitInstructionKind.Depolarize2, circuit.Instructions[1].Kind);

        Assert.Equal(16, circuit.NormalizedInstructions.Count);
        Assert.All(circuit.NormalizedInstructions.Skip(1), instruction =>
        {
            Assert.Equal(CircuitInstructionKind.PauliError, instruction.Kind);
            Assert.Equal(NoiseComponentKind.Depolarize2Component, instruction.NoiseKind);
            Assert.Equal(expectedComponentProbability, instruction.Probability, precision: 12);
            Assert.NotNull(instruction.NoisePauli);
        });
        Assert.Equal(Enumerable.Range(1, 15), circuit.NoiseInstructionIndices);
    }

    [Fact]
    public void Detector_NegativeReferencesResolveAgainstMeasurementOrder()
    {
        var circuit = new Circuit();
        circuit.Trap(pauliCount: 2);
        circuit.MZ(0);
        circuit.MZ(1);

        circuit.Detector(-1, -2);

        MeasurementParity detector = Assert.Single(circuit.Detectors);
        Assert.Equal(new[] { 1, 0 }, detector.MeasurementIndices);
        Assert.False(detector.Negated);
    }

    [Fact]
    public void ObservableInclude_NegativeReferencesResolveAgainstMeasurementOrder()
    {
        var circuit = new Circuit();
        circuit.Trap(pauliCount: 2);
        circuit.MZ(0);
        circuit.MZ(1);

        circuit.ObservableInclude(-2, -1);

        MeasurementParity observable = Assert.Single(circuit.Observables);
        Assert.Equal(new[] { 0, 1 }, observable.MeasurementIndices);
        Assert.False(observable.Negated);
    }

    [Fact]
    public void AppendingInstruction_InvalidatesCaches()
    {
        var circuit = new Circuit();
        circuit.Trap(pauliCount: 1);
        circuit.SetCachedReferenceMeasurements([1]);
        circuit.SetCachedPrototype([new PrototypeEntry(NoiseComponentKind.OneError, [], [])]);

        circuit.Tick();

        Assert.Null(circuit.CachedReferenceMeasurements);
        Assert.Null(circuit.CachedPrototype);
    }

    private static PauliOperator PauliZ(int count, int index)
    {
        var z = new BitArray(count);
        z[index] = true;
        return new PauliOperator(new BitArray(count), z, Coefficient.PlusOne);
    }
}
