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
        var platform = new Platform(pauliCount: 1);
        var nonHermitian = new PauliOperator(
            occupiedX: new BitArray(1) { [0] = true },
            occupiedZ: new BitArray(1) { [0] = true },
            coefficient: Coefficient.PlusOne);

        Assert.Throws<ArgumentException>(() => platform.Measure(nonHermitian));
    }

    [Fact]
    public void Measure_SizeMismatch_Throws()
    {
        var platform = new Platform(pauliCount: 1);
        var pauliWrongSize = new PauliOperator(
            occupiedX: new BitArray(2),
            occupiedZ: new BitArray(2),
            coefficient: Coefficient.PlusOne);

        Assert.Throws<ArgumentException>(() => platform.Measure(pauliWrongSize));
    }

    [Fact]
    public void Measure_ZOnFreshState_ReturnsPlusOne()
    {
        var platform = new Platform(pauliCount: 1);

        Assert.Equal(1, platform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void Measure_XThenMeasureXAgain_IsDeterministicAfterCollapse()
    {
        var platform = new Platform(pauliCount: 1);

        int first = platform.Measure(PauliX(1, 0));
        int second = platform.Measure(PauliX(1, 0));

        Assert.True(first is 1 or -1);
        Assert.Equal(first, second);
    }

    [Fact]
    public void CX_OnBellPreparation_CreatesExpectedCorrelations()
    {
        var platform = new Platform(pauliCount: 2);

        platform.H(0);
        platform.CX(0, 1);

        Assert.Equal(1, platform.Measure(PauliOperatorOf(2, [0, 1], [], Coefficient.PlusOne)));
        Assert.Equal(1, platform.Measure(PauliOperatorOf(2, [], [0, 1], Coefficient.PlusOne)));
    }

    [Fact]
    public void SingleQubitGates_ProduceExpectedEffects()
    {
        var xPlatform = new Platform(pauliCount: 1);
        xPlatform.X(0);
        Assert.Equal(-1, xPlatform.Measure(PauliZ(1, 0)));

        var yPlatform = new Platform(pauliCount: 1);
        yPlatform.Y(0);
        Assert.Equal(-1, yPlatform.Measure(PauliZ(1, 0)));

        var zPlatform = new Platform(pauliCount: 1);
        zPlatform.H(0);
        zPlatform.Z(0);
        Assert.Equal(-1, zPlatform.Measure(PauliX(1, 0)));
    }

    [Fact]
    public void CliffordGates_ProduceExpectedEffects()
    {
        var hPlatform = new Platform(pauliCount: 1);
        hPlatform.H(0);
        Assert.Equal(1, hPlatform.Measure(PauliX(1, 0)));

        var sPlatform = new Platform(pauliCount: 1);
        sPlatform.H(0);
        sPlatform.S(0);
        Assert.Equal(1, sPlatform.Measure(PauliY(1, 0)));
    }

    [Fact]
    public void OutOfRangeOperations_Throw()
    {
        var platform = new Platform(pauliCount: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => platform.X(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.H(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.CX(0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.Reset(2));
    }

    [Fact]
    public void ErrorMethods_InvalidProbability_Throws()
    {
        var platform = new Platform(pauliCount: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => platform.XError(0, -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.YError(0, 1.1));
    }

    [Fact]
    public void CliffordCycles_ReturnToOriginalState()
    {
        var hPlatform = new Platform(pauliCount: 1);
        hPlatform.H(0);
        hPlatform.H(0);
        Assert.Equal(1, hPlatform.Measure(PauliZ(1, 0)));

        var sPlatform = new Platform(pauliCount: 1);
        sPlatform.S(0);
        sPlatform.S(0);
        sPlatform.S(0);
        sPlatform.S(0);
        Assert.Equal(1, sPlatform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void ErrorMethods_WithProbabilityOne_ApplyUnderlyingGate()
    {
        var xPlatform = new Platform(pauliCount: 1);
        xPlatform.XError(0, 1);
        Assert.Equal(-1, xPlatform.Measure(PauliZ(1, 0)));

        var yPlatform = new Platform(pauliCount: 1);
        yPlatform.YError(0, 1);
        Assert.Equal(-1, yPlatform.Measure(PauliZ(1, 0)));

        var zPlatform = new Platform(pauliCount: 1);
        zPlatform.H(0);
        zPlatform.ZError(0, 1);
        Assert.Equal(-1, zPlatform.Measure(PauliX(1, 0)));
    }

    [Fact]
    public void Reset_OnMinusZState_RestoresPlusZ()
    {
        var platform = new Platform(pauliCount: 1);
        platform.X(0);

        platform.Reset(0);

        Assert.Equal(1, platform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void Detect_ReturnsEigenvalueWithoutCollapsing()
    {
        var platform = new Platform(pauliCount: 1);
        platform.X(0);

        int? detected = platform.Detect(PauliZ(1, 0));
        int measured = platform.Measure(PauliZ(1, 0));

        Assert.Equal(-1, detected);
        Assert.Equal(-1, measured);
    }

    [Fact]
    public void Detect_OnNonStabilizer_ReturnsNull()
    {
        var platform = new Platform(pauliCount: 1);

        Assert.Null(platform.Detect(PauliX(1, 0)));
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

    private static PauliOperator PauliOperatorOf(int count, int[] xIndices, int[] zIndices, Coefficient coefficient)
    {
        var x = new BitArray(count);
        foreach (int index in xIndices)
        {
            x[index] = true;
        }

        var z = new BitArray(count);
        foreach (int index in zIndices)
        {
            z[index] = true;
        }

        return new PauliOperator(x, z, coefficient);
    }
}
