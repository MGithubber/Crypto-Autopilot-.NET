﻿using Application.Exceptions;

using Binance.Net.Enums;

namespace Infrastructure.Tests.Integration.BinanceFuturesTradingServiceTestsBase.ShortPositions;

public class PlaceLimitOrderTests : Base.BinanceFuturesTradingServiceTestsBase
{
    [Test]
    public async Task PlaceLimitOrderAsync_ShouldPlaceLimitOrder_WhenInputIsCorrect()
    {
        // Arrange
        var current_price = await this.MarketDataProvider.GetCurrentPriceAsync(this.CurrencyPair.Name);
        var limitPrice = current_price + 50;
        var stopLoss = limitPrice + 25;
        var takeProfit = limitPrice - 25;
        
        // Act
        var order = await this.SUT_PlaceLimitOrderAsync(OrderSide.Sell, limitPrice, this.Margin, stopLoss, takeProfit);
        
        // Assert
        order.Price.Should().Be(limitPrice);
        this.SUT.IsInPosition().Should().BeFalse();
        this.SUT.LimitOrder!.Price.Should().Be(limitPrice);
        this.SUT.LimitOrder!.Side.Should().Be(OrderSide.Sell);
        this.SUT.LimitOrder!.PositionSide.Should().Be(PositionSide.Short);
    }

    [Test]
    public async Task PlaceLimitOrderAsync_ShouldThrow_WhenLimitPriceInputIsIncorrect()
    {
        // Arrange
        var current_price = await this.MarketDataProvider.GetCurrentPriceAsync(this.CurrencyPair.Name);
        var limitPrice = current_price - 50;
        var stopLoss = limitPrice + 25;
        var takeProfit = limitPrice - 25;

        // Act
        var func = async () => await this.SUT_PlaceLimitOrderAsync(OrderSide.Sell, limitPrice, this.Margin, stopLoss, takeProfit);

        // Assert
        await func.Should().ThrowExactlyAsync<InvalidOrderException>().WithMessage("The limit price for a sell order can't be less greater than the current price");
    }

    [Test]
    public async Task PlaceLimitOrderAsync_ShouldThrow_WhenTpSlInputIsIncorrect()
    {
        // Arrange
        var current_price = await this.MarketDataProvider.GetCurrentPriceAsync(this.CurrencyPair.Name);
        var limitPrice = current_price - 50;
        var stopLoss = limitPrice + 25;
        var takeProfit = limitPrice - 25;

        // Act
        var func = async () => await this.SUT_PlaceLimitOrderAsync(OrderSide.Sell, limitPrice, this.Margin, stopLoss, takeProfit);

        // Assert
        await func.Should().ThrowExactlyAsync<InvalidOrderException>();
    }
}
