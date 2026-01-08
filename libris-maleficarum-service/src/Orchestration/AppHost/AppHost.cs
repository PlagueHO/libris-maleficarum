#pragma warning disable ASPIRECOSMOSDB001 // Suppress experimental diagnostic for preview emulator

var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Cosmos DB Linux-based Emulator (preview) for local development
// Includes Data Explorer endpoint for debugging
var cosmosdb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithDataExplorer();
    });

// Add the API service with Cosmos DB reference
var apiService = builder.AddProject<Projects.LibrisMaleficarum_Api>("api")
    .WithReference(cosmosdb)
    .WaitFor(cosmosdb);

// Add the React Vite frontend
var frontend = builder.AddViteApp("frontend", "../../../../libris-maleficarum-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
