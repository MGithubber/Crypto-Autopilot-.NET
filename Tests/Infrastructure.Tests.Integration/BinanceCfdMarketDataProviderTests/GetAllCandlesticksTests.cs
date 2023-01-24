﻿using Binance.Net.Enums;

using Domain.Models;

using Infrastructure.Tests.Integration.BinanceCfdMarketDataProviderTests.Base;

namespace Infrastructure.Tests.Integration.BinanceCfdMarketDataProviderTests;

[Parallelizable(ParallelScope.All)]
public class GetAllCandlesticksTests : BinanceCfdMarketDataProviderTestsBase
{
    [Test, TestCaseSource(nameof(GetValidKlineIntervals))]
    public async Task GetAllCandlesticksAsync_ShouldReturnAllCandlesticks_WhenTimeframeIsValid(KlineInterval timeframe)
    {
        // Act
        var candlesticks = await SUT.GetAllCandlesticksAsync(this.CurrencyPair.Name, timeframe);

        // Assert
        candlesticks.Last().Date.Add(TimeSpan.FromSeconds((int)timeframe)).Should().BeAfter(DateTime.UtcNow);
        base.AreCandlesticksTimelyConsistent(candlesticks, timeframe).Should().BeTrue();
    }


    [Test, TestCaseSource(nameof(GetInvalidKlineIntervals))]
    public async Task GetAllCandlesticksAsync_ShouldThrow_WhenTimeframeIsUnsupported(KlineInterval timeframe)
    {
        // Act
        var func = async () => await SUT.GetAllCandlesticksAsync(this.CurrencyPair.Name, timeframe);

        // Assert
        await func.Should().ThrowExactlyAsync<NotSupportedException>().WithMessage($"The {timeframe} timeframe is not supported");
    }
}
