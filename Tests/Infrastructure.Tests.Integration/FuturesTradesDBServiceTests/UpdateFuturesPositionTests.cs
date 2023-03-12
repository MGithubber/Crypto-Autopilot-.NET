using Application.Data.Mapping;

using Infrastructure.Tests.Integration.FuturesTradesDBServiceTests.Base;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Integration.FuturesTradesDBServiceTests;

public class UpdateFuturesPositionTests : FuturesTradesDBServiceTestsBase
{
    [Test]
    public async Task UpdateFuturesPosition_ShouldUpdateOrder_WhenInputIsValid()
    {
        // Arrange
        var position = this.FuturesPositionsGenerator.Generate($"default, {PositionSideLong}");

        var tran = await this.DbContext.Database.BeginTransactionAsync();
        await this.DbContext.FuturesPositions.AddAsync(position.ToDbEntity());
        await this.DbContext.SaveChangesAsync();
        await tran.CommitAsync();

        var newPosition = this.FuturesPositionsGenerator.Clone()
            .RuleFor(x => x.CryptoAutopilotId, position.CryptoAutopilotId)
            .Generate($"default, {PositionSideLong}");


        // Act
        await this.SUT.UpdateFuturesPositionAsync(position.CryptoAutopilotId, newPosition);

        
        // Assert
        this.DbContext.FuturesPositions.Single().ToDomainObject().Should().BeEquivalentTo(newPosition);
    }
    
    [Test]
    public async Task UpdateFuturesPosition_ShouldThrow_WhenUpdatedOrderHasDiffrentCryptoAutopilotId()
    {
        // Arrange
        var position = this.FuturesPositionsGenerator.Generate($"default, {PositionSideLong}");

        var tran = await this.DbContext.Database.BeginTransactionAsync();
        await this.DbContext.FuturesPositions.AddAsync(position.ToDbEntity());
        await this.DbContext.SaveChangesAsync();
        await tran.CommitAsync();
        
        var newPosition = this.FuturesPositionsGenerator.Clone()
            .RuleFor(x => x.CryptoAutopilotId, Guid.NewGuid())
            .Generate($"default, {PositionSideLong}");


        // Act
        var func = async () => await this.SUT.UpdateFuturesPositionAsync(position.CryptoAutopilotId, newPosition);


        // Assert
        (await func.Should().ThrowExactlyAsync<DbUpdateException>().WithMessage("An error occurred while saving the entity changes. See the inner exception for details."))
            .WithInnerException<ArgumentException>().WithMessage("The position CryptoAutopilotId cannot be changed since it would not be valid");
    }

    [Test]
    public async Task UpdateFuturesPosition_ShouldThrow_WhenUpdatedOrderHasOpposideSide()
    {
        // Arrange
        var position = this.FuturesPositionsGenerator.Generate($"default, {PositionSideLong}");

        var tran = await this.DbContext.Database.BeginTransactionAsync();
        await this.DbContext.FuturesPositions.AddAsync(position.ToDbEntity());
        await this.DbContext.SaveChangesAsync();
        await tran.CommitAsync();

        var newPosition = this.FuturesPositionsGenerator.Clone()
            .RuleFor(x => x.CryptoAutopilotId, position.CryptoAutopilotId)
            .Generate($"default, {PositionSideShort}");


        // Act
        var func = async () => await this.SUT.UpdateFuturesPositionAsync(position.CryptoAutopilotId, newPosition);


        // Assert
        (await func.Should().ThrowExactlyAsync<DbUpdateException>().WithMessage("An error occurred while saving the entity changes. See the inner exception for details."))
            .WithInnerException<ArgumentException>().WithMessage("The position side cannot be changed since it would not be valid");
    }
}
