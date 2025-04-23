using Aspire.Hosting.MailDev;
using Aspire.Hosting.MinIO;
using SchulCloud.AppHost.Extensions;
using SchulCloud.ServiceDefaults;

namespace SchulCloud.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

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

        builder.AddProject<Projects.Gateway>("gateway")
            .WithReference(webFrontend)
            .WithReference(restApi)
            .WithEndpoint("http", e => e.Port = 8000, createIfNotExists: false)
            .WithEndpoint("https", e => e.Port = 8001, createIfNotExists: false)
            .WithExternalHttpEndpoints();

        builder.Build().Run();
    }
}