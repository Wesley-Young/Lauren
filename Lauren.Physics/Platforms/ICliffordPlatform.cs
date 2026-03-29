// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Platforms;

public interface ICliffordPlatform
{
    int PauliCount { get; }

    void X(int qubitIndex);

    void Y(int qubitIndex);

    void Z(int qubitIndex);

    void H(int qubitIndex);

    void S(int qubitIndex);

    void CX(int controlIndex, int targetIndex);

    void XError(int qubitIndex, double probability);

    void YError(int qubitIndex, double probability);

    void ZError(int qubitIndex, double probability);

    void Reset(int qubitIndex);
}
