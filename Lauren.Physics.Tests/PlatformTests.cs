using System.Collections;
using Lauren.Physics.Operators;
using Lauren.Physics.Platforms;
using Xunit;

namespace Lauren.Physics.Tests;

public class PlatformTests
{
    [Fact]
    public void Measure_NonHermitian_Throws()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        var nonHermitian = new PauliOperator(
            occupiedX: new BitArray(1) { [0] = true },
            occupiedZ: new BitArray(1) { [0] = true },
            coefficient: Coefficient.PlusOne);

        Assert.Throws<ArgumentException>(() => platform.Measure(nonHermitian));
    }

    [Fact]
    public void Measure_ZOnFreshState_ReturnsPlusOne()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        var z = new PauliOperator(
            occupiedX: new BitArray(1),
            occupiedZ: new BitArray(1) { [0] = true },
            coefficient: Coefficient.PlusOne);

        int outcome = platform.Measure(z);

        Assert.Equal(1, outcome);
        Assert.Equal(Coefficient.PlusOne, platform.PauliStabilizers[0].Coefficient);
        Assert.True(BitsEqual(platform.PauliStabilizers[0].Qubits, new BitArray(2) { [1] = true }));
    }

    [Fact]
    public void Measure_XThenMeasureXAgain_IsDeterministicAfterCollapse()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        var x = new PauliOperator(
            occupiedX: new BitArray(1) { [0] = true },
            occupiedZ: new BitArray(1),
            coefficient: Coefficient.PlusOne);

        int first = platform.Measure(x);
        int second = platform.Measure(x);

        Assert.True(first is 1 or -1);
        Assert.Equal(first, second);

        Assert.True(BitsEqual(platform.PauliStabilizers[0].Qubits, new BitArray(2) { [0] = true }));
        var expectedCoefficient = first == 1 ? Coefficient.PlusOne : Coefficient.MinusOne;
        Assert.Equal(expectedCoefficient, platform.PauliStabilizers[0].Coefficient);
    }

    [Fact]
    public void Measure_MajoranaOnFreshState_ReturnsPlusOne()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);
        var numberParity = new MajoranaOperator(
            occupiedX: new BitArray(1) { [0] = true },
            occupiedZ: new BitArray(1) { [0] = true },
            coefficient: Coefficient.PlusI);

        int outcome = platform.Measure(numberParity);

        Assert.Equal(1, outcome);
        Assert.Equal(Coefficient.PlusI, platform.MajoranaStabilizers[0].Coefficient);
        Assert.True(BitsEqual(platform.MajoranaStabilizers[0].FermiSites, new BitArray(2) { [0] = true, [1] = true }));
    }

    [Fact]
    public void Measure_GammaThenMeasureGammaAgain_IsDeterministicAfterCollapse()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);
        var gamma = new MajoranaOperator(
            occupiedX: new BitArray(1) { [0] = true },
            occupiedZ: new BitArray(1),
            coefficient: Coefficient.PlusOne);

        int first = platform.Measure(gamma);
        int second = platform.Measure(gamma);

        Assert.True(first is 1 or -1);
        Assert.Equal(first, second);
        Assert.True(BitsEqual(platform.MajoranaStabilizers[0].FermiSites, new BitArray(2) { [0] = true }));
        var expectedCoefficient = first == 1 ? Coefficient.PlusOne : Coefficient.MinusOne;
        Assert.Equal(expectedCoefficient, platform.MajoranaStabilizers[0].Coefficient);
    }

    [Fact]
    public void X_OnFreshPauliQubit_FlipsStabilizerSign()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.X(0);

        Assert.Equal(Coefficient.MinusOne, platform.PauliStabilizers[0].Coefficient);
        Assert.True(BitsEqual(platform.PauliStabilizers[0].Qubits, new BitArray(2) { [1] = true }));
    }

    [Fact]
    public void Y_OnFreshPauliQubit_FlipsStabilizerSign()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.Y(0);

        Assert.Equal(Coefficient.MinusOne, platform.PauliStabilizers[0].Coefficient);
        Assert.True(BitsEqual(platform.PauliStabilizers[0].Qubits, new BitArray(2) { [1] = true }));
    }

    [Fact]
    public void Z_OnXStabilizer_FlipsStabilizerSign()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        platform.PauliStabilizers[0].Qubits[0] = true;
        platform.PauliStabilizers[0].Qubits[1] = false;

        platform.Z(0);

        Assert.Equal(Coefficient.MinusOne, platform.PauliStabilizers[0].Coefficient);
        Assert.True(BitsEqual(platform.PauliStabilizers[0].Qubits, new BitArray(2) { [0] = true }));
    }

    [Fact]
    public void H_OnZStabilizer_MapsToX()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.H(0);

        Assert.Equal(Coefficient.PlusOne, platform.PauliStabilizers[0].Coefficient);
        Assert.True(BitsEqual(platform.PauliStabilizers[0].Qubits, new BitArray(2) { [0] = true }));
    }

    [Fact]
    public void S_OnXStabilizer_MapsToXZAndAddsIPhase()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        platform.PauliStabilizers[0].Qubits[0] = true;
        platform.PauliStabilizers[0].Qubits[1] = false;

        platform.S(0);

        Assert.Equal(Coefficient.PlusI, platform.PauliStabilizers[0].Coefficient);
        Assert.True(BitsEqual(platform.PauliStabilizers[0].Qubits, new BitArray(2) { [0] = true, [1] = true }));
    }

    [Fact]
    public void SingleQubitGates_OutOfRange_Throws()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => platform.X(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.Y(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.Z(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.H(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.S(4));
    }

    private static bool BitsEqual(BitArray left, BitArray right)
    {
        if (left.Length != right.Length) return false;

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }
}
