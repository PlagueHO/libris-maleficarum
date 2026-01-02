var builder = DistributedApplication.CreateBuilder(args);

// Add the API service
var apiService = builder.AddProject<Projects.LibrisMaleficarum_Api>("api");

// Add the React Vite frontend
var frontend = builder.AddViteApp("frontend", "../../../../libris-maleficarum-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
