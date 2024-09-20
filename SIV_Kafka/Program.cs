using DataBase;
using DataBase.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIV_Kafka;
using NLog;
using NLog.Extensions.Logging;
using Util;

public class Program
{

    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            })
            .ConfigureServices((hostContext, services) =>
            {

                var configuration = hostContext.Configuration;
                services.AddSingleton(configuration);
                services.Configure<AppSettings>(configuration);

                // 配置日志  
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddNLog();
                });
                services.AddAutoMapperExt();

                services.AddScoped<MyDbContext>(provider => new MyDbContext(configuration));
                //services.AddSqlSugar(configuration);

                services.AddHostedService<DataCacheService>();
                // 注册其他服务
                services.AddSingleton<KafkaService>(); // 将 KafkaService 注册为单例
                //services.AddHostedService<FaultWarnService>();
                services.AddHostedService<KafkaStartService>();

            })
            .Build();

        host.Run();

    }
    
}
