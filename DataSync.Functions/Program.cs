using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Azure.Storage.Queues;
using DataSync.Functions.Flows;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var cfg = builder.Configuration;

// Cosmos
var cosmos = new CosmosClient(cfg["Cosmos:ConnectionString"]);
var container = cosmos.GetDatabase(cfg["Cosmos:Database"])
    .GetContainer(cfg["Cosmos:Container"]);
builder.Services.AddSingleton(container);

// Storage Queue (used by orchestrator to enqueue)
builder.Services.AddSingleton(new QueueClient(
    cfg["Values:AzureWebJobsStorage"], cfg["Queue:Name"]!));

// Flow registry + auto discovery of flows in /Flows folder
builder.Services.AddSingleton<IFlowRegistry, FlowRegistry>();
FlowRegistry.RegisterAllFlows(builder.Services);

builder.Build().Run();