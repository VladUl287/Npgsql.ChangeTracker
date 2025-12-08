using BenchmarkDotNet.Running;
using Tracker.Benchmarks;

new ETagComparerBenchmark().Compare_Equal_PartialGenerate();

BenchmarkRunner.Run<ETagComparerBenchmark>();
