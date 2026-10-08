using Microsoft.AspNetCore.Builder;
using Serilog;

namespace BuildingBlocks.Infrastructure.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Serilog yapılandırması: Minimum seviye ve override'lar appsettings'ten, çıktılar Console + Seq.
    /// Her log kaydına "Application" alanı eklenir; Seq'te servis bazında filtreleme yapılabilir.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder, string applicationName)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

        return builder;
    }
}
