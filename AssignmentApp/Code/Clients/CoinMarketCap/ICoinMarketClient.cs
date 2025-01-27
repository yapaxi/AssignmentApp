
using System.Text.Json.Serialization;

namespace AssignmentApp.Code.Clients.CoinMarketCap;

public interface ICoinMarketClient
{
    Task<ApiResult<CryptoExchangeRates>> GetQuotes(
        CoinMarketAuth auth,
        IReadOnlyList<string> cryptoCurrencySymbols,
        IReadOnlyList<string> fiatCurrencySymbols,
        bool includeTokens, 
        CancellationToken cancellationToken
    );
}

public record CoinMarketAuth(string ApiKey, Uri Url);

public record ApiResult<TSome>(TSome? Some, CoinMarketCapError? Err)
    : Either<TSome, CoinMarketCapError>(Some, Err);

public record CryptoExchangeRates(
    [property: JsonPropertyName("data")] IReadOnlyDictionary<string, IReadOnlyList<CryptoCurrency>> Data
);

public record CryptoCurrency(
    [property: JsonPropertyName("id")] int ExternalId,
    [property: JsonPropertyName("quote")] IReadOnlyDictionary<string, CurrencyRate> Quote,
    [property: JsonPropertyName("platform")] Platform? Platform
);

public record CurrencyRate(
    [property: JsonPropertyName("price")] decimal Price
);

public record CoinMarketCapError(
    [property: JsonPropertyName("status")] ErrorStatus? Status
);

public record ErrorStatus(
    [property: JsonPropertyName("error_code")] int? ErrorCode,
    [property: JsonPropertyName("error_message")] string? ErrorMessage
);

public record Platform(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("token_address")] string TokenAddress
);
