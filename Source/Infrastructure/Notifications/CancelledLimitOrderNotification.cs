using Application.Interfaces.Services;

using MediatR;

namespace Infrastructure.Notifications;

public class CancelledLimitOrderNotification : INotification
{
    public required Guid BybitId { get; init; }
}

public class CancelledLimitOrderNotificationHandler : INotificationHandler<CancelledLimitOrderNotification>
{
    private readonly IFuturesTradesDBService DbService;
    public CancelledLimitOrderNotificationHandler(IFuturesTradesDBService dbService)
    {
        this.DbService = dbService;
    }

    public async Task Handle(CancelledLimitOrderNotification notification, CancellationToken cancellationToken)
    {
        await this.DbService.DeleteFuturesOrderAsync(notification.BybitId);
    }
}