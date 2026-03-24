using System.Collections;
using Lauren.Physics.Operators;
using Lauren.Physics.Platforms;
using Xunit;

namespace Lauren.Physics.Tests;

public class FrameTests
{
    [Fact]
    public void Measure_NonHermitian_Throws()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 1);
        var nonHermitian = new PauliOperator(
            occupiedX: new BitArray(1) { [0] = true },
            occupiedZ: new BitArray(1) { [0] = true },
            coefficient: Coefficient.PlusOne);

        Assert.Throws<ArgumentException>(() => frame.Measure(nonHermitian, 1));
    }

    [Fact]
    public void Measure_InvalidReferenceValue_Throws()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Measure(PauliZ(1, 0), 0));
    }

    [Fact]
    public void X_IsNoOpOnFrame()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 1);

        frame.X(0);

        Assert.Equal(1, frame.Measure(PauliZ(1, 0), 1));
    }

    [Fact]
    public void XError_WithProbabilityOne_FlipsZReferenceMeasurement()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 1);

        frame.XError(0, 1);

        Assert.Equal(-1, frame.Measure(PauliZ(1, 0), 1));
    }

    [Fact]
    public void H_PropagatesXFrameIntoZFrame()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 1);
        frame.XError(0, 1);

        frame.H(0);

        Assert.Equal(-1, frame.Measure(PauliX(1, 0), 1));
    }

    [Fact]
    public void CX_PropagatesControlXErrorToTarget()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 2);
        frame.XError(0, 1);

        frame.CX(0, 1);

        Assert.Equal(-1, frame.Measure(PauliZ(2, 1), 1));
    }

    [Fact]
    public void Reset_ClearsXFrameComponent()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 1);
        frame.XError(0, 1);

        frame.Reset(0);

        Assert.Equal(1, frame.Measure(PauliZ(1, 0), 1));
    }

    [Fact]
    public void OutOfRangeAndInvalidProbability_Throw()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.H(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.CX(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.XError(0, -0.1));
    }

    private static PauliOperator PauliX(int count, int index)
    {
        var x = new BitArray(count);
        x[index] = true;
        return new PauliOperator(x, new BitArray(count), Coefficient.PlusOne);
    }

    private static PauliOperator PauliZ(int count, int index)
    {
        var z = new BitArray(count);
        z[index] = true;
        return new PauliOperator(new BitArray(count), z, Coefficient.PlusOne);
    }
}
