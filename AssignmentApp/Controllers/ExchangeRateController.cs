using AssignmentApp.Code;
using AssignmentApp.Code.Clients.CoinMarketCap;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;

namespace AssignmentApp.Controllers;

[ApiController]
public class ExchangeRateController(
    ILogger<ExchangeRateController> logger, 
    ICoinMarketClient coinMarketClient,
    CurrencyConfig currencyConfig
) : ControllerBase
{
    [HttpGet]
    [Route("api/1.0/crypto/exchange-rates")]
    public async Task<IActionResult> Get(
        [FromQuery] IReadOnlyList<string> cryptoCurrencySymbols,
        [FromQuery] IReadOnlyList<string>? fiatCurrencySymbols = null,
        [FromQuery] bool includeTokens = false
    )
    {
        cryptoCurrencySymbols = cryptoCurrencySymbols?.Where(q => !string.IsNullOrWhiteSpace(q)).ToArray() ?? [];
        fiatCurrencySymbols = fiatCurrencySymbols?.Where(q => !string.IsNullOrWhiteSpace(q)).ToArray() ?? [];

        if (cryptoCurrencySymbols is { Count: 0 })
        {
            return BadRequest(new { message = $"at least one {nameof(cryptoCurrencySymbols)} is expected" });
        }

        if (fiatCurrencySymbols is { Count: 0 })
        {
            fiatCurrencySymbols = currencyConfig.DefaultFiatCurrencySymbols;
        }

        try
        {
            var r = await coinMarketClient.GetQuotes(
                cryptoCurrencySymbols, 
                fiatCurrencySymbols, 
                includeTokens, 
                Request.HttpContext.RequestAborted
            );

            return r switch
            {
                { Some: { } s } => Ok(s),
                { Err: { } clientError } => clientError.Status?.ErrorCode switch
                {
                    null => StatusCode(500, new { message = $"Unexpected error: coin-market-cap did not provide the reason" }),
                    429 or 1008 => StatusCode(429, new { message = "too many requesrs: slow down", client_error = clientError }),
                    _ => StatusCode(500, new { message = "coin-market-cap could not process the request, see clientError for details", client_error = clientError })
                },
                _ => throw new UnreachableException(),
            };
        }
        catch (Exception e) when (Request.HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning(e, "client timeout");
            throw;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "unhandled exception");
            throw;
        }
    }
}
