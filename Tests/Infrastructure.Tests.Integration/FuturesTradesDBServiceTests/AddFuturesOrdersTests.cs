using System.Text;

using Application.Data.Mapping;

using Infrastructure.Tests.Integration.FuturesTradesDBServiceTests.Base;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Integration.FuturesTradesDBServiceTests;

public class AddFuturesOrdersTests : FuturesTradesDBServiceTestsBase
{
    [Test]
    public async Task AddFuturesOrdersWithoutPositionGuid_ShouldAddFuturesOrders_WhenAllOrdersShouldNotPointToPosition()
    {
        // Arrange
        var orders = this.FuturesOrderGenerator.Generate(10, $"default, {LimitOrder}, {SideBuy}");

        // Act
        await this.SUT.AddFuturesOrdersAsync(orders);

        // Assert
        this.DbContext.FuturesOrders.Select(x => x.ToDomainObject()).Should().BeEquivalentTo(orders);
    }

    [Test]
    public async Task AddFuturesOrdersWithoutPositionGuid_ShouldThrow_WhenAllOrdersRequirePosition()
    {
        // Arrange
        var orders = this.FuturesOrderGenerator.Generate(10, $"default, {MarketOrder}, {SideBuy}");

        // Act
        var func = async () => await this.SUT.AddFuturesOrdersAsync(orders);

        // Assert
        (await func.Should()
            .ThrowExactlyAsync<DbUpdateException>().WithMessage("An error occurred while saving the entity changes. See the inner exception for details."))
            .WithInnerExceptionExactly<ArgumentException>().WithMessage("A created market order or a filled limit order needs to be associated with a position and no position identifier has been specified.");
    }


    [Test]
    public async Task AddFuturesOrderWithPositionGuid_ShouldAddFuturesOrders_WhenAllFuturesOrdersRequirePosition()
    {
        // Arrange
        var orders = this.FuturesOrderGenerator.Generate(10, $"default, {MarketOrder}, {SideBuy}, {OrderPositionLong}");
        var position = this.FuturesPositionsGenerator.Generate($"default, {PositionSideLong}");

        var tran = await this.DbContext.Database.BeginTransactionAsync();
        await this.DbContext.FuturesPositions.AddAsync(position.ToDbEntity());
        await this.DbContext.SaveChangesAsync();
        await tran.CommitAsync();


        // Act
        await this.SUT.AddFuturesOrdersAsync(orders, position.CryptoAutopilotId);


        // Assert
        this.DbContext.FuturesOrders.Select(x => x.ToDomainObject()).Should().BeEquivalentTo(orders);
    }

    [Test]
    public async Task AddFuturesOrderWithPositionGuid_ShouldThrow_WhenAllFuturesOrdersShouldNotPointToPosition()
    {
        // Arrange
        var orders = this.FuturesOrderGenerator.Generate(10, $"default, {LimitOrder}, {SideBuy}, {OrderPositionLong}");
        var position = this.FuturesPositionsGenerator.Generate($"default, {PositionSideLong}");

        var tran = await this.DbContext.Database.BeginTransactionAsync();
        await this.DbContext.FuturesPositions.AddAsync(position.ToDbEntity());
        await this.DbContext.SaveChangesAsync();
        await tran.CommitAsync();


        // Act
        var func = async () => await this.SUT.AddFuturesOrdersAsync(orders, position.CryptoAutopilotId);


        // Assert
        (await func.Should()
            .ThrowExactlyAsync<DbUpdateException>().WithMessage("An error occurred while saving the entity changes. See the inner exception for details."))
            .WithInnerExceptionExactly<ArgumentException>().WithMessage("Only a created market order or a filled limit order can be associated with a position.");
    }


    [Test]
    public async Task AddFuturesOrder_ShouldThrow_WhenNotAllOrdersRequirePosition()
    {
        // Arrange
        var orders = new[]
        {
            this.FuturesOrderGenerator.Generate($"default, {MarketOrder}, {SideBuy}"),
            this.FuturesOrderGenerator.Generate($"default, {LimitOrder}, {SideBuy}"),
        };

        // Act
        var func = async () => await this.SUT.AddFuturesOrdersAsync(orders);

        // Assert
        var sb = new StringBuilder();
        sb.Append("Some of the specified orders can be associated with a position while some cannot. ");
        sb.Append("All the specified orders need to have the same requirements in terms of beeing associated with a position to add them in the database at once.");
        await func.Should().ThrowExactlyAsync<ArgumentException>().WithMessage($"{sb} (Parameter 'futuresOrders')");
    }

    [Test]
    public async Task AddFuturesOrder_ShouldThrow_WhenNotAllOrdersRequireTheSamePositionSide()
    {
        // Arrange
        var orders = new[]
        {
            this.FuturesOrderGenerator.Generate($"default, {MarketOrder}, {SideBuy}, {OrderPositionLong}"),
            this.FuturesOrderGenerator.Generate($"default, {MarketOrder}, {SideBuy}, {OrderPositionShort}"),
        };
        
        // Act
        var func = async () => await this.SUT.AddFuturesOrdersAsync(orders);

        // Assert
        (await func.Should()
            .ThrowExactlyAsync<DbUpdateException>().WithMessage("An error occurred while saving the entity changes. See the inner exception for details."))
            .WithInnerExceptionExactly<ArgumentException>().WithMessage("Not all orders have the same position side");
    }

    [Test]
    public async Task AddFuturesOrder_ShouldThrow_WhenThePositionSideOfTheOrdersDoesNotMatchThePositionSide()
    {
        // Arrange
        var orders = this.FuturesOrderGenerator.Generate(10, $"default, {MarketOrder}, {SideBuy}, {OrderPositionLong}");
        var position = this.FuturesPositionsGenerator.Generate($"default, {PositionSideShort}");
        await this.DbContext.FuturesPositions.AddAsync(position.ToDbEntity());
        await this.DbContext.SaveChangesAsync();
        
        // Act
        var func = async () => await this.SUT.AddFuturesOrdersAsync(orders, position.CryptoAutopilotId);
        
        // Assert
        (await func.Should()
            .ThrowExactlyAsync<DbUpdateException>().WithMessage("An error occurred while saving the entity changes. See the inner exception for details."))
            .WithInnerException<ArgumentException>().WithMessage($"The position side of the orders did not match the side of the position with CryptoAutopilotId {position.CryptoAutopilotId}");
    }
}
