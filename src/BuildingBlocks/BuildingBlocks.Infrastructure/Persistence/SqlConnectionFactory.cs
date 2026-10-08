using System.Data.Common;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Dapper (Query tarafı) için bağlantı fabrikası. Okuma tarafı EF Core change tracker'ını kullanmaz;
/// doğrudan, projeksiyon odaklı SQL çalıştırır.
/// </summary>
public interface ISqlConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}

internal sealed class NpgsqlConnectionFactory(NpgsqlDataSource dataSource) : ISqlConnectionFactory
{
  /// <summary>
  /// CancellationToken: Tüm sorgulara CancellationToken geçerek HTTP talebi iptal edildiğinde PostgreSQL üzerindeki sorguyu da abort ettirebiliruz. Bu, sunucu kaynaklarını korur.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
        await dataSource.OpenConnectionAsync(cancellationToken);
}
