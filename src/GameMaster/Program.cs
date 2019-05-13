using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameMaster
{
    public class Program
    {
        public static void Main(string[] args) => 
            CreateHostBuilder(args).Build().Run();

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddHostedService<GameMaster>());
    }
}