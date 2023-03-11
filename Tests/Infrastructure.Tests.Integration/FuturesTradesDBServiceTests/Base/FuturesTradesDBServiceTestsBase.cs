using Bybit.Net.Enums;

using Domain.Models;

using Infrastructure.Database.Contexts;
using Infrastructure.Services;
using Infrastructure.Tests.Integration.Common;

using Microsoft.EntityFrameworkCore;

using Respawn;

namespace Infrastructure.Tests.Integration.FuturesTradesDBServiceTests.Base;

public abstract class FuturesTradesDBServiceTestsBase
{
    protected readonly string ConnectionString = new SecretsManager().GetConnectionString("TradingHistoryDB-TestDatabase");

    protected FuturesTradesDBService SUT;
    protected FuturesTradingDbContext DbContext;


    protected readonly Faker Faker = new Faker();

    protected readonly Faker<CurrencyPair> CurrencyPairGenerator = new Faker<CurrencyPair>()
        .CustomInstantiator(f => new CurrencyPair(f.Finance.Currency().Code, f.Finance.Currency().Code));


    
    protected const string PositionSideLong = "Long";
    protected const string PositionSideShort = "Short";

    protected readonly Faker<FuturesPosition> FuturesPositionsGenerator = new Faker<FuturesPosition>()
        .RuleFor(p => p.CryptoAutopilotId, f => Guid.NewGuid())
        .RuleFor(p => p.CurrencyPair, f => new CurrencyPair("BTC", "USDT"))
        .RuleFor(p => p.Margin, f => f.Random.Decimal(1, 1000))
        .RuleFor(p => p.Leverage, f => f.Random.Decimal(0, 100))
        .RuleFor(p => p.EntryPrice, f => f.Random.Decimal(5000, 15000))
        .RuleSet(PositionSideLong, set =>
        {
            set.RuleFor(p => p.Side, PositionSide.Buy);
            set.RuleFor(p => p.Quantity, (_, p) => p.Margin * p.Leverage / p.EntryPrice);
            set.RuleFor(p => p.ExitPrice, (f, p) => f.Random.Decimal(p.EntryPrice, p.EntryPrice + 3000));
        })
        .RuleSet(PositionSideShort, set =>
        {
            set.RuleFor(p => p.Side, PositionSide.Sell);
            set.RuleFor(p => p.Quantity, (_, p) => p.Margin * p.Leverage / p.EntryPrice);
            set.RuleFor(p => p.ExitPrice, (f, p) => f.Random.Decimal(p.EntryPrice, p.EntryPrice - 3000));
        });
    

    protected const string LimitOrder = "LimitOrder";
    protected const string MarketOrder = "MarketOrder";
    protected const string StatusCreated = "StatusCreated";
    protected const string SideBuy = "SideBuy";
    protected const string SideSell = "SideSell";
    protected const string OrderPositionLong = "PositionLong";
    protected const string OrderPositionShort = "PositionShort";
    
    protected readonly Faker<FuturesOrder> FuturesOrderGenerator = new Faker<FuturesOrder>()
        .RuleFor(o => o.BybitID, f => Guid.NewGuid())
        .RuleFor(o => o.CurrencyPair, f => new CurrencyPair("BTC", "USDT"))
        .RuleFor(o => o.CreateTime, f => f.Date.Past(1))
        .RuleFor(o => o.UpdateTime, (f, p) => p.CreateTime.AddHours(f.Random.Int(0, 12)))
        .RuleFor(o => o.Price, f => f.Random.Decimal(5000, 15000))
        .RuleFor(o => o.Quantity, f => f.Random.Decimal(100, 300))
        .RuleFor(o => o.Status, OrderStatus.Created)
        .RuleSet(LimitOrder, set =>
        {
            set.RuleFor(o => o.Type, OrderType.Limit);
            set.RuleFor(o => o.TimeInForce, TimeInForce.GoodTillCanceled);
        })
        .RuleSet(MarketOrder, set =>
        {
            set.RuleFor(o => o.Type, OrderType.Market);
            set.RuleFor(o => o.TimeInForce, TimeInForce.ImmediateOrCancel);
        })
        .RuleSet(StatusCreated, set => set.RuleFor(o => o.Status, OrderStatus.Created))
        .RuleSet(SideBuy, set =>
        {
            set.RuleFor(o => o.Side, f => OrderSide.Buy);
            set.RuleFor(o => o.StopLoss, (f, p) => f.Random.Decimal(p.Price, p.Price - 3000));
            set.RuleFor(o => o.TakeProfit, (f, p) => f.Random.Decimal(p.Price, p.Price + 3000));
        })
        .RuleSet(SideSell, set =>
        {
            set.RuleFor(o => o.Side, f => OrderSide.Sell);
            set.RuleFor(o => o.StopLoss, (f, p) => f.Random.Decimal(p.Price, p.Price + 3000));
            set.RuleFor(o => o.TakeProfit, (f, p) => f.Random.Decimal(p.Price, p.Price - 3000));
        })
        .RuleSet(OrderPositionLong, set =>
        {
            set.RuleFor(o => o.PositionSide, f => PositionSide.Buy);
        })
        .RuleSet(OrderPositionShort, set =>
        {
            set.RuleFor(o => o.PositionSide, f => PositionSide.Sell);
        });
    

    
    private Respawner DbRespawner;
    protected async Task ClearDatabaseAsync() => await this.DbRespawner.ResetAsync(this.ConnectionString);


    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(this.ConnectionString);
        this.DbContext = new FuturesTradingDbContext(optionsBuilder.Options);

        await this.DbContext.Database.MigrateAsync();

        this.SUT = new FuturesTradesDBService(this.DbContext);

        this.DbRespawner = await Respawner.CreateAsync(this.ConnectionString, new RespawnerOptions { CheckTemporalTables = true });
        await this.ClearDatabaseAsync(); // in case the test database already exists and is populated
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await this.DbContext.Database.EnsureDeletedAsync();
    }


    [TearDown]
    public virtual async Task TearDown() => await this.ClearDatabaseAsync();
}
