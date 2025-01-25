using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssignmentApp.Code.Clients.CoinMarketCap;

public record ApiResult<TSome>(TSome? Some, CoinMarketCapError? Err)
    : Either<TSome, CoinMarketCapError>(Some, Err);

public class CoinMarketClient(
    HttpClient httpClient,
    ILogger<CoinMarketClient> logger,
    LazyCache.IAppCache appCache,
    bool freeVersion
) : ICoinMarketClient
{
    public static readonly string HTTP_CLIENT_NAME = Guid.NewGuid().ToString();

    public async Task<ApiResult<CryptoExchangeRates>> GetQuotes(
        IReadOnlyList<string> cryptoCurrencySymbols,
        IReadOnlyList<string> fiatCurrencySymbols,
        bool includeTokens, 
        CancellationToken cancellationToken
    )
    {
        var cryptoParam = string.Join(",", cryptoCurrencySymbols.Select(Esc));

        if (freeVersion)
        {
            // CoinMarket's free version does not support multiple "convert" values
            // so multiple calls are merged into one

            var acc = new Dictionary<string, List<CryptoCurrency>>();

            foreach (var fc in fiatCurrencySymbols)
            {
                var fiatParam = Esc(fc);
                var cacheKey = MakeCacheKey(cryptoParam, fiatParam, includeTokens);

                switch (await appCache.GetOrAddAsync(cacheKey, ce => FromSource(ce, fiatParam)))
                {
                    case { Err: { } } c:
                        return c;
                    case { Some.Data: null or { Count: 0} }:
                        continue;
                    case var c:
                        MergeInto(c.Some!.Data, acc);
                        break;
                }
            }

            return new ApiResult<CryptoExchangeRates>(new CryptoExchangeRates(acc.ToReadOnly()), Err: null);
        }
        else
        {
            var fiatParam = string.Join(",", fiatCurrencySymbols.Select(Esc));
            var cacheKey = MakeCacheKey(cryptoParam, fiatParam, includeTokens);
            return await appCache.GetOrAddAsync(cacheKey, ce => FromSource(ce, fiatParam));
        }

        async Task<ApiResult<CryptoExchangeRates>> FromSource(ICacheEntry cacheEntry, string fiatParam)
        {
            HttpContent content;
            switch (await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"/v2/cryptocurrency/quotes/latest?symbol={cryptoParam}&convert={fiatParam}"),
                cancellationToken
            ))
            {
                case { IsSuccessStatusCode: true } c:
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(61);
                    content = c.Content;
                    break;
                case var r:
                    var err = await r.Content.ReadFromJsonAsync<CoinMarketCapError>(cancellationToken: cancellationToken);
                    if (err?.Status is { } status
                        && (status.ErrorCode is not null || status.ErrorMessage is not null))
                    {
                        cacheEntry.AbsoluteExpirationRelativeToNow = (int)r.StatusCode switch
                        {
                            429 => TimeSpan.FromSeconds(61),
                            < 500 => TimeSpan.FromMinutes(60),
                            >= 500 => TimeSpan.FromSeconds(30)
                        };
                     
                        return new(null, err);
                    }
                    else
                    {
                        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);

                        var rawError = await r.Content.ReadAsStringAsync(cancellationToken);
                        logger.LogError("Unexpected error when getting latest crypto quotes; {error}; {traceId}", rawError, Activity.Current?.TraceId);
                        r.EnsureSuccessStatusCode();
                        throw new UnreachableException();
                    }
            }

            var obj = (await content.ReadFromJsonAsync<CryptoExchangeRates>(cancellationToken))!;

            if (!includeTokens)
            {
                obj = obj with
                {
                    Data = obj.Data.ToDictionary(q => q.Key, q => q.Value.Where(q => q.Platform is null).ToReadOnly())
                };
            }

            return new(obj, null);
        }

        string Esc(string str) => Uri.EscapeDataString(str);
    }

    private static void MergeInto(
        IReadOnlyDictionary<string, IReadOnlyList<CryptoCurrency>> input,
        Dictionary<string, List<CryptoCurrency>> acc
    )
    {
        foreach (var (k, inputCryptos) in input)
        {
            if (!acc.TryGetValue(k, out var accCryptos))
            {
                acc[k] = [.. inputCryptos];
                continue;
            }

            foreach (var ic in inputCryptos)
            {
                var index = accCryptos.FindIndex(z => z.ExternalId == ic.ExternalId);
                var ac = accCryptos[index];

                if (ac is null)
                {
                    accCryptos.Add(ic);
                    continue;
                }

                ac = ac with { Quote = ac.Quote.CopyAdd(ic.Quote) };

                accCryptos[index] = ac;
            }
        }
    }

    private static string MakeCacheKey(string crypto, string fiat, bool includeTokens) => $"cc:{crypto}_fiat:{fiat}_inct:{includeTokens}";
}