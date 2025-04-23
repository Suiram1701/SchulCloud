using SchulCloud.ServiceDefaults;

namespace Gateway;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration);

        WebApplication app = builder.Build();
        app.MapReverseProxy();

        app.Run();
    }
}
