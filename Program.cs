using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LavaCake.Kevin;
using LavaCake.SourPi;
using Quartz;

namespace LavaCake;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Activating LavaCake...");

        var host = Host.CreateDefaultBuilder(args).ConfigureServices(services =>
        {
            services.AddHostedService<KevinService>();
            services.AddHostedService<FileWatcherService>();
        }).Build();

        await host.RunAsync();
    }
}
