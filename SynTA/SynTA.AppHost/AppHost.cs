var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SynTA>("synta");

builder.Build().Run();
