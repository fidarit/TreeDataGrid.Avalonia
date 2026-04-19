using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace Avalonia.Controls.TreeDataGridBenchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly, DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default));
    }
}
