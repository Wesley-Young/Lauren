using Lauren.Physics.Platforms;
using Xunit;

namespace Lauren.Physics.Tests;

public class CliffordPlatformInterfaceTests
{
    [Fact]
    public void Platform_CanBeDrivenThroughSharedCliffordInterface()
    {
        ICliffordPlatform platform = new Platform(pauliCount: 2);

        ApplySharedOperations(platform);

        Assert.Equal(2, platform.PauliCount);
    }

    [Fact]
    public void Frame_CanBeDrivenThroughSharedCliffordInterface()
    {
        var frame = new Frame();
        frame.Trap(pauliCount: 2);
        ICliffordPlatform platform = frame;

        ApplySharedOperations(platform);

        Assert.Equal(2, platform.PauliCount);
    }

    private static void ApplySharedOperations(ICliffordPlatform platform)
    {
        platform.X(0);
        platform.Y(1);
        platform.Z(0);
        platform.H(1);
        platform.S(0);
        platform.CX(0, 1);
        platform.XError(0, 0);
        platform.YError(1, 0);
        platform.ZError(0, 0);
        platform.Reset(1);
    }
}
