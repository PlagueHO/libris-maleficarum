var builder = DistributedApplication.CreateBuilder(args);

// Add the API service
var apiService = builder.AddProject<Projects.LibrisMaleficarum_Api>("api");

builder.Build().Run();
