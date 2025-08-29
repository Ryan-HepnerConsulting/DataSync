// DataSync.Functions.Tests/RunFlowHandlerTests.cs

using System.Text.Json;
using Azure.Security.KeyVault.Secrets;
using DataSync.Functions;
using DataSync.Functions.Flows;
using DataSync.Functions.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Infrastructure;

public class RunFlowHandlerTests
{
    [Fact]
    public async Task Calls_The_Correct_Flow_With_TenantId()
    {
        var called = false;
        var tenantSeen = "";

        // Fake flow
        var flow = new TestFlow(() => { called = true; }, t => tenantSeen = t);

        // Registry
        var sc = new ServiceCollection();
        sc.AddSingleton<IFlowRegistry>(new SingleFlowRegistry("fake-flow", flow));
        sc.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["KeyVault:Uri"] = "https://example.invalid" }).Build());

        var sp = sc.BuildServiceProvider();
        var handler = new RunFlow(sp.GetRequiredService<IFlowRegistry>(),
            sp.GetRequiredService<IConfiguration>(),
            NullLogger<RunFlow>.Instance);

        var job = new FlowJob("tenant-xyz", "fake-flow");
        var base64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(job));

        await handler.HandleAsync(base64, default);

        called.Should().BeTrue();
        tenantSeen.Should().Be("tenant-xyz");
    }

    private sealed class TestFlow : IFlowTask
    {
        private readonly Action _onRun;
        private readonly Action<string> _onTenant;
        public TestFlow(Action onRun, Action<string> onTenant) { _onRun = onRun; _onTenant = onTenant; }

        public Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct)
        { _onTenant(tenantId); _onRun(); return Task.CompletedTask; }
    }

    private sealed class SingleFlowRegistry : IFlowRegistry
    {
        private readonly string _name; private readonly IFlowTask _task;
        public SingleFlowRegistry(string name, IFlowTask task) { _name = name; _task = task; }
        public IFlowTask Resolve(string flowName) =>
            flowName.Equals(_name, StringComparison.OrdinalIgnoreCase) ? _task
                : throw new InvalidOperationException("Unknown");
    }
}