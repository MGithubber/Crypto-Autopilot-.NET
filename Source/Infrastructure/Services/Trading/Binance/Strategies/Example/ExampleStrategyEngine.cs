﻿using Application.Interfaces.Services.Trading.Binance;
using Application.Interfaces.Services.Trading.Binance.Monitors;

using Binance.Net.Enums;

using Domain.Models;

using Generated;

using Infrastructure.Notifications;
using Infrastructure.Services.Trading.Binance.Strategies.Abstract;
using Infrastructure.Services.Trading.Binance.Strategies.Example.Enums;

using MediatR;

namespace Infrastructure.Services.Trading.Binance.Strategies.Example;

/// <summary>
/// An implementation of the <see cref="StrategyEngine"/> class that uses Relative Strength Index (RSI) divergence to determine and moving average exponential (EMA) market trend and open/close positions accordingly
/// </summary>
public class ExampleStrategyEngine : StrategyEngine
{
    protected readonly int EMALength;

    protected readonly decimal Margin;
    protected readonly decimal RiskRewardRatio;

    internal ExampleStrategyEngine(Guid guid, CurrencyPair currencyPair, KlineInterval klineInterval) : base(guid, currencyPair, klineInterval) { }
    public ExampleStrategyEngine(CurrencyPair currencyPair, KlineInterval klineInterval, int emaLength, decimal margin, decimal riskRewardRatio, IFuturesTradingService futuresTrader, IFuturesMarketDataProvider futuresDataProvider, IFuturesCandlesticksMonitor candlestickMonitor, IMediator mediator) : base(currencyPair, klineInterval, futuresTrader, futuresDataProvider, candlestickMonitor, mediator)
    {
        this.EMALength = emaLength;
        this.Margin = margin;
        this.RiskRewardRatio = riskRewardRatio;
        this.IndicatorsAdapter = new IndicatorsAdapter(this.Candlesticks);
    }

    private IList<Candlestick> Candlesticks = default!;
    internal IIndicatorsAdapter IndicatorsAdapter = default!;
    internal decimal Price;
    internal decimal EMA;

    internal override async Task MakeMoveAsync()
    {
        await this.CandlestickMonitor.WaitForNextCandlestickAsync(this.CurrencyPair.Name, ContractType.Perpetual, this.KlineInterval);


        await GetLatestMarketDataAsync();

        var BuyCondition = this.Divergence == RsiDivergence.Bullish && this.Price > this.EMA;
        var SellCondition = this.Divergence == RsiDivergence.Bearish;

        if (BuyCondition && !this.FuturesTrader.IsInPosition())
        {
            await this.OpenLongPositionAsync();
            this.Divergence = null;
        }
        else if (SellCondition && this.FuturesTrader.IsInPosition())
        {
            await this.ClosePositionAsync();
            this.Divergence = null;
        }
    }
    private async Task GetLatestMarketDataAsync()
    {
        this.Candlesticks = (await this.FuturesDataProvider.GetCompletedCandlesticksAsync(this.CurrencyPair.Name, this.KlineInterval)).ToList();
        this.Price = await this.FuturesDataProvider.GetCurrentPriceAsync(this.CurrencyPair.Name);
        this.EMA = Convert.ToDecimal(this.IndicatorsAdapter.GetEma(this.EMALength).Last().Ema);
    }
    private async Task OpenLongPositionAsync()
    {
        var stopLoss = this.EMA;
        var takeProfit = this.Price + (this.Price - this.EMA) * this.RiskRewardRatio;

        await this.FuturesTrader.PlaceMarketOrderAsync(OrderSide.Buy, this.Margin, stopLoss, takeProfit);
        await this.Mediator.Publish(new PositionOpenedNotification(this.Candlesticks.Last(), this.FuturesTrader.Position!));
    }
    private async Task ClosePositionAsync()
    {
        var closingOrder = await this.FuturesTrader.ClosePositionAsync();
        await this.Mediator.Publish(new PositionClosedNotification(this.Candlesticks.Last(), closingOrder));
    }


    // a null value indicates that there is no divergence or it has been consumed
    public RsiDivergence? Divergence { get; private set; }

    /// <summary>
    /// Informs the engine about a divergence that has occured in the market
    /// </summary>
    /// <param name="divergence">The divergence that has occured in the market</param>
    public void FlagDivergence(RsiDivergence divergence) => this.Divergence = divergence;
}
