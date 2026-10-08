using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.IntegrationEvents;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Idempotency;
using BuildingBlocks.Infrastructure.Persistence.Inbox;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Command tarafı (EF Core), Query tarafı (Dapper bağlantı fabrikası), Unit of Work,
    /// Inbox ve Idempotency store kayıtları.
    /// </summary>
    public static IServiceCollection AddPersistenceCore<TContext>(this IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

    // PostgreSQL'deki snake_case sütun adlarını C# PascalCase property'lerine otomatik map etmek için Dapper'a verilen en temiz ayardır.
    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    // NET 6/7/8 ile gelen NpgsqlDataSource kullanımı bağlantı havuzu (connection pooling) performansını devasa miktarda artırır. AddSingleton olarak kaydedip connection pooling performansını sağladık.
    // Read side için Entity Framework gibi ağır ORM'ler yerine Dapper ve immutable DTO/Response modelleri seçmek minimum bellek harcaması ve maksimum throughput sağlar.
    services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<ISqlConnectionFactory, NpgsqlConnectionFactory>();

        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddScoped<IInboxStore, EfInboxStore<TContext>>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore<TContext>>();

        return services;
    }

    /// <summary>
    /// DotNetCore.CAP ile Outbox + RabbitMQ yapılandırması.
    /// CAP, outbox (cap.published) ve kendi alım kayıtlarını (cap.received) servisin kendi Postgres veritabanında tutar.
    /// </summary>
    public static IServiceCollection AddCapMessaging<TContext>(
        this IServiceCollection services, IConfiguration configuration, string groupName)
        where TContext : DbContext
    {
        var rabbitMq = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddCap(options =>
        {
            options.UseEntityFramework<TContext>();
            options.UseRabbitMQ(rabbit =>
            {
                rabbit.HostName = rabbitMq.HostName;
                rabbit.Port = rabbitMq.Port;
                rabbit.UserName = rabbitMq.UserName;
                rabbit.Password = rabbitMq.Password;
            });
            options.UseDashboard(); // http://<servis>/cap

            // Her servis kendi consumer grubuyla dinler; aynı servisin birden fazla instance'ı
            // mesajları paylaşarak (competing consumers) işler.
            options.DefaultGroupName = groupName;

            // Resiliency (messaging seviyesi): Başarısız yayın/tüketim işlemleri arka planda yeniden denenir.
            options.FailedRetryCount = 5;
            options.FailedRetryInterval = 15; // saniye
            options.SucceedMessageExpiredAfter = 24 * 3600;
            options.FailedThresholdCallback = failed =>
            {
                var logger = failed.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CAP");
                failed.Message.Headers.TryGetValue(Headers.MessageName, out var messageName);
                logger.LogError(
                    "CAP {MessageType} message {MessageName} exhausted all retries and requires manual intervention",
                    failed.MessageType, messageName);
            };
        });

        services.AddScoped<IIntegrationEventPublisher, CapIntegrationEventPublisher>();

        return services;
    }
}
