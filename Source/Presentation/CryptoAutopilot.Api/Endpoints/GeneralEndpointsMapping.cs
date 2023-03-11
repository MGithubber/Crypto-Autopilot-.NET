using Application.Interfaces.Services;
using Application.Interfaces.Services.Bybit;

using Bybit.Net.Enums;

using CryptoAutopilot.Api.Contracts.Responses.Common;
using CryptoAutopilot.Api.Contracts.Responses.MarketData;
using CryptoAutopilot.Api.Contracts.Responses.Strategies;
using CryptoAutopilot.Api.Endpoints.Internal;
using CryptoAutopilot.Api.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace CryptoAutopilot.Api.Endpoints;

public static partial class ServicesEndpointsExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("MarketData/ContractCandlesticks", async ([FromServices] IBybitUsdFuturesMarketDataProvider provider, [FromQuery] string currencyPair, int timeframeMinutes) =>
        {
            var timeframe = (KlineInterval)(timeframeMinutes * 60);
            var klines = await provider.GetAllCandlesticksAsync(currencyPair, timeframe);
            var candlesticks = klines.Select(x => new CandlestickResponse
            {
                CurrencyPair = x.Symbol,
                Date = x.OpenTime,
                Open = x.OpenPrice,
                High = x.HighPrice,
                Low = x.LowPrice,
                Close = x.ClosePrice,
                Volume = x.Volume
            });
            
            var response = new GetContractCandlesticksResponse
            {
                CurrencyPair = currencyPair,
                Timeframe = timeframe,
                Candlesticks = candlesticks
            };

            return Results.Ok(response);
        });


        app.MapGet("strategies", ([FromServices] IStrategiesTracker StrategiesTracker, Guid? guid, IServiceProvider services) =>
        {
            if (guid is null)
            {
                var strategies = StrategiesTracker.GetAll();
                var responses = strategies.Select(StrategyEngineToResponse);
                var response = new GetAllStrategyEnginesResponse { Strategies = responses };
                return Results.Ok(response);
            }
            else
            {
                var strategy = StrategiesTracker.Get(guid.Value);
                if (strategy is null)
                    return Results.NotFound();

                var response = StrategyEngineToResponse(strategy);
                return Results.Ok(response);
            }
        }).WithTags("Strategies");

        app.MapDelete($"StopStrategy/{{guid}}", async ([FromServices] IStrategiesTracker StrategiesTracker, Guid guid, IServiceProvider services) =>
        {
            var strategy = StrategiesTracker.Get(guid);
            if (strategy is null)
                return Results.NotFound();

            return await strategy.TryAwaitShutdownAsync(services, TimeSpan.FromSeconds(15));
        }).WithTags("Strategies");
    }
    private static GetStrategyEngineResponse StrategyEngineToResponse(IStrategyEngine strategy) => new GetStrategyEngineResponse
    {
        Guid = strategy.Guid,
        StartedStrategyTypeName = strategy.GetType().Name,
        IsRunning = strategy.IsRunning(),
    };
}
