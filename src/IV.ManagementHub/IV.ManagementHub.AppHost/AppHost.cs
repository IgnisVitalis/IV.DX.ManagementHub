var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.IV_ManagementHub_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.IV_ManagementHub_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
