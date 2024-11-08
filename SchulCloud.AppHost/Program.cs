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

        builder.AddProject<Projects.SchulCloud_DbManager>("db-manager")
            .WithReference(identityDb);

        builder.AddProject<Projects.SchulCloud_Frontend>("frontend")
            .WithReference(identityDb)
            .WithReference(mailDev);

        builder.Build().Run();
    }
}