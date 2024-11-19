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
            .WithDefaultHealthChecks()
            .WaitFor(mailDev);     // The MailKit health check fails if mail dev isn't available on start.

        builder.AddProject<Projects.SchulCloud_DbManager>("db-manager")
            .WithReference(identityDb)
            .WithDefaultHealthChecks();

        bool isHttps = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https";
        int? gatewayPort = int.TryParse(builder.Configuration["GatewayPort"], out int port) ? port : null;

        builder.AddYarp("gateway")
            .WithEndpoint(scheme: isHttps ? "https" : "http", port: gatewayPort)
            .LoadFromConfiguration("ReverseProxy")
            .WithReference(webFrontend);

        builder.Build().Run();
    }
}