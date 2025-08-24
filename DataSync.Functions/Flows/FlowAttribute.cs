using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DataSync.Functions.Flows;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class FlowAttribute : Attribute
{
    public string Name { get; }
    public FlowAttribute(string name) => Name = name;
}

public interface IFlowRegistry { IFlowTask Resolve(string flowName); }

public sealed class FlowRegistry : IFlowRegistry
{
    private readonly IServiceProvider _sp;
    private readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);

    public FlowRegistry(IServiceProvider sp) => _sp = sp;

    internal void Register(string name, Type t) => _map[name] = t;

    public IFlowTask Resolve(string flowName)
    {
        if (!_map.TryGetValue(flowName, out var t))
            throw new InvalidOperationException($"Unknown flow '{flowName}'.");
        return (IFlowTask)_sp.GetRequiredService(t);
    }

    public static void RegisterAllFlows(IServiceCollection services)
    {
        var asm = Assembly.GetExecutingAssembly();
        var flows = asm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IFlowTask).IsAssignableFrom(t))
            .Select(t => new { Type = t, Attr = t.GetCustomAttribute<FlowAttribute>() })
            .Where(x => x.Attr is not null)
            .ToArray();

        services.AddSingleton<FlowRegistry>();
        foreach (var f in flows) services.AddSingleton(f.Type);

        services.AddSingleton<IFlowRegistry>(sp =>
        {
            var reg = sp.GetRequiredService<FlowRegistry>();
            foreach (var f in flows) reg.Register(f!.Attr!.Name, f.Type);
            return reg;
        });
    }
}