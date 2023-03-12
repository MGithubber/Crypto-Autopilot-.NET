using Application.Interfaces.Services;

using Domain.Models;

using MediatR;

namespace Infrastructure.Notifications;

public class PositionOpenedNotification : INotification
{
    public required FuturesPosition Position { get; init; }
    public required IEnumerable<FuturesOrder> FuturesOrders { get; init; }
}

public class PositionNotificationHandler : INotificationHandler<PositionOpenedNotification>
{
    private readonly IFuturesTradesDBService DbService;
    public PositionNotificationHandler(IFuturesTradesDBService dbService)
    {
        this.DbService = dbService;
    }


    public async Task Handle(PositionOpenedNotification notification, CancellationToken cancellationToken)
    {
        await this.DbService.AddFuturesPositionAsync(notification.Position, notification.FuturesOrders);
    }
}
