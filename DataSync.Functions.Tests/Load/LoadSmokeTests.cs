using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Azure.Security.KeyVault.Secrets;
using DataSync.Functions;
using DataSync.Functions.Flows;
using DataSync.Functions.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Load;

public class LoadSmokeTests
{
    [Xunit.Theory]
    // jobs, per-run simulated ms, max total ms
    [InlineData(200, 25, 10_000)]   // fast burst
    [InlineData(500, 10, 12_000)]   // larger burst
    public async Task RunFlow_HandleAsync_Completes_All_Jobs_Under_Threshold(
        int jobs, int perRunMs, int maxTotalMs)
    {
        // Arrange: set up a fake flow + registry and a handler
        var flow = new LatencyFlow(perRunMs);
        var registry = new SingleFlowRegistry("fake-load", flow);

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KeyVault:Uri"] = "https://example.invalid" // not used by fake flow
            }).Build();

        var handler = new RunFlow(registry, cfg, NullLogger<RunFlow>.Instance);

        // Pre-build all messages (base64-encoded FlowJob payloads)
        var base64Messages = Enumerable.Range(0, jobs)
            .Select(i =>
            {
                var job = new FlowJob($"tenant-{i % 7}", "fake-load");
                return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(job));
            })
            .ToArray();

        var sw = Stopwatch.StartNew();
        var errors = new ConcurrentBag<Exception>();

        // Act: fan-out with high concurrency, like the Queue trigger would
        await Parallel.ForEachAsync(base64Messages,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
            async (msg, ct) =>
            {
                try { await handler.HandleAsync(msg, ct); }
                catch (Exception ex) { errors.Add(ex); }
            });

        sw.Stop();

        // Assert
        errors.Should().BeEmpty("no handler invocation should throw");
        flow.InvocationCount.Should().Be(jobs, "every message must run exactly once");
        sw.ElapsedMilliseconds.Should().BeLessThan(maxTotalMs,
            $"load smoke should finish under {maxTotalMs} ms (actual {sw.ElapsedMilliseconds} ms).");
    }

    // ----- helpers -----

    /// <summary>Fake flow that simulates API latency with Task.Delay, thread-safe counter.</summary>
    private sealed class LatencyFlow : IFlowTask
    {
        private readonly int _delayMs;
        private int _count;

        public LatencyFlow(int delayMs) => _delayMs = delayMs;
        public int InvocationCount => _count;

        public async Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct)
        {
            // Simulate real work (HTTP + mapping) without network flakiness.
            await Task.Delay(_delayMs, ct);
            Interlocked.Increment(ref _count);
        }
    }

    /// <summary>Registry that maps one name to one task. Used to drive RunFlow directly.</summary>
    private sealed class SingleFlowRegistry : IFlowRegistry
    {
        private readonly string _name;
        private readonly IFlowTask _task;
        public SingleFlowRegistry(string name, IFlowTask task) { _name = name; _task = task; }

        public IFlowTask Resolve(string flowName)
            => flowName.Equals(_name, StringComparison.OrdinalIgnoreCase)
                ? _task
                : throw new InvalidOperationException("Unknown flow");
    }
}
