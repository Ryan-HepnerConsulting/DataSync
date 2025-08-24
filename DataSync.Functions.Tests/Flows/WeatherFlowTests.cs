using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using DataSync.Functions.Flows;
using FluentAssertions;
using Xunit;

namespace DataSync.Functions.Tests;

public class TestFlowTests
{
    // We pass a dummy SecretClient because the flow doesn't use KV.
    // It won't make any KV calls during the test.
    private static SecretClient DummyKv() =>
        new SecretClient(new Uri("https://example.invalid"), new DefaultAzureCredential());

    [Fact]
    public async Task TestFlow_RunsAndGetsTemperature()
    {
        // arrange
        var flow = new TestFlow();
        var kv = DummyKv();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // act
        Func<Task> act = async () => await flow.RunAsync("tenant-test", kv, cts.Token);
        
        // assert
        await act.Should().NotThrowAsync(); // If the API call or parse fails, this would throw
    }
}