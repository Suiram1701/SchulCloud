IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SchulCloud_Server>("schulcloud-server");

builder.Build().Run();
