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

        IResourceBuilder<PostgresServerResource> postgresServer = builder.AddPostgresServer("postgres-server");
        IResourceBuilder<PostgresDatabaseResource> identityDb = postgresServer.AddDatabase(ResourceNames.IdentityDatabase);

        IResourceBuilder<MinIOServerResource> minIOStorage = builder.AddMinIO("minio-server");
        IResourceBuilder<MinIOBucketResource> schulcloudBucket = minIOStorage.AddBucket(ResourceNames.SchulCloudStorage);

        IResourceBuilder<MailDevResource> mailDev = builder.AddMailDev(ResourceNames.MailServer);

        IResourceBuilder<ProjectResource> webFrontend = builder.AddProject<Projects.SchulCloud_Frontend>("web-frontend")
            .WithReference(identityDb)
            .WithReference(schulcloudBucket)
            .WithReference(mailDev)
            .WaitFor(identityDb)
            .WaitFor(schulcloudBucket)
            .WaitFor(mailDev)
            .WithDefaultHealthChecks()
            .WithDefaultCommands();

        IResourceBuilder<ProjectResource> restApi = builder.AddProject<Projects.SchulCloud_RestApi>("rest-api")
            .WithReference(identityDb)
            .WithReference(schulcloudBucket)
            .WaitFor(identityDb)
            .WaitFor(schulcloudBucket)
            .WithDefaultHealthChecks()
            .WithDefaultCommands();

        builder.AddProject<Projects.SchulCloud_DbManager>("db-manager")
            .WithReference(identityDb)
            .WithReference(schulcloudBucket)
            .WaitFor(identityDb)
            .WaitFor(schulcloudBucket)
            .WithDefaultHealthChecks()
            .WithDefaultCommands()
            .WithDbManagerCommands();

        builder.AddYarp("gateway")
            .WithEndpoint(scheme: "http", port: 8000)
            .WithEndpoint(scheme: "https", port: 8001)
            .WithExternalHttpEndpoints()
            .WithReference(webFrontend)
            .WithReference(restApi)
            .LoadFromConfiguration("ReverseProxy");

        builder.Build().Run();
    }
}