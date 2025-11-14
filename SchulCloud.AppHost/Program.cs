using Aspire.Hosting.MailDev;
using Aspire.Hosting.MinIO;
using Aspire.Hosting.Yarp.Transforms;
using Microsoft.Extensions.DependencyInjection;
using SchulCloud.AppHost.Extensions;
using SchulCloud.ServiceDefaults;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Hide parameters in the dashboard
builder.Eventing.Subscribe<BeforeStartEvent>(async (e, _) =>
{
    var resourceNotification = e.Services.GetRequiredService<ResourceNotificationService>();
    foreach (ParameterResource p in e.Model.Resources.OfType<ParameterResource>())
    {
        await resourceNotification.PublishUpdateAsync(p, s => s with {  IsHidden = true });
    }
});

IResourceBuilder<PostgresDatabaseResource> identityDb = builder 
    .AddPostgresServer("postgres-server")
    .AddDatabase(ResourceNames.IdentityDatabase); 
IResourceBuilder<MinIOBucketDatabaseResource> schulcloudBucket = builder
    .AddMinIO("minio-server")
    .AddBucket(ResourceNames.FileBucket);

IResourceBuilder<MailDevResource> mailDev = builder.AddMailDev(ResourceNames.MailServer);

IResourceBuilder<ProjectResource> webFrontend = builder.AddProject<Projects.SchulCloud_Frontend>("web-frontend")
    .WithReference(identityDb)
    .WithReference(schulcloudBucket)
    .WithReference(mailDev)
    .WaitFor(identityDb)
    .WaitFor(schulcloudBucket)
    .WaitFor(mailDev)
    .WithDefaultHealthChecks()
    .WithDefaultCommands()
    .WithExternalHttpEndpoints();

IResourceBuilder<ProjectResource> restApi = builder.AddProject<Projects.SchulCloud_RestApi>("rest-api")
    .WithReference(identityDb)
    .WithReference(schulcloudBucket)
    .WaitFor(identityDb)
    .WaitFor(schulcloudBucket)
    .WithDefaultHealthChecks()
    .WithDefaultCommands()
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.SchulCloud_DbManager>("db-manager")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithDefaultHealthChecks()
    .WithDefaultCommands()
    .WithDbManagerCommands();

builder.AddYarp("gateway")
    .WithConfiguration(yarp =>
    {
        yarp.AddRoute("{**catch-all}", webFrontend);
        yarp.AddRoute("/api/rest/{**remainder}", restApi)
            .WithTransformPathRemovePrefix("/api/rest");
    })
    .WithEndpoint("http", e => e.Port = 8000)
    // .WithEndpoint("https", e => e.Port = 8001)     // Waiting for HTTPS support in https://github.com/dotnet/aspire/issues/11534
    .WithExternalHttpEndpoints();

builder.Build().Run();