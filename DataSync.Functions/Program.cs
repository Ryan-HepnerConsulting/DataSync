using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Azure.Storage.Queues;
using DataSync.Functions.Flows;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var cfg = builder.Configuration;
var logFactory = LoggerFactory.Create(b => b.AddConsole());
var startupLog = logFactory.CreateLogger("Startup");

// Cosmos
var cosmosConn = cfg["Cosmos:ConnectionString"];
var cosmosDb   = cfg["Cosmos:Database"];
var cosmosCtr  = cfg["Cosmos:Container"];

if (string.IsNullOrWhiteSpace(cosmosConn) ||
    string.IsNullOrWhiteSpace(cosmosDb)   ||
    string.IsNullOrWhiteSpace(cosmosCtr))
{
    startupLog.LogError("Missing Cosmos settings. Check Cosmos:ConnectionString/Database/Container in local.settings.json.");
    throw new InvalidOperationException("Cosmos settings missing.");
}

/*
var cosmosClient = new CosmosClient(cosmosConn);
var container = cosmosClient.GetDatabase(cosmosDb).GetContainer(cosmosCtr);
builder.Services.AddSingleton(container);

// Queue
var storageConn = cfg["AzureWebJobsStorage"];          // <-- no "Values:" prefix
var queueName   = cfg["Queue:Name"] ?? "flow-jobs";

if (string.IsNullOrWhiteSpace(storageConn))
{
    startupLog.LogError("AzureWebJobsStorage missing.");
    throw new InvalidOperationException("AzureWebJobsStorage missing.");
}

builder.Services.AddSingleton(new QueueClient(storageConn, queueName));
*/

// Flow registry
builder.Services.AddSingleton<IFlowRegistry, FlowRegistry>();
FlowRegistry.RegisterAllFlows(builder.Services);

builder.Build().Run();