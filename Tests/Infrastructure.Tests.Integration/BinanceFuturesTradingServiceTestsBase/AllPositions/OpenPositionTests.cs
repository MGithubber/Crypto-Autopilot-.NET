﻿using Binance.Net.Enums;

using Infrastructure.Tests.Integration.BinanceFuturesTradingServiceTestsBase.Base;

namespace Infrastructure.Tests.Integration.BinanceFuturesTradingServiceTestsBase.AllPositions;

public class OpenPositionTests : Base.BinanceFuturesTradingServiceTestsBase
{
    [Test]
    public async Task OpenPosition_ShouldThrow_WhenPositionIsAlreadyOpen()
    {
        // Arrange
        await this.SUT.PlaceMarketOrderAsync((OrderSide)Random.Shared.Next(2), this.Margin);
        
        // Act
        var func = async () => await this.SUT.PlaceMarketOrderAsync(OrderSide.Buy, this.Margin);

        // Assert
        await func.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("A position is open already");
    }
}
