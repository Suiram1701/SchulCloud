namespace SchulCloud.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

        IResourceBuilder<PostgresDatabaseResource> postgresdb = AddPostgresDatabase(builder);

        builder.AddProject<Projects.SchulCloud_DbManager>("schulcloud-dbmanager")
            .WithReference(postgresdb);

        builder.AddProject<Projects.SchulCloud_Web>("schulcloud-web")
            .WithReference(postgresdb);

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
}