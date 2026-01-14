using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;

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
    .WaitFor(cosmosDb)
    .WaitFor(cosmosDbDatabase)
    .WaitFor(storage);

// Add the React Vite frontend
var frontend = builder.AddViteApp("frontend", "../../../../libris-maleficarum-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
