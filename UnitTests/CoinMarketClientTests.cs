using AssignmentApp.Code.Clients.CoinMarketCap;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests
{
    [TestClass]
    public sealed class CoinMarketClientTests
    {
        [TestMethod]
        public async Task HappyPath()
        {
            var httpClient = new HttpClient(new FuncHttpMessageHandler(req => new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new ByteArrayContent(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new CryptoExchangeRates(
                    new Dictionary<string, IReadOnlyList<CryptoCurrency>>()
                    {
                        ["BTC"] = [new CryptoCurrency(1, new Dictionary<string, CurrencyRate>() 
                        {
                            [req.RequestUri!.ToString().Contains("USD") ? "USD" : "EUR"] = new CurrencyRate(Price: 1.234M)
                        }, Platform: null)],
                        ["ETH"] = [new CryptoCurrency(2, new Dictionary<string, CurrencyRate>()
                        {
                            [req.RequestUri!.ToString().Contains("USD") ? "USD" : "EUR"] = new CurrencyRate(Price: 53.123M)
                        }, Platform: null)]
                    }
                )))
            }))
            {
                BaseAddress = new Uri("https://don-call-me.no")
            };

            var client = new CoinMarketClient(
                httpClient, 
                new Mock<ILogger<CoinMarketClient>>().Object, 
                new LazyCache.CachingService(), 
                freeVersion: true
            );

            var r = await client.GetQuotes(["BTC", "ETH"], ["USD", "EUR"], includeTokens: false, default);

            Assert.IsNotNull(r.Some);
            Assert.IsNull(r.Err);
            Assert.AreEqual(2, r.Some.Data.Count);

            var btc = r.Some.Data["BTC"].Single();
            var eth = r.Some.Data["ETH"].Single();

            Assert.AreEqual(2, btc.Quote.Count);
            Assert.AreEqual(2, eth.Quote.Count);

            foreach (var d in new[] { btc, eth })
            {
                Assert.AreEqual(2, d.Quote.Count);
                var eur = d.Quote["EUR"];
                var usd = d.Quote["USD"];

                Assert.IsTrue(eur.Price > 0);
                Assert.IsTrue(usd.Price > 0);
            }
        }
    }

    public class FuncHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> func): HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(func(request));
        }
    }
}
