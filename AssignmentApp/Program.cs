using AssignmentApp.Code;
using AssignmentApp.Code.Clients.CoinMarketCap;
using LazyCache;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// asp.net
{
    builder.Services.AddLogging(q => q.AddConsole());
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(c =>
    {
        c.ResolveConflictingActions(apiDescription => apiDescription.Last());

        const string API_KEY_SCHEME = "ApiKey";

        c.AddSecurityDefinition(API_KEY_SCHEME, new OpenApiSecurityScheme()
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = $"Input format: {API_KEY_SCHEME} the-key"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = API_KEY_SCHEME,
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

// infra
{
    builder.Services.AddSingleton<IAppCache>(q => new CachingService());
}

// deps
{
    builder.Services.AddSingleton(q => new CurrencyConfig(
        DefaultFiatCurrencySymbols: ["USD", "EUR", "BRL", "GBP", "AUD"]
    ));

    AddCointMarket();
}

// app
{
    var app = builder.Build();

    var logger = app.Services.GetRequiredService<ILogger<ILogger>>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSwagger();

    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    try
    {
        await app.StartAsync();

        logger.LogInformation("started");

        await app.WaitForShutdownAsync();

        logger.LogInformation("stopped");
    }
    catch (Exception e)
    {
        logger.LogInformation(e, "startup failed");
        throw;
    }
}

string REQUIRE(string key)
{
    var value = config.GetValue<string>(key);

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new Exception($"Missing configuration value for key '{key}'");
    }

    return value;
}

void AddCointMarket()
{
    var coinmarketcapBaseUrl = REQUIRE("CoinmarketcapBaseUrl");
    var coinmarketcapApiKey = REQUIRE("CoinmarketcapApiKey");

    builder.Services.AddHttpClient(CoinMarketClient.HTTP_CLIENT_NAME, q =>
    {
        q.BaseAddress = new Uri(coinmarketcapBaseUrl);
        q.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", coinmarketcapApiKey);
    });

    builder.Services.AddScoped(q => new CoinMarketClient(
        q.GetRequiredService<IHttpClientFactory>().CreateClient(CoinMarketClient.HTTP_CLIENT_NAME),
        logger: q.GetRequiredService<ILogger<CoinMarketClient>>(),
        appCache: q.GetRequiredService<IAppCache>(),
        freeVersion: true
    ));

    builder.Services.AddScoped<ICoinMarketClient>(q => q.GetRequiredService<CoinMarketClient>());
}