using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using DataSync.Functions.Flows;
using DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1;
using FluentAssertions;
using Xunit;

namespace Tests.Flows;

public class HomeDepot_BuilderPrimeFlowTests
{
    // We pass a dummy SecretClient because the flow doesn't use KV yet.
    // It won't make any KV calls during the test.
    private static SecretClient DummyKv() =>
        new SecretClient(new Uri("https://example.invalid"), new DefaultAzureCredential());

    [Fact]
    public async Task HomeDepot_BuilderPrimeFlowTest()
    {
        // arrange
        var flow = new HomeDepotToBuilderPrimeFlow();
        var kv = DummyKv();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(360));

        // act
        Func<Task> act = async () => await flow.RunAsync("tenant-test", kv, cts.Token);
        
        // assert
        await act.Should().NotThrowAsync(); // If the API call or parse fails, this would throw
    }
}