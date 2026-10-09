using BuildingBlocks.Infrastructure;
using Contracts.Grpc.Inventory;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Ports;
using Ordering.Domain.Customers;
using Ordering.Domain.Orders;
using Ordering.Infrastructure.Adapters.Grpc;
using Ordering.Infrastructure.Adapters.LegacyErp;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Persistence.ReadModels;
using Ordering.Infrastructure.Persistence.Repositories;
using Polly;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderingDb")
            ?? throw new InvalidOperationException("Connection string 'OrderingDb' is missing.");

        // Persistence (EF Core command + Dapper query + UoW + Inbox/Idempotency) ve CAP Outbox
        services.AddPersistenceCore<OrderingDbContext>(connectionString);
        services.AddCapMessaging<OrderingDbContext>(configuration, groupName: "ordering");

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderReadRepository, DapperOrderReadRepository>();

        services.AddInventoryGrpcClient(configuration);
        services.AddLegacyErpClient(configuration);

        return services;
    }

    /// <summary>
    /// gRPC client + gRPC'nin yerleşik retry politikası.
    /// Neden HTTP resilience handler değil? gRPC hataları HTTP 200 + trailer içinde döner; HTTP seviyesindeki
    /// bir retry bunları göremez. gRPC ServiceConfig, StatusCode bazında doğru retry yapar.
    /// </summary>
    private static void AddInventoryGrpcClient(this IServiceCollection services, IConfiguration configuration)
    {
        var address = configuration["Services:InventoryGrpc"]
            ?? throw new InvalidOperationException("Configuration 'Services:InventoryGrpc' is missing.");

    // InventoryService.InventoryServiceClient grpc Tools tarafından üretilen client sınıfıdır. DI ile inject edilebilir.
    services.AddGrpcClient<InventoryService.InventoryServiceClient>(options => options.Address = new Uri(address))
            .ConfigureChannel(channel =>
            {
                channel.ServiceConfig = new ServiceConfig
                {
                    MethodConfigs =
                    {
                        new MethodConfig
                        {
                            Names = { MethodName.Default },
                            RetryPolicy = new RetryPolicy
                            {
                                MaxAttempts = 4,
                                InitialBackoff = TimeSpan.FromMilliseconds(500),
                                MaxBackoff = TimeSpan.FromSeconds(5), // maksimum bekleme süresi 5 sn; retry sayısı arttıkça bekleme süresi üstel olarak artar.
                                BackoffMultiplier = 2,
                                // Her başarısız denemeden sonra bekleme süresi üstel olarak artar (500ms -> 1s -> 2s -> 4s).
                                // Sunucu cevap vermiyorsa hemen üstüne gitme, her denemede bekleme süreni iki katına çıkararak sunucuya nefes alacak zaman tanı" diyen koruma kalkanıdır.
                                RetryableStatusCodes = { StatusCode.Unavailable }
                              // Yalnızca geçici ağ kesintileri veya pod/sunucu yeniden başlatmaları durumunda devreye girer; mantıksal hatalarda (örn: NotFound, InvalidArgument) gereksiz retry yaparak sunucuyu boğmaz.
                              // gRPC kütüphanesi bu süreleri hesaplarken milisaniyelik rastgele sapmalar (Jitter) ekler. Örneğin tam 1.000 ms yerine 940 ms veya 1.060 ms bekler. Bunun amacı, aynı anda çöken 1.000 farklı istemcinin milisaniyesi milisaniyesine aynı anda tekrar istek atıp sunucuyu kitlemesini engellemektir.
                            }
                        }
                    }
                };
            });

        services.AddScoped<IInventoryService, InventoryGrpcAdapter>();
    }

    /// <summary>
    /// Legacy ERP HTTP client + Microsoft.Extensions.Http.Resilience (Polly v8) standart pipeline:
    /// Rate limiter -> Total timeout -> Retry (exponential + jitter) -> Circuit breaker -> Attempt timeout
    /// </summary>
    private static void AddLegacyErpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Services:LegacyErp"]
            ?? throw new InvalidOperationException("Configuration 'Services:LegacyErp' is missing.");

        services.AddHttpClient<LegacyErpClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            })
            .AddStandardResilienceHandler(options =>
            {
                // Retry: 3 deneme, üstel bekleme + jitter (thundering herd'ü önler)
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromMilliseconds(300);

                // Timeout: tek deneme 2 sn, tüm süreç (retry'lar dahil) 10 sn
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);

                // Circuit breaker: 10 sn içinde en az 5 istekte hata oranı %50'yi aşarsa 15 sn devre açık kalır.
                // (SamplingDuration, AttemptTimeout'un en az iki katı olmalıdır.)
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });

        services.AddScoped<ICustomerCreditService, LegacyErpCustomerCreditAdapter>();
    }
}
