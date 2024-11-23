using Aspire.Hosting.MailDev;
using SchulCloud.ServiceDefaults;

namespace SchulCloud.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

        IResourceBuilder<PostgresServerResource> postgresServer = builder.AddPostgresServer("postgres-server");
        IResourceBuilder<PostgresDatabaseResource> identityDb = postgresServer.AddDatabase(ResourceNames.IdentityDatabase);

        IResourceBuilder<MailDevResource> mailDev = builder.AddMailDev(ResourceNames.MailServer);

        IResourceBuilder<ProjectResource> webFrontend = builder.AddProject<Projects.SchulCloud_Frontend>("web-frontend")
            .WithReference(identityDb)
            .WithReference(mailDev)
            .WaitFor(mailDev)     // The MailKit health check fails if mail dev isn't available on start.
            .WithDefaultHealthChecks();

        IResourceBuilder<ProjectResource> restApi = builder.AddProject<Projects.SchulCloud_RestApi>("rest-api")
            .WithReference(identityDb)
            .WithDefaultHealthChecks();

        builder.AddProject<Projects.SchulCloud_DbManager>("db-manager")
            .WithReference(identityDb)
            .WithDefaultHealthChecks();

        builder.AddYarp("gateway")
            .WithEndpoint(scheme: "http", port: 8000)
            .WithEndpoint(scheme: "https", port: 8001)
            .WithReference(webFrontend)
            .WithReference(restApi)
            .LoadFromConfiguration("ReverseProxy");

        builder.Build().Run();
    }
}