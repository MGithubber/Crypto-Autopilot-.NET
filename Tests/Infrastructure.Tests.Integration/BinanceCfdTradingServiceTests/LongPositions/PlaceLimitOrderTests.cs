﻿using Application.Exceptions;

using Binance.Net.Enums;

using Infrastructure.Tests.Integration.BinanceCfdTradingServiceTests.Base;

namespace Infrastructure.Tests.Integration.BinanceCfdTradingServiceTests.LongPositions;

public class PlaceLimitOrderTests : BinanceCfdTradingServiceTestsBase
{
    [Test]
    public async Task PlaceLimitOrderAsync_ShouldPlaceLimitOrder_WhenInputIsCorrect()
    {
        // Arrange
        var current_price = await this.SUT.GetCurrentPriceAsync();
        var limitPrice = current_price - 50;
        var stopLoss = limitPrice - 25;
        var takeProfit = limitPrice + 25;

        // Act
        var placedOrder = await this.SUT.PlaceLimitOrderAsync(OrderSide.Buy, limitPrice, this.testMargin, stopLoss, takeProfit);
        
        // Assert
        this.SUT.IsInPosition().Should().BeFalse();
        placedOrder.Price.Should().Be(limitPrice);
    }
    
    [Test]
    public async Task PlaceLimitOrderAsync_ShouldThrow_WhenLimitPriceInputIsIncorrect()
    {
        // Arrange
        var current_price = await this.SUT.GetCurrentPriceAsync();
        var limitPrice = current_price + 50;
        var stopLoss = limitPrice - 25;
        var takeProfit = limitPrice + 25;

        // Act
        var func = async () => await this.SUT.PlaceLimitOrderAsync(OrderSide.Buy, limitPrice, this.testMargin, stopLoss, takeProfit);

        // Assert
        await func.Should().ThrowExactlyAsync<InvalidOrderException>().WithMessage("The limit price for a buy order can't be greater than the current price");
    }

    [Test]
    public async Task PlaceLimitOrderAsync_ShouldThrow_WhenTpSlInputIsIncorrect()
    {
        // Arrange
        var current_price = await this.SUT.GetCurrentPriceAsync();
        var limitPrice = current_price - 50;
        var stopLoss = limitPrice + 25;
        var takeProfit = limitPrice - 25;
        
        // Act
        var func = async () => await this.SUT.PlaceLimitOrderAsync(OrderSide.Buy, limitPrice, this.testMargin, stopLoss, takeProfit);
        
        // Assert
        await func.Should().ThrowExactlyAsync<InvalidOrderException>();
    }
}
