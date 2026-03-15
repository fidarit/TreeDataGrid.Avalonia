using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Avalonia.Controls.TreeDataGridBenchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly, DefaultConfig.Instance
            .AddJob(Job.Default.WithRuntime(ClrRuntime.Net48))
            .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0))
            .AddDiagnoser(MemoryDiagnoser.Default));
    }
}
