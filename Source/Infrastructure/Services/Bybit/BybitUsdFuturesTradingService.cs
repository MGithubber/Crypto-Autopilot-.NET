using Application.Exceptions;
using Application.Interfaces.Services.Bybit;

using Bybit.Net.Enums;
using Bybit.Net.Objects.Models;

using Domain.Models;

using Infrastructure.Extensions.Bybit;
using Infrastructure.Notifications;

using MediatR;

namespace Infrastructure.Services.Bybit;

public class BybitUsdFuturesTradingService : IBybitUsdFuturesTradingService
{
    public CurrencyPair CurrencyPair { get; }
    public decimal Leverage { get; }

    private readonly IBybitFuturesAccountDataProvider FuturesAccount;
    private readonly IBybitUsdFuturesMarketDataProvider MarketDataProvider;
    private readonly IBybitUsdFuturesTradingApiClient TradingClient;
    private readonly IMediator Mediator;

    public BybitUsdFuturesTradingService(CurrencyPair currencyPair, decimal leverage, IBybitFuturesAccountDataProvider futuresAccount, IBybitUsdFuturesMarketDataProvider marketDataProvider, IBybitUsdFuturesTradingApiClient tradingClient, IMediator mediator)
    {
        if (leverage is <= 0 or > 100)
            throw new ArgumentException("The leverage has to be between 0 and 100 inclusive", nameof(leverage));

        this.CurrencyPair = currencyPair ?? throw new ArgumentNullException(nameof(currencyPair));
        this.Leverage = leverage;
        this.FuturesAccount = futuresAccount ?? throw new ArgumentNullException(nameof(futuresAccount));
        this.MarketDataProvider = marketDataProvider ?? throw new ArgumentNullException(nameof(marketDataProvider));
        this.TradingClient = tradingClient ?? throw new ArgumentNullException(nameof(tradingClient));
        this.Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }


    //// //// ////

    private readonly IDictionary<PositionSide, Guid?> PositionGuids = new Dictionary<PositionSide, Guid?>()
    {
        { PositionSide.Buy, null },
        { PositionSide.Sell, null }
    };
    private readonly IDictionary<PositionSide, BybitPositionUsd?> Positions = new Dictionary<PositionSide, BybitPositionUsd?>()
    {
        { PositionSide.Buy, null },
        { PositionSide.Sell, null }
    };
    public BybitPositionUsd? LongPosition => this.Positions[PositionSide.Buy];
    public BybitPositionUsd? ShortPosition => this.Positions[PositionSide.Sell];


    private readonly IDictionary<OrderSide, BybitUsdPerpetualOrder?> LimitOrders = new Dictionary<OrderSide, BybitUsdPerpetualOrder?>()
    {
        { OrderSide.Buy, null },
        { OrderSide.Sell, null }
    };
    public BybitUsdPerpetualOrder? BuyLimitOrder => this.LimitOrders[OrderSide.Buy];
    public BybitUsdPerpetualOrder? SellLimitOrder => this.LimitOrders[OrderSide.Sell];


    public async Task OpenPositionAsync(PositionSide positionSide, decimal Margin, decimal? StopLoss = null, decimal? TakeProfit = null, TriggerType tradingStopTriggerType = TriggerType.LastPrice)
    {
        if (this.Positions[positionSide] is not null)
            throw new InvalidOrderException($"There is a {positionSide} position open already");


        var lastPrice = await this.MarketDataProvider.GetLastPriceAsync(this.CurrencyPair.Name);
        var quantity = Math.Round(Margin * this.Leverage / lastPrice, 2);

        var order = await this.TradingClient.PlaceOrderAsync(this.CurrencyPair.Name,
                                                             positionSide.GetEntryOrderSide(),
                                                             OrderType.Market,
                                                             quantity,
                                                             TimeInForce.ImmediateOrCancel,
                                                             false,
                                                             false,
                                                             stopLossPrice: StopLoss,
                                                             stopLossTriggerType: tradingStopTriggerType,
                                                             takeProfitPrice: TakeProfit,
                                                             takeProfitTriggerType: tradingStopTriggerType,
                                                             positionMode: positionSide.ToToPositionMode());

        var position = await this.FuturesAccount.GetPositionAsync(this.CurrencyPair.Name, positionSide);

        this.PositionGuids[positionSide] = Guid.NewGuid();
        this.Positions[positionSide] = position;

        _ = this.Mediator.Publish(new PositionOpenedNotification
        {
            Position = position!.ToDomainObject(this.PositionGuids[positionSide]!.Value),
            FuturesOrders = new[] { order.ToDomainObject(positionSide) },
        });
    }
    public async Task ModifyTradingStopAsync(PositionSide positionSide, decimal? newStopLoss = null, decimal? newTakeProfit = null, TriggerType newTradingStopTriggerType = TriggerType.LastPrice)
    {
        var position = this.Positions[positionSide] ?? throw new InvalidOrderException($"No open {positionSide} was found");

        await this.TradingClient.SetTradingStopAsync(this.CurrencyPair.Name,
                                                     positionSide,
                                                     stopLossPrice: newStopLoss,
                                                     stopLossQuantity: position.Quantity,
                                                     stopLossTriggerType: newTradingStopTriggerType,
                                                     takeProfitPrice: newTakeProfit,
                                                     takeProfitQuantity: position.Quantity,
                                                     takeProfitTriggerType: newTradingStopTriggerType,
                                                     positionMode: position.PositionMode);

        var updatedPosition = await this.FuturesAccount.GetPositionAsync(this.CurrencyPair.Name, positionSide);
        this.Positions[positionSide] = updatedPosition;
         
        var cryptoAutopilotId = this.PositionGuids[positionSide]!.Value;
        _ = this.Mediator.Publish(new PositionUpdatedNotification
        {
            PositionCryptoAutopilotId = cryptoAutopilotId,
            UpdatedPosition = updatedPosition!.ToDomainObject(cryptoAutopilotId)
        });
    }
    public async Task ClosePositionAsync(PositionSide positionSide)
    {
        var position = this.Positions[positionSide] ?? throw new InvalidOrderException($"No open {positionSide} position was found");

        var order = await this.TradingClient.PlaceOrderAsync(this.CurrencyPair.Name,
                                                             positionSide.GetClosingOrderSide(),
                                                             OrderType.Market,
                                                             position.Quantity,
                                                             TimeInForce.ImmediateOrCancel,
                                                             false,
                                                             false,
                                                             positionMode: position.PositionMode);

        var cryptoAutopilotId = this.PositionGuids[positionSide]!.Value;
        _ = this.Mediator.Publish(new PositionUpdatedNotification
        {
            PositionCryptoAutopilotId = cryptoAutopilotId,
            UpdatedPosition = this.Positions[positionSide]!.ToDomainObject(cryptoAutopilotId, order.Price)
        });

        this.Positions[positionSide] = null;
    }


