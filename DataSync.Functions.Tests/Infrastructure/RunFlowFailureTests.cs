// DataSync.Functions.Tests/RunFlowFailureTests.cs

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

public class RunFlowFailureTests
{
    [Fact]
    public async Task Propagates_Exception_For_Retry()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFlowRegistry>(new SingleFlowRegistry("boom", new BoomFlow()));
        sc.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string,string?> { ["KeyVault:Uri"] = "https://example.invalid" }).Build());

        var handler = new RunFlow(sc.BuildServiceProvider().GetRequiredService<IFlowRegistry>(),
            sc.BuildServiceProvider().GetRequiredService<IConfiguration>(),
            NullLogger<RunFlow>.Instance);

        var job = new FlowJob("tenant-x", "boom");
        var base64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(job));

        Func<Task> act = async () => await handler.HandleAsync(base64, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Boom*");
    }

    private sealed class BoomFlow : IFlowTask
    {
        public Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct)
            => throw new InvalidOperationException("Boom");
    }

    private sealed class SingleFlowRegistry : IFlowRegistry
    {
        private readonly string _name; private readonly IFlowTask _task;
        public SingleFlowRegistry(string name, IFlowTask task) { _name = name; _task = task; }
        public IFlowTask Resolve(string flowName) => _name == flowName ? _task : throw new InvalidOperationException("Unknown");
    }
}