using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;

namespace BuildingBlocks.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Örnek uygulama için şemayı oluşturur (EnsureCreated) ve isteğe bağlı seed çalıştırır.
    /// Veritabanı container'ı henüz hazır olmayabileceği için Polly v8 (Polly.Core) ile retry uygulanır.
    /// Gerçek projelerde EnsureCreated yerine EF Core Migrations kullanılmalıdır.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync<TContext>(
        this IServiceProvider services,
        Func<TContext, CancellationToken, Task>? seed = null)
        where TContext : DbContext
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>().Handle<TimeoutException>(),
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    logger.LogWarning("Database is not ready yet (attempt {Attempt}). Retrying...", args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        await pipeline.ExecuteAsync(async cancellationToken =>
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            if (seed is not null)
            {
                await seed(context, cancellationToken);
            }
        }, CancellationToken.None);

        logger.LogInformation("Database for {DbContext} is ready", typeof(TContext).Name);
    }
}
