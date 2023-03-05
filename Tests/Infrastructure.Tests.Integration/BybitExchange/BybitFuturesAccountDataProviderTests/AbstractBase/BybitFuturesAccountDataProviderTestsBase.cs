﻿using Application.Interfaces.Services.Trading.BybitExchange;

using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects;
using Bybit.Net.Objects.Models;

using CryptoExchange.Net.Authentication;

using CryptoExchange.Net.Objects;

using Domain.Models;

using Infrastructure.Services.General;
using Infrastructure.Services.Trading.BybitExchange;
using Infrastructure.Tests.Integration.Common;

namespace Infrastructure.Tests.Integration.BybitExchange.BybitFuturesAccountDataProviderTests.AbstractBase;

public abstract class BybitFuturesAccountDataProviderTestsBase
{
    private readonly SecretsManager SecretsManager = new SecretsManager();

    protected readonly CurrencyPair CurrencyPair = new CurrencyPair("BTC", "USDT");

    protected readonly IBybitFuturesAccountDataProvider SUT;
    protected readonly IBybitUsdFuturesTradingApi TradingClient;
    protected readonly IBybitUsdFuturesDataProvider FuturesDataProvider;
    
    public BybitFuturesAccountDataProviderTestsBase()
    {
        var bybitClient = new BybitClient(new BybitClientOptions
        {
            UsdPerpetualApiOptions = new RestApiClientOptions
            {
                ApiCredentials = new ApiCredentials(this.SecretsManager.GetSecret("BybitTestnetApiCredentials:key"), this.SecretsManager.GetSecret("BybitTestnetApiCredentials:secret")),
                BaseAddress = "https://api-testnet.bybit.com"
            }
        });

        this.TradingClient = new BybitUsdFuturesTradingApi(bybitClient.UsdPerpetualApi.Trading);
        this.SUT = new BybitFuturesAccountDataProvider(bybitClient.UsdPerpetualApi.Account);
        this.FuturesDataProvider = new BybitUsdFuturesDataProvider(new DateTimeProvider(), bybitClient.UsdPerpetualApi.ExchangeData);
    }


    //// //// //// ////


    private readonly List<BybitUsdPerpetualOrder> Orders = new();


    [TearDown]
    public async Task TearDown()
    {
        await Parallel.ForEachAsync(this.Orders, async (order, _) => await this.TradingClient.CloseOrderAsync(order));
        this.Orders.Clear();
    }


    protected async Task<BybitUsdPerpetualOrder> TradingClient_PlaceOrderAsync(string symbol, OrderSide side, OrderType type, decimal quantity, TimeInForce timeInForce, bool reduceOnly, bool closeOnTrigger, decimal? price = null, string? clientOrderId = null, decimal? takeProfitPrice = null, decimal? stopLossPrice = null, TriggerType? takeProfitTriggerType = null, TriggerType? stopLossTriggerType = null, PositionMode? positionMode = null, long? receiveWindow = null)
    {
        var perpetualOrder = await this.TradingClient.PlaceOrderAsync(symbol, side, type, quantity, timeInForce, reduceOnly, closeOnTrigger, price, clientOrderId, takeProfitPrice, stopLossPrice, takeProfitTriggerType, stopLossTriggerType, positionMode, receiveWindow);
        this.Orders.Add(perpetualOrder);
        return perpetualOrder;
    }
}
