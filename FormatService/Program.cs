using Axinom.Toolkit;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace FormatService
{
    static class Program
    {
        public static IConfiguration Configuration { get; private set; }

        static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Default.RegisterListener(new ConsoleLogListener());

            WebHost.CreateDefaultBuilder()
                .UseKestrel()
                .UseUrls("http://+:5643")
                .ConfigureServices(services =>
                {
                    services.AddMvc();
                })
                .Configure(builder =>
                {
                    builder.UseMvc();
                })
                .Build()
                .Run();
        }
    }
}
