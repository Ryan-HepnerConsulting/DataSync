using DataSync.Functions.Flows;

namespace Tests.Common;

public sealed class FakeFlowRegistry : IFlowRegistry
{
    private readonly Dictionary<string, IFlowTask> _flows =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string name, IFlowTask flow) => _flows[name] = flow;

    public IFlowTask Resolve(string flowName)
    {
        if (!_flows.TryGetValue(flowName, out var flow))
            throw new InvalidOperationException($"Unknown flow '{flowName}' in FakeFlowRegistry.");
        return flow;
    }
}