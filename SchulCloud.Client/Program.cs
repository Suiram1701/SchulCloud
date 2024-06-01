using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace SchulCloud.Client;

class Program
{
    static async Task Main(string[] args)
    {
        WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

        await builder.Build().RunAsync();
    }
}