    public async Task PlaceLimitOrderAsync(OrderSide orderSide, decimal LimitPrice, decimal Margin, decimal? StopLoss = null, decimal? TakeProfit = null, TriggerType tradingStopTriggerType = TriggerType.LastPrice)
    {
        if (this.LimitOrders[orderSide] is not null)
            throw new InvalidOrderException($"There is an open {orderSide} limit order already");


        var quantity = Math.Round(Margin * this.Leverage / LimitPrice, 2);

        var order = this.LimitOrders[orderSide] = await this.TradingClient.PlaceOrderAsync(this.CurrencyPair.Name,
                                                                                           orderSide,
                                                                                           OrderType.Limit,
                                                                                           quantity,
                                                                                           TimeInForce.GoodTillCanceled,
                                                                                           false,
                                                                                           false,
                                                                                           price: LimitPrice,
                                                                                           stopLossPrice: StopLoss,
                                                                                           stopLossTriggerType: tradingStopTriggerType,
                                                                                           takeProfitPrice: TakeProfit,
                                                                                           takeProfitTriggerType: tradingStopTriggerType,
                                                                                           positionMode: orderSide.ToPositionMode());

        this.LimitOrders[orderSide] = order;
        
        _ = this.Mediator.Publish(new LimitOrderPlacedNotification
        {
            LimitOrder = order.ToDomainObject(orderSide.ToPositionSide())
        });
    }
    public async Task ModifyLimitOrderAsync(OrderSide orderSide, decimal newLimitPrice, decimal newMargin, decimal? newStopLoss = null, decimal? newTakeProfit = null, TriggerType newTradingStopTriggerType = TriggerType.LastPrice)
    {
        var oldLimitOrder = this.LimitOrders[orderSide] ?? throw new InvalidOrderException($"No open {orderSide} limit order was found");

        ValidateNewTradingStopParameters(orderSide, newLimitPrice, newStopLoss, newTakeProfit);

        var newQuantity = Math.Round(newMargin * this.Leverage / newLimitPrice, 2);

        await TradingClient.ModifyOrderAsync(this.CurrencyPair.Name,
                                             oldLimitOrder.Id,
                                             newPrice: newLimitPrice,
                                             newQuantity: newQuantity,
                                             stopLossPrice: newStopLoss,
                                             stopLossTriggerType: newTradingStopTriggerType,
                                             takeProfitPrice: newTakeProfit,
                                             takeProfitTriggerType: newTradingStopTriggerType);
        
        var order = await this.TradingClient.GetOrderAsync(this.CurrencyPair.Name, oldLimitOrder.Id);
        _ = this.Mediator.Publish(new UpdatedLimitOrderNotification
        {
            BybitId = Guid.Parse(order.Id),
            UpdatedLimitOrder = order.ToDomainObject(orderSide.ToPositionSide())
        });

        this.LimitOrders[orderSide] = order;
    }
    public async Task CancelLimitOrderAsync(OrderSide orderSide)
    {
        var limitOrder = this.LimitOrders[orderSide] ?? throw new InvalidOrderException($"No open {orderSide} limit order was found");
        await this.TradingClient.CancelOrderAsync(this.CurrencyPair.Name, limitOrder.Id);
        
        _ = this.Mediator.Publish(new CancelledLimitOrderNotification
        {
            BybitId = Guid.Parse(this.LimitOrders[orderSide]!.Id)
        });

        this.LimitOrders[orderSide] = null;
    }

    private static void ValidateNewTradingStopParameters(OrderSide orderSide, decimal newLimitPrice, decimal? newStopLoss, decimal? newTakeProfit)
    {
        if (orderSide == OrderSide.Buy && (newStopLoss > newLimitPrice || newTakeProfit < newLimitPrice))
            throw new InternalTradingServiceException("Incorrect trading stop parameters for updating limit Buy order");

        if (orderSide == OrderSide.Sell && (newStopLoss < newLimitPrice || newTakeProfit > newLimitPrice))
            throw new InternalTradingServiceException("Incorrect trading stop parameters for updating limit Sell order");
    }
}
