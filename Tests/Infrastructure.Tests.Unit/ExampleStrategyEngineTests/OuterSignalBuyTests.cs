﻿using Binance.Net.Enums;

using Infrastructure.Notifications;
using Infrastructure.Strategies.Example.Enums;
using Infrastructure.Tests.Unit.ExampleStrategyEngineTests.Base;

namespace Infrastructure.Tests.Unit.ExampleStrategyEngineTests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public class OuterSignalBuyTests : ExampleStrategyEngineTestsBase
{
    [Test]
    public async Task OuterSignalBuy_ShouldTriggerPositionOpening_WhenPriceIsAboveEmaAndTraderIsNotInPosition()
    {
        // Arrange
        this.ArrangeFor_OuterSignalBuy_ShouldTriggerPositionOpening_WhenPriceIsAboveEmaAndTraderIsNotInPosition(out var currentPrice, out var emaPrice);
        
        decimal stopLoss = emaPrice;
        decimal takeProfit = currentPrice + (currentPrice - emaPrice) * this.RiskRewardRatio;

        // Act
        this.SUT.FlagDivergence(RsiDivergence.Bullish);
        await this.SUT.MakeMoveAsync();

        // Assert
        await this.FuturesTrader.Received(1).OpenPositionAtMarketPriceAsync(OrderSide.Buy, this.Margin, stopLoss, takeProfit);
        await this.Mediator.Received(1).Publish(Arg.Any<PositionOpenedNotification>());
    }

    [Test]
    public async Task OuterSignalBuy_ShouldNotTriggerPositionOpening_WhenTraderIsAlreadyInPosition()
    {
        // Arrange
        this.ArrangeFor_OuterSignalBuy_ShouldNotTriggerPositionOpening_WhenTraderIsAlreadyInPosition(out _, out _);

        // Act
        this.SUT.FlagDivergence(RsiDivergence.Bullish);
        await this.SUT.MakeMoveAsync();

        // Assert
        await this.FuturesTrader.DidNotReceive().OpenPositionAtMarketPriceAsync(Arg.Any<OrderSide>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        await this.Mediator.DidNotReceive().Publish(Arg.Any<PositionOpenedNotification>());
    }
    
    [Test]
    public async Task OuterSignalBuy_ShouldNotTriggerPositionOpening_WhenPriceIsNotAboveEma()
    {
        // Arrange
        this.ArrangeFor_OuterSignalBuy_ShouldNotTriggerPositionOpening_WhenPriceIsNotAboveEma(out _, out _);
        
        // Act
        this.SUT.FlagDivergence(RsiDivergence.Bullish);
        await this.SUT.MakeMoveAsync();

        // Assert
        await this.FuturesTrader.DidNotReceive().OpenPositionAtMarketPriceAsync(Arg.Any<OrderSide>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        await this.Mediator.DidNotReceive().Publish(Arg.Any<PositionOpenedNotification>());
    }
}
