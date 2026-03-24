using BenchmarkDotNet.Running;
using Lauren.Physics.Benchmarks;

if (args.Length == 1 && args[0] == "--manual-compare-trysolve")
{
    ManualTrySolvePauliSpanComparison.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
