using System.Text;

using Application.Data.Mapping;
using Application.Interfaces.Services;

using Domain.Models;

using FluentValidation;
using FluentValidation.Results;

using Infrastructure.Database.Contexts;
using Infrastructure.Database.Internal;
using Infrastructure.Validators;

using Microsoft.EntityFrameworkCore;

using static Infrastructure.Validators.FuturesOrderValidator;

namespace Infrastructure.Services;

public class FuturesTradesDBService : IFuturesTradesDBService
{
    private static readonly FuturesOrderValidator OrderValidator = new();
    private static readonly FuturesPositionValidator PositionValidator = new();
    
    private readonly FuturesTradingDbContext DbContext;
    public FuturesTradesDBService(FuturesTradingDbContext dbContext)
    {
        this.DbContext = dbContext;
    }
    

    public async Task AddFuturesOrderAsync(FuturesOrder futuresOrder, Guid? positionId = null)
    {
        _ = futuresOrder ?? throw new ArgumentNullException(nameof(futuresOrder));
       
        var validaiton = await ValidateOrderNeedsPositionAsync(futuresOrder);
        if (validaiton.IsValid && positionId is null)
        {
            var innerException = new ArgumentException("A created market order or a filled limit order needs to be associated with a position and no position identifier has been specified.", new ValidationException(validaiton.Errors));
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }
        else if (!validaiton.IsValid && positionId is not null)
        {
            var innerException = new ArgumentException("Only a created market order or a filled limit order can be associated with a position.", new ValidationException(validaiton.Errors));
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }

        
        var entity = futuresOrder.ToDbEntity();
        if (positionId is not null)
        {
            var position = await this.DbContext.FuturesPositions.Where(x => x.CryptoAutopilotId == positionId).SingleAsync();
            if (position.Side != futuresOrder.PositionSide)
            {
                var innerException = new ArgumentException($"The position side of the order did not match the side of the position with CryptoAutopilotId {positionId}");
                throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
            }
            
            entity.PositionId = position.Id;
        }

        using var transaction = await this.BeginTransactionAsync();

        await this.DbContext.FuturesOrders.AddAsync(entity);
        await this.DbContext.SaveChangesAsync();
    }
    public async Task AddFuturesOrdersAsync(IEnumerable<FuturesOrder> futuresOrders, Guid? positionId = null)
    {
        _ = futuresOrders ?? throw new ArgumentNullException(nameof(futuresOrders));


        PositionRequirementConsistentForAllOrders(futuresOrders, out var allNeedPosition, out var noneNeedPosition);
        
        if (allNeedPosition && positionId is null)
        {
            var innerException = new ArgumentException("A created market order or a filled limit order needs to be associated with a position and no position identifier has been specified.");
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }
        else if (noneNeedPosition && positionId is not null)
        {
            var innerException = new ArgumentException("Only a created market order or a filled limit order can be associated with a position.");
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }



        var futuresOrderDbEntities = futuresOrders.Select(order => order.ToDbEntity()).ToArray();
        if (positionId is not null)
        {
            var position = await this.DbContext.FuturesPositions.Where(x => x.CryptoAutopilotId == positionId).SingleAsync();
            if (position.Side != futuresOrders.First().PositionSide)
            {
                var innerException = new ArgumentException($"The position side of the orders did not match the side of the position with CryptoAutopilotId {positionId}");
                throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
            }

            foreach (var orderDbEntity in futuresOrderDbEntities)
                orderDbEntity.PositionId = position.Id;
        }

        
        using var transaction = await this.BeginTransactionAsync();
        
        await this.DbContext.FuturesOrders.AddRangeAsync(futuresOrderDbEntities);
        await this.DbContext.SaveChangesAsync();
    }
    public async Task<IEnumerable<FuturesOrder>> GetAllFuturesOrdersAsync()
    {
        return await this.DbContext.FuturesOrders
            .OrderBy(x => x.CurrencyPair)
            .OrderByDescending(x => x.CreateTime)
            .Select(x => x.ToDomainObject())
            .ToListAsync();
    }
    public async Task<IEnumerable<FuturesOrder>> GetFuturesOrdersByCurrencyPairAsync(string currencyPair)
    {
        return await this.DbContext.FuturesOrders
            .Where(x => x.CurrencyPair == currencyPair)
            .OrderBy(x => x.CurrencyPair)
            .OrderByDescending(x => x.CreateTime)
            .Select(x => x.ToDomainObject())
            .ToListAsync();
    }
    public async Task UpdateFuturesOrderAsync(Guid bybitID, FuturesOrder updatedFuturesOrder, Guid? positionId = null)
    {
        _ = updatedFuturesOrder ?? throw new ArgumentNullException(nameof(updatedFuturesOrder));
        
        var validation = await ValidateOrderNeedsPositionAsync(updatedFuturesOrder);
        if (validation.IsValid && positionId is null)
        {
            var innerException = new ArgumentException("A created market order or a filled limit order needs to be associated with a position and no position identifier has been specified.", new ValidationException(validation.Errors));
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }
        else if (!validation.IsValid && positionId is not null)
        {
            var innerException = new ArgumentException("Only a created market order or a filled limit order can be associated with a position.", new ValidationException(validation.Errors));
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }


        using var transaction = await this.BeginTransactionAsync();

        var dbEntity = await this.DbContext.FuturesOrders.Where(x => x.BybitID == bybitID).SingleOrDefaultAsync() ?? throw new DbUpdateException($"Could not find futures order with uniqueID == {bybitID}");
        if (positionId is not null)
        {
            var position = await this.DbContext.FuturesPositions.Where(x => x.CryptoAutopilotId == positionId).SingleAsync();
            if (position.Side != updatedFuturesOrder.PositionSide)
            {
                var innerException = new ArgumentException($"The position side of the order did not match the side of the position with CryptoAutopilotId {positionId}");
                throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
            }

            dbEntity.PositionId = position.Id;
        }
        
        dbEntity.CurrencyPair = updatedFuturesOrder.CurrencyPair.Name;
        dbEntity.BybitID = updatedFuturesOrder.BybitID;
        dbEntity.CreateTime = updatedFuturesOrder.CreateTime;
        dbEntity.UpdateTime = updatedFuturesOrder.UpdateTime;
        dbEntity.Side = updatedFuturesOrder.Side;
        dbEntity.PositionSide = updatedFuturesOrder.PositionSide;
        dbEntity.Type = updatedFuturesOrder.Type;
        dbEntity.Price = updatedFuturesOrder.Price;
        dbEntity.Quantity = updatedFuturesOrder.Quantity;
        dbEntity.StopLoss = updatedFuturesOrder.StopLoss;
        dbEntity.TakeProfit = updatedFuturesOrder.TakeProfit;
        dbEntity.TimeInForce = updatedFuturesOrder.TimeInForce;
        dbEntity.Status = updatedFuturesOrder.Status;

        await this.DbContext.SaveChangesAsync();
    }
    public async Task DeleteFuturesOrderAsync(Guid bybitID)
    {
        using var _ = await this.BeginTransactionAsync();
        
        var order = await this.DbContext.FuturesOrders.Where(x => x.BybitID == bybitID).SingleOrDefaultAsync() ?? throw new DbUpdateException($"No order with bybitID {bybitID} was found in the database");
        this.DbContext.Remove(order);
        await this.DbContext.SaveChangesAsync();
    }
    
    public async Task AddFuturesPositionAsync(FuturesPosition position, IEnumerable<FuturesOrder> futuresOrders)
    {
        _ = position ?? throw new ArgumentNullException(nameof(position));
        _ = futuresOrders ?? throw new ArgumentNullException(nameof(futuresOrders));


        PositionRequirementConsistentForAllOrders(futuresOrders, out var allNeedPosition, out var noneNeedPosition);

        if (noneNeedPosition)
        {
            var innerException = new ArgumentException("Only a created market order or a filled limit order can be associated with a position.");
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }
        else if (allNeedPosition && futuresOrders.First().PositionSide != position.Side)
        {
            var innerException = new ArgumentException("The position side of the specified orders does not match the side of the specified position");
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }
        


        var positionDbEntity = position.ToDbEntity();
        var ordersDbEntities = futuresOrders.Select(x =>
        {
            var entity = x.ToDbEntity();
            entity.Position = positionDbEntity;
            return entity;
        });

        
        using var transaction = await this.BeginTransactionAsync();
        
        await this.DbContext.FuturesOrders.AddRangeAsync(ordersDbEntities);
        await this.DbContext.SaveChangesAsync();
    }


    private static Task<ValidationResult> ValidateOrderNeedsPositionAsync(FuturesOrder futuresOrder)
    {
        return OrderValidator.ValidateAsync(futuresOrder, x => x.IncludeRuleSets(MarketOrder, StatusCreated));
    }
    private static Task<ValidationResult> ValidateOrderShouldNotHavePositionAsync(FuturesOrder futuresOrder)
    {
        return OrderValidator.ValidateAsync(futuresOrder, x => x.IncludeRuleSets(LimitOrder, StatusCreated));
    }
    private static void PositionRequirementConsistentForAllOrders(IEnumerable<FuturesOrder> futuresOrders, out bool allNeedPosition, out bool noneNeedPosition)
    {
        allNeedPosition = futuresOrders.All(x => ValidateOrderNeedsPositionAsync(x).GetAwaiter().GetResult().IsValid);
        noneNeedPosition = futuresOrders.All(x => ValidateOrderShouldNotHavePositionAsync(x).GetAwaiter().GetResult().IsValid);
        
        if (!allNeedPosition && !noneNeedPosition)
        {
            var sb = new StringBuilder();
            sb.Append("Some of the specified orders can be associated with a position while some cannot. ");
            sb.Append("All the specified orders need to have the same requirements in terms of beeing associated with a position to add them in the database at once.");
            throw new ArgumentException(sb.ToString(), nameof(futuresOrders));
        }
        
        var positionSide = futuresOrders.First().PositionSide;
        if (!futuresOrders.All(x => x.PositionSide == positionSide))
        {
            var innerException = new ArgumentException("Not all orders have the same position side");
            throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", innerException);
        }
    }

    private async Task<TransactionalOperation> BeginTransactionAsync()
        => new TransactionalOperation(await this.DbContext.Database.BeginTransactionAsync());
}
