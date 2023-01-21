﻿using Binance.Net.Enums;

using Infrastructure.Notifications;

namespace Infrastructure.Tests.Unit.SimpleStrategyEngineTests.ShortStrategy;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class CFDMovingDownTests : ShortStrategyEngineTestsBase
{
    [Test]
    public async Task CFDMovingDown_ShouldTriggerPositionOpening_WhenTraderIsNotInPosition()
    {
        // Arrange
        decimal currentPrice = this.Random.Next(1000, 3000);
        this.FuturesTrader.GetCurrentPriceAsync().Returns(currentPrice);
        this.FuturesTrader.IsInPosition().Returns(false, true);
        this.FuturesDataProvider.GetCompletedCandlesticksAsync(Arg.Any<KlineInterval>()).Returns(this.Candlesticks);

        // Act
        this.SUT.CFDMovingDown();
        await this.SUT.MakeMoveAsync();

        // Assert
        await this.FuturesTrader.Received(1).OpenPositionAtMarketPriceAsync(OrderSide.Sell, 20m, 1.01m * currentPrice, 0.99m * currentPrice);
        await this.Mediator.Received().Publish(Arg.Any<PositionOpenedNotification>());
        this.SUT.Signal.Should().BeNull();
    }
    
    [Test]
    public async Task CFDMovingDown_ShouldNotTriggerPositionOpening_WhenTraderIsAlreadyInPosition()
    {
        // Arrange
        await CFDMovingDown_ShouldTriggerPositionOpening_WhenTraderIsNotInPosition();

        // assume one more candlestick was created
        var candlestick = this.CandlestickGenerator.Generate();
        this.FuturesDataProvider.GetCompletedCandlesticksAsync(Arg.Any<KlineInterval>()).Returns(this.Candlesticks.Append(candlestick));

        this.FuturesTrader.ClearReceivedCalls();
        this.Mediator.ClearReceivedCalls();


        // Act
        this.SUT.CFDMovingDown();
        await this.SUT.MakeMoveAsync();


        // Assert
        await this.FuturesTrader.DidNotReceive().OpenPositionAtMarketPriceAsync(Arg.Any<OrderSide>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        await this.Mediator.DidNotReceive().Publish(Arg.Any<PositionOpenedNotification>());
        this.SUT.Signal.Should().BeNull();
    }

    [Test]
    public async Task CFDMovingDown_ShouldNotTriggerPositionOpening_WhenTraderIsInPosition()
    {
        // Arrange
        this.FuturesTrader.IsInPosition().Returns(true);
        
        // Act
        this.SUT.CFDMovingDown();
        await this.SUT.MakeMoveAsync();

        // Assert
        await this.FuturesTrader.DidNotReceive().OpenPositionAtMarketPriceAsync(Arg.Any<OrderSide>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        await this.Mediator.DidNotReceive().Publish(Arg.Any<PositionOpenedNotification>());
        this.SUT.Signal.Should().BeNull();
    }
}