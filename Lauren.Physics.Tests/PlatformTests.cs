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
    public void Measure_SizeMismatch_Throws()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 1);

        var pauliWrongSize = new PauliOperator(
            occupiedX: new BitArray(2),
            occupiedZ: new BitArray(2),
            coefficient: Coefficient.PlusOne);
        var majoranaWrongSize = new MajoranaOperator(
            occupiedX: new BitArray(2),
            occupiedZ: new BitArray(2),
            coefficient: Coefficient.PlusOne);

        Assert.Throws<ArgumentException>(() => platform.Measure(pauliWrongSize));
        Assert.Throws<ArgumentException>(() => platform.Measure(majoranaWrongSize));
    }

    [Fact]
    public void Measure_ZOnFreshState_ReturnsPlusOne()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        int outcome = platform.Measure(PauliZ(1, 0));

        Assert.Equal(1, outcome);
    }

    [Fact]
    public void Measure_XThenMeasureXAgain_IsDeterministicAfterCollapse()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        int first = platform.Measure(PauliX(1, 0));
        int second = platform.Measure(PauliX(1, 0));

        Assert.True(first is 1 or -1);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Measure_MajoranaParityOnFreshState_ReturnsPlusOne()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);

        int outcome = platform.Measure(MajoranaParity(1, 0));

        Assert.Equal(1, outcome);
    }

    [Fact]
    public void Measure_GammaThenMeasureGammaAgain_IsDeterministicAfterCollapse()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);

        int first = platform.Measure(MajoranaGamma(1, 0));
        int second = platform.Measure(MajoranaGamma(1, 0));

        Assert.True(first is 1 or -1);
        Assert.Equal(first, second);
    }

    [Fact]
    public void X_OnFreshPauliQubit_FlipsZEigenvalue()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.X(0);

        Assert.Equal(-1, platform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void Y_OnFreshPauliQubit_FlipsZEigenvalue()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.Y(0);

        Assert.Equal(-1, platform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void Z_OnXStabilizer_FlipsXEigenvalue()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        platform.H(0);

        platform.Z(0);

        Assert.Equal(-1, platform.Measure(PauliX(1, 0)));
    }

    [Fact]
    public void H_OnZStabilizer_MapsToX()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.H(0);

        Assert.Equal(1, platform.Measure(PauliX(1, 0)));
    }

    [Fact]
    public void S_OnXStabilizer_MapsToY()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        platform.H(0);

        platform.S(0);

        Assert.Equal(1, platform.Measure(PauliY(1, 0)));
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

    [Fact]
    public void H_Twice_ReturnsToOriginalState()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.H(0);
        platform.H(0);

        Assert.Equal(1, platform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void S_FourTimes_ReturnsToOriginalState()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        platform.S(0);
        platform.S(0);
        platform.S(0);
        platform.S(0);

        Assert.Equal(1, platform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void PauliSingleQubitGates_DoNotChangeMajoranaParity()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 1);
        var parity = MajoranaParity(1, 0);

        int before = platform.Measure(parity);

        platform.X(0);
        platform.Y(0);
        platform.Z(0);
        platform.H(0);
        platform.S(0);

        int after = platform.Measure(parity);

        Assert.Equal(1, before);
        Assert.Equal(1, after);
    }

    private static PauliOperator PauliX(int count, int index)
    {
        var x = new BitArray(count);
        x[index] = true;
        return new PauliOperator(x, new BitArray(count), Coefficient.PlusOne);
    }

    private static PauliOperator PauliY(int count, int index)
    {
        var x = new BitArray(count);
        var z = new BitArray(count);
        x[index] = true;
        z[index] = true;
        return new PauliOperator(x, z, Coefficient.PlusI);
    }

    private static PauliOperator PauliZ(int count, int index)
    {
        var z = new BitArray(count);
        z[index] = true;
        return new PauliOperator(new BitArray(count), z, Coefficient.PlusOne);
    }

    private static MajoranaOperator MajoranaGamma(int count, int index)
    {
        var x = new BitArray(count);
        x[index] = true;
        return new MajoranaOperator(x, new BitArray(count), Coefficient.PlusOne);
    }

    private static MajoranaOperator MajoranaParity(int count, int index)
    {
        var x = new BitArray(count);
        var z = new BitArray(count);
        x[index] = true;
        z[index] = true;
        return new MajoranaOperator(x, z, Coefficient.PlusI);
    }
}
