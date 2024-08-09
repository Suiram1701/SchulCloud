using Aspire.Hosting.MailDev;

namespace SchulCloud.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

        IResourceBuilder<PostgresDatabaseResource> postgresDb = AddPostgresDatabase(builder);
        IResourceBuilder<MailDevResource> mailDev = AddMailDev(builder);

        builder.AddProject<Projects.SchulCloud_DbManager>("schulcloud-dbmanager")
            .WithReference(postgresDb);

        builder.AddProject<Projects.SchulCloud_Web>("schulcloud-web")
            .WithReference(postgresDb)
            .WithReference(mailDev);

        builder.Build().Run();
    }

    private static IResourceBuilder<PostgresDatabaseResource> AddPostgresDatabase(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> username = builder.AddParameter("postgresUsername");
        IResourceBuilder<ParameterResource> password = builder.AddParameter("postgresPassword");

        return builder.AddPostgres("postgres-server", username, password)
            .WithDataVolume()
            .AddDatabase("schulcloud-db");
    }

    private static IResourceBuilder<MailDevResource> AddMailDev(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> username = builder.AddParameter("maildevUsername");
        IResourceBuilder<ParameterResource> password = builder.AddParameter("maildevPassword");

        return builder.AddMailDev("maildev", username: username, password: password);
    }
}