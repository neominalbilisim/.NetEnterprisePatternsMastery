using System.Net;
using System.Net.Http.Json;

namespace Ordering.Infrastructure.Adapters.LegacyErp;

/// <summary>
/// Legacy ERP'ye HTTP ile erişen typed client. Resiliency pipeline'ı (retry, timeout, circuit breaker)
/// HttpClient'a DI tarafında eklenir; bu sınıf yalnızca protokol detayını bilir.
/// </summary>
internal sealed class LegacyErpClient(HttpClient httpClient)
{
    public async Task<ErpCustomerResponse?> GetCustomerAsync(string customerNo, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"erp/api/v1/musteri/{Uri.EscapeDataString(customerNo)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ErpCustomerResponse>(cancellationToken);
    }
}
