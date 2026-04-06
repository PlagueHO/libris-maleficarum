using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

var builder = DistributedApplication.CreateBuilder(args);

// Helper method to find an available port starting from a base port
static int FindAvailablePort(int startPort)
{
    var port = startPort;
    while (port < 65535)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            return port;
        }
        catch (SocketException)
        {
            // Port is in use, try next one
            port++;
        }
    }
    throw new InvalidOperationException($"No available ports found starting from {startPort}");
}

// Configure Azure Cosmos DB Emulator for local development
//
// CRITICAL: These ports MUST remain fixed at 8081 and 1234.
// 
// The Cosmos DB emulator container internally hardcodes these ports in the connection metadata
// it returns to clients, regardless of what port the container is published on. For example:
// - If the container publishes on random port 54321, the emulator still tells clients "connect to me on port 8081"
// - This causes connection failures because the SDK tries port 8081 but nothing is listening there
// 
// Solution: Disable port randomization in tests (AppHostFixture.cs uses "DcpPublisher:RandomizePorts=false")
// so that Aspire publishes the container on these exact ports, matching what the emulator advertises.
//
// DO NOT change these values or enable port randomization without updating the emulator's internal configuration.
var cosmosDbGatewayPort = 8081;
var cosmosDbDataExplorerPort = 1234;

// Add Azure Cosmos DB Linux-based Emulator (preview) for local development
// Configure emulator using documented environment variables
// See: https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux#docker-commands
#pragma warning disable ASPIRECOSMOSDB001 // Suppress experimental diagnostic for preview emulator
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithGatewayPort(cosmosDbGatewayPort);
        emulator.WithDataExplorer(cosmosDbDataExplorerPort);
    });

// Add the database to the Cosmos DB account
var cosmosDbDatabase = cosmosDb.AddCosmosDatabase("LibrisMaleficarum");
#pragma warning restore ASPIRECOSMOSDB001

// Azure AI Search — uses Aspire hosting integration (Aspire.Hosting.Azure.Search)
// Azure AI Search does not have a local emulator — uses Azure provisioning or an existing instance
// See: https://aspire.dev/integrations/cloud/azure/azure-ai-search/azure-ai-search-host/
var aiSearch = builder.AddAzureSearch("aisearch");

// Azure AI Foundry — uses Aspire hosting integration (Aspire.Hosting.Azure.AIFoundry)
// Provides AI model hosting and inference capabilities (embeddings, chat, etc.)
// See: https://aspire.dev/integrations/cloud/azure/azure-ai-foundry/azure-ai-foundry-host/
var aiFoundry = builder.AddAzureAIFoundry("aiFoundry");

// Add AI model deployments to the Foundry resource
var chatDeployment = aiFoundry.AddDeployment("chat", "gpt-5.2-chat", "2026-02-10", "OpenAI");
var embeddingDeployment = aiFoundry.AddDeployment("embedding", "text-embedding-3-large", "1", "OpenAI");

// Add Azure Storage (Azurite emulator) for local development
// Azurite provides blob, queue, and table storage emulation
// 
// Dynamic port assignment to prevent conflicts when multiple AppHost instances run in parallel
// (e.g., during integration tests). Each instance gets unique available ports starting from
// the standard Azurite defaults (10000, 10001, 10002).
var blobPort = FindAvailablePort(10000);
var queuePort = FindAvailablePort(blobPort + 1);
var tablePort = FindAvailablePort(queuePort + 1);

Console.WriteLine($"[AppHost] Azurite ports: Blob={blobPort}, Queue={queuePort}, Table={tablePort}");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataVolume();
        emulator.WithBlobPort(blobPort);
        emulator.WithQueuePort(queuePort);
        emulator.WithTablePort(tablePort);
    });

// Add blob service endpoint from storage
var blobs = storage.AddBlobs("blobs");

// Add the API service with Cosmos DB and Blob Storage references
var apiService = builder.AddProject<Projects.LibrisMaleficarum_Api>("api")
    .WithReference(cosmosDb)
    .WithReference(blobs)
    .WithReference(aiSearch)
    .WithReference(chatDeployment)
    .WithReference(embeddingDeployment)
    .WaitFor(cosmosDb)
    .WaitFor(cosmosDbDatabase)
    .WaitFor(storage)
    .WaitFor(chatDeployment)
    .WaitFor(embeddingDeployment);

// Propagate optional Entra ID auth configuration to API service
// When set, the API switches from anonymous single-user mode to multi-user Entra ID auth
var entraClientId = builder.Configuration["EntraId:ClientId"];
var entraTenantId = builder.Configuration["EntraId:TenantId"];
var entraAudience = builder.Configuration["EntraId:Audience"];

if (!string.IsNullOrEmpty(entraClientId))
{
    apiService
        .WithEnvironment("AzureAd__ClientId", entraClientId)
        .WithEnvironment("AzureAd__TenantId", entraTenantId ?? "common")
        .WithEnvironment("AzureAd__Audience", entraAudience ?? "api://libris-maleficarum-api");
}

// Add the Search Index Worker service (Change Feed Processor for AI Search sync)
// Runs independently from the API for fault isolation, independent scaling, and deployment independence
var searchWorker = builder.AddProject<Projects.LibrisMaleficarum_SearchIndexWorker>("search-index-worker")
    .WithReference(cosmosDb)
    .WithReference(aiSearch)
    .WithReference(embeddingDeployment)
    .WaitFor(cosmosDb)
    .WaitFor(cosmosDbDatabase)
    .WaitFor(embeddingDeployment);

// Add the React Vite frontend
var frontend = builder.AddViteApp("frontend", "../../../../libris-maleficarum-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

// Propagate optional Entra ID auth configuration to frontend
// Vite reads these as process.env vars and injects via define block (see vite.config.ts)
if (!string.IsNullOrEmpty(entraClientId))
{
    frontend
        .WithEnvironment("ENTRA_CLIENT_ID", entraClientId)
        .WithEnvironment("ENTRA_TENANT_ID", entraTenantId ?? "common");
}

// Seed sample data: run CLI importer to import Grimhollow sample world after the API is healthy.
// The frontend does NOT wait for this — it starts as soon as the API is ready.
// On subsequent runs, the import may fail if the world already exists — this is expected.
var sampleDataPath = Path.GetFullPath(
    Path.Combine(GetSourceFileDirectory(), "..", "..", "..", "..", "samples", "worlds", "grimhollow"));

builder.AddProject<Projects.LibrisMaleficarum_Cli>("seed-importer")
    .WithEnvironment("LIBRIS_API_URL", apiService.GetEndpoint("http"))
    .WithEnvironment("LIBRIS_API_TOKEN", "development-seed")
    .WithArgs("world", "import", "--source", sampleDataPath, "--verbose")
    .WaitFor(apiService);

builder.Build().Run();

/// <summary>
/// Returns the directory containing this source file at compile time.
/// Used to resolve relative paths to sample data regardless of runtime working directory.
/// </summary>
static string GetSourceFileDirectory([CallerFilePath] string sourceFilePath = "")
    => Path.GetDirectoryName(sourceFilePath)!;
