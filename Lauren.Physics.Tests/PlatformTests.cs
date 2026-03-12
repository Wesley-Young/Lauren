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
    public void U_OnMeasuredGamma_FlipsGammaEigenvalue()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);

        int first = platform.Measure(MajoranaGamma(1, 0));
        platform.U(0);
        int second = platform.Measure(MajoranaGamma(1, 0));

        Assert.Equal(-first, second);
    }

    [Fact]
    public void V_OnMeasuredGammaPrime_FlipsGammaPrimeEigenvalue()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);

        int first = platform.Measure(MajoranaGammaPrime(1, 0));
        platform.V(0);
        int second = platform.Measure(MajoranaGammaPrime(1, 0));

        Assert.Equal(-first, second);
    }

    [Fact]
    public void N_OnOddRemoteGamma_FlipsRemoteGammaEigenvalue()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 2);

        int first = platform.Measure(MajoranaGamma(2, 1));
        platform.N(0);
        int second = platform.Measure(MajoranaGamma(2, 1));

        Assert.Equal(-first, second);
    }

    [Fact]
    public void P_OnMeasuredGamma_MapsToGammaPrime()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);

        int first = platform.Measure(MajoranaGamma(1, 0));
        platform.P(0);
        int second = platform.Measure(MajoranaGammaPrime(1, 0));

        Assert.Equal(first, second);
    }

    [Fact]
    public void CX_OnBellPreparation_CreatesExpectedCorrelations()
    {
        var platform = new Platform(pauliCount: 2, majoranaCount: 0);

        platform.H(0);
        platform.CX(0, 1);

        Assert.Equal(1, platform.Measure(PauliOperatorOf(2, [0, 1], [] , Coefficient.PlusOne)));
        Assert.Equal(1, platform.Measure(PauliOperatorOf(2, [], [0, 1], Coefficient.PlusOne)));
    }

    [Fact]
    public void CNN_OnMeasuredGamma_MapsToExpectedMajoranaOperator()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 2);

        int first = platform.Measure(MajoranaGamma(2, 1));
        platform.CNN(0, 1);
        int second = platform.Measure(MajoranaOperatorOf(2, [0, 1], [0], Coefficient.PlusI));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Braid_OnMeasuredGamma_MapsToNegativeGammaPrime()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 2);

        int first = platform.Measure(MajoranaGamma(2, 1));
        platform.Braid(0, 1);
        int second = platform.Measure(MajoranaGammaPrime(2, 0));

        Assert.Equal(-first, second);
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
    public void SingleMajoranaGates_OutOfRange_Throws()
    {
        var platform = new Platform(pauliCount: 0, majoranaCount: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => platform.U(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.V(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.N(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.P(3));
    }

    [Fact]
    public void CompositeOperations_OutOfRange_Throws()
    {
        var pauliPlatform = new Platform(pauliCount: 2, majoranaCount: 0);
        var mixedPlatform = new Platform(pauliCount: 1, majoranaCount: 1);
        var majoranaPlatform = new Platform(pauliCount: 0, majoranaCount: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => pauliPlatform.CX(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => mixedPlatform.CNX(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => majoranaPlatform.CNN(0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => majoranaPlatform.Braid(2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => pauliPlatform.Reset(2));
    }

    [Fact]
    public void ErrorMethods_InvalidProbability_Throws()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => platform.XError(0, -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => platform.UError(0, 1.1));
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

    [Fact]
    public void ErrorMethods_WithProbabilityOne_ApplyUnderlyingGate()
    {
        var xPlatform = new Platform(pauliCount: 1, majoranaCount: 0);
        xPlatform.XError(0, 1);
        Assert.Equal(-1, xPlatform.Measure(PauliZ(1, 0)));

        var yPlatform = new Platform(pauliCount: 1, majoranaCount: 0);
        yPlatform.YError(0, 1);
        Assert.Equal(-1, yPlatform.Measure(PauliZ(1, 0)));

        var zPlatform = new Platform(pauliCount: 1, majoranaCount: 0);
        zPlatform.H(0);
        zPlatform.ZError(0, 1);
        Assert.Equal(-1, zPlatform.Measure(PauliX(1, 0)));

        var uPlatform = new Platform(pauliCount: 0, majoranaCount: 1);
        int gamma = uPlatform.Measure(MajoranaGamma(1, 0));
        uPlatform.UError(0, 1);
        Assert.Equal(-gamma, uPlatform.Measure(MajoranaGamma(1, 0)));

        var vPlatform = new Platform(pauliCount: 0, majoranaCount: 1);
        int gammaPrime = vPlatform.Measure(MajoranaGammaPrime(1, 0));
        vPlatform.VError(0, 1);
        Assert.Equal(-gammaPrime, vPlatform.Measure(MajoranaGammaPrime(1, 0)));

        var nPlatform = new Platform(pauliCount: 0, majoranaCount: 2);
        int remoteGamma = nPlatform.Measure(MajoranaGamma(2, 1));
        nPlatform.NError(0, 1);
        Assert.Equal(-remoteGamma, nPlatform.Measure(MajoranaGamma(2, 1)));
    }

    [Fact]
    public void Reset_OnMinusZState_RestoresPlusZ()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        platform.X(0);

        platform.Reset(0);

        Assert.Equal(1, platform.Measure(PauliZ(1, 0)));
    }

    [Fact]
    public void Detect_ReturnsEigenvalueWithoutCollapsing()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);
        platform.X(0);

        int? detected = platform.Detect(PauliZ(1, 0));
        int measured = platform.Measure(PauliZ(1, 0));

        Assert.Equal(-1, detected);
        Assert.Equal(-1, measured);
    }

    [Fact]
    public void Detect_OnNonStabilizer_ReturnsNull()
    {
        var platform = new Platform(pauliCount: 1, majoranaCount: 0);

        int? detected = platform.Detect(PauliX(1, 0));

        Assert.Null(detected);
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

    private static MajoranaOperator MajoranaGamma(int count, int index)
    {
        var x = new BitArray(count);
        x[index] = true;
        return new MajoranaOperator(x, new BitArray(count), Coefficient.PlusOne);
    }

    private static MajoranaOperator MajoranaGammaPrime(int count, int index)
    {
        var z = new BitArray(count);
        z[index] = true;
        return new MajoranaOperator(new BitArray(count), z, Coefficient.PlusOne);
    }

    private static MajoranaOperator MajoranaParity(int count, int index)
    {
        var x = new BitArray(count);
        var z = new BitArray(count);
        x[index] = true;
        z[index] = true;
        return new MajoranaOperator(x, z, Coefficient.PlusI);
    }

    private static MajoranaOperator MajoranaOperatorOf(int count, int[] xIndices, int[] zIndices, Coefficient coefficient)
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

        return new MajoranaOperator(x, z, coefficient);
    }
}
