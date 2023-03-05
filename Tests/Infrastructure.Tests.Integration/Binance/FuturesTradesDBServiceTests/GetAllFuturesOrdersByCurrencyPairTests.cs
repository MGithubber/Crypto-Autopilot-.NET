﻿using Infrastructure.Tests.Integration.Binance.FuturesTradesDBServiceTests.Base;

namespace Infrastructure.Tests.Integration.Binance.FuturesTradesDBServiceTests;

public class GetAllFuturesOrdersByCurrencyPairTests : FuturesTradesDBServiceTestsBase
{
    [Test]
    public async Task GetAllFuturesOrdersByCurrencyPair_ShouldReturnAllFuturesOrdersWithCurrencyPair_WhenFuturesOrdersWithCurrencyPairExist()
    {
        // Arrange
        var currencyPair = this.CurrencyPairGenerator.Generate();
        var matchingCandlestick = this.CandlestickGenerator.Clone().RuleFor(c => c.CurrencyPair, currencyPair).Generate();
        var matchingFuturesOrders = this.FuturesOrderGenerator.Clone().RuleFor(o => o.Symbol, matchingCandlestick.CurrencyPair.Name).Generate(15);
        await InsertOneCandlestickAndMultipleFuturesOrdersAsync(matchingCandlestick, matchingFuturesOrders);

        for (var i = 0; i < 10; i++)
        {
            var randomCandlestick = this.CandlestickGenerator.Clone().RuleFor(c => c.CurrencyPair, f => GetRandomCurrencyPairExcept(f, currencyPair)).Generate();
            var randomFuturesOrders = this.FuturesOrderGenerator.Clone().RuleFor(o => o.Symbol, randomCandlestick.CurrencyPair.Name).Generate(15);
            await InsertOneCandlestickAndMultipleFuturesOrdersAsync(randomCandlestick, randomFuturesOrders);
        }


        // Act
        var retrievedFuturesOrders = await this.SUT.GetFuturesOrdersByCurrencyPairAsync(currencyPair.Name);

        // Assert
        retrievedFuturesOrders.Should().BeEquivalentTo(matchingFuturesOrders);
    }

    [Test]
    public async Task GetAllFuturesOrdersByCurrencyPair_ShouldReturnEmptyEnumerable_WhenNoFuturesOrdersWithCurrencyPairExist()
    {
        // Arrange
        var currencyPair = this.CurrencyPairGenerator.Generate();

        for (var i = 0; i < 10; i++)
        {
            var randomCandlestick = this.CandlestickGenerator.Clone().RuleFor(c => c.CurrencyPair, f => GetRandomCurrencyPairExcept(f, currencyPair)).Generate();
            var randomFuturesOrders = this.FuturesOrderGenerator.Clone().RuleFor(o => o.Symbol, randomCandlestick.CurrencyPair.Name).Generate(15);
            await InsertOneCandlestickAndMultipleFuturesOrdersAsync(randomCandlestick, randomFuturesOrders);
        }


        // Act
        var retrievedFuturesOrders = await this.SUT.GetFuturesOrdersByCurrencyPairAsync(currencyPair.Name);

        // Assert
        retrievedFuturesOrders.Should().BeEmpty();
    }
}