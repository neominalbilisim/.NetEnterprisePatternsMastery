using BuildingBlocks.Abstractions.Results;
using Microsoft.Extensions.Logging;
using Ordering.Application.Errors;
using Ordering.Application.Ports;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ordering.Infrastructure.Adapters.LegacyErp;

/// <summary>
/// ANTI-CORRUPTION LAYER - Adapter (Facade):
/// ICustomerCreditService portunu Legacy ERP üzerinden uygular.
/// Client (protokol) + Translator (model çevirisi) + hata çevirisi (exception -> Result) burada birleşir.
/// </summary>
internal sealed class LegacyErpCustomerCreditAdapter(
    LegacyErpClient erpClient,
    ILogger<LegacyErpCustomerCreditAdapter> logger) : ICustomerCreditService
{
    public async Task<Result<CustomerCreditInfo>> GetCreditInfoAsync(
        string customerCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var erpCustomer = await erpClient.GetCustomerAsync(customerCode, cancellationToken);
            if (erpCustomer is null)
            {
                return CustomerErrors.NotFound(customerCode);
            }

            return LegacyErpCustomerTranslator.ToCustomerCreditInfo(erpCustomer);
        }
        catch (Exception ex) when (ex is BrokenCircuitException or TimeoutRejectedException or HttpRequestException)
        {
            // Resilience pipeline tüm denemeleri tükettiyse veya devre açıksa (circuit open)
            // teknik hata, uygulamanın anlayacağı bir iş sonucuna çevrilir.
            logger.LogWarning(ex, "Legacy ERP call failed for customer {CustomerCode}: {ErrorType}",
                customerCode, ex.GetType().Name);
            return CustomerErrors.ServiceUnavailable;
        }
    }
}
