using Application.Interfaces.Services;

using Domain.Models;

using MediatR;

namespace Infrastructure.Notifications;

public class UpdatedLimitOrderNotification : INotification
{
    public required Guid BybitId { get; init; }
    public required FuturesOrder UpdatedLimitOrder { get; init; } 
}

public class UpdatedLimitOrderNotificationHandler : INotificationHandler<UpdatedLimitOrderNotification>
{
    private readonly IFuturesTradesDBService DbService;
    public UpdatedLimitOrderNotificationHandler(IFuturesTradesDBService dbService)
    {
        this.DbService = dbService;
    }

    public async Task Handle(UpdatedLimitOrderNotification notification, CancellationToken cancellationToken)
    {
        await this.DbService.UpdateFuturesOrderAsync(notification.BybitId, notification.UpdatedLimitOrder);
    }
}