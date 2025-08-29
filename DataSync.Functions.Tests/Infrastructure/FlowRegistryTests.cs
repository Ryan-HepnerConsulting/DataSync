// DataSync.Functions.Tests/FlowRegistryTests.cs

using DataSync.Functions.Flows;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Infrastructure;

public class FlowRegistryTests
{
    [Fact]
    public void Resolves_Annotated_Flow_By_Name()
    {
        var sc = new ServiceCollection();
        FlowRegistry.RegisterAllFlows(sc);
        var sp = sc.BuildServiceProvider();
        var reg = sp.GetRequiredService<IFlowRegistry>();

        var flow = reg.Resolve("test"); // your TestWeatherFlow
        flow.Should().NotBeNull();
    }

    [Fact]
    public void Throws_On_Unknown_Flow()
    {
        var sc = new ServiceCollection();
        FlowRegistry.RegisterAllFlows(sc);
        var sp = sc.BuildServiceProvider();
        var reg = sp.GetRequiredService<IFlowRegistry>();

        Action act = () => reg.Resolve("not-a-flow");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown flow*");
    }
}