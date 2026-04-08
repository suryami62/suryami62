#region

using suryami62.Tests.Benchmarks;

#endregion

// Entry point untuk menjalankan benchmarks
Console.WriteLine("=== suryami62 Benchmark Runner ===\n");

if (args.Length == 0 || args[0] == "--all")
{
    Console.WriteLine("Running all benchmarks...\n");
    BenchmarkRunner.RunAll();
}
else if (args[0] == "--caching")
{
    Console.WriteLine("Running caching benchmarks...\n");
    BenchmarkRunner.RunCachingBenchmarks();
}
else if (args[0] == "--image")
{
    Console.WriteLine("Running image processing benchmarks...\n");
    BenchmarkRunner.RunImageBenchmarks();
}
else
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- [options]");
    Console.WriteLine("");
    Console.WriteLine("Options:");
    Console.WriteLine("  --all      Run all benchmarks (default)");
    Console.WriteLine("  --caching  Run caching benchmarks only");
    Console.WriteLine("  --image    Run image processing benchmarks only");
}