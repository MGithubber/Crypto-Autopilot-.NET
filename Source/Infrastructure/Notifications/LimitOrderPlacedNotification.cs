using Application.Interfaces.Services;

using Domain.Models;

using MediatR;

namespace Infrastructure.Notifications;

public class LimitOrderPlacedNotification : INotification
{
    public required FuturesOrder LimitOrder { get; init; }
}

public class LimitOrderPlacedNotificationHandler : INotificationHandler<LimitOrderPlacedNotification>
{
    private readonly IFuturesTradesDBService DbService;
    public LimitOrderPlacedNotificationHandler(IFuturesTradesDBService dbService)
    {
        this.DbService = dbService;
    }


    public async Task Handle(LimitOrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        await this.DbService.AddFuturesOrderAsync(notification.LimitOrder);
    }
}
