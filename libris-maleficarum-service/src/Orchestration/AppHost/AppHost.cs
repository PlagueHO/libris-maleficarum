using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOSMOSDB001 // Suppress experimental diagnostic for preview emulator
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

IResourceBuilder<AzureCosmosDBResource> cosmosdb;

// Add Azure Cosmos DB Linux-based Emulator (preview) for local development
// Configure emulator using documented environment variables
// See: https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux#docker-commands
cosmosdb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithGatewayPort(cosmosDbGatewayPort);
        emulator.WithDataExplorer(cosmosDbDataExplorerPort);
    });
#pragma warning restore ASPIRECOSMOSDB001

// Add Azure Storage (Azurite emulator) for local development
// Azurite provides blob, queue, and table storage emulation
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        // Azurite uses fixed ports: 10000 (blob), 10001 (queue), 10002 (table)
        // These are standard Azurite defaults and should not be changed
        emulator.WithDataVolume();
    });

// Add blob service endpoint from storage
var blobs = storage.AddBlobs("blobs");

// Add the API service with Cosmos DB and Blob Storage references
var apiService = builder.AddProject<Projects.LibrisMaleficarum_Api>("api")
    .WithReference(cosmosdb)
    .WithReference(blobs)
    .WaitFor(cosmosdb)
    .WaitFor(storage);

// Add the React Vite frontend
var frontend = builder.AddViteApp("frontend", "../../../../libris-maleficarum-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
