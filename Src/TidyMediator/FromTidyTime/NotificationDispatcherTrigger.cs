using System.Threading;
using System.Threading.Tasks;

namespace TidyMediator.FromTidyTime
{
    public class NotificationDispatcherTrigger<TNotification> : INotificationHandler<TNotification>
        where TNotification : INotification
    {
        private readonly INotificationDispatcher<TNotification> _notificationDispatcher;
        private readonly ISyncContextNotificationDispatcher<TNotification> _syncContextNotificationDispatcher;

        public NotificationDispatcherTrigger(INotificationDispatcher<TNotification> notificationDispatcher, 
            ISyncContextNotificationDispatcher<TNotification> syncContextNotificationDispatcher)
        {
            this._notificationDispatcher = notificationDispatcher;
            this._syncContextNotificationDispatcher = syncContextNotificationDispatcher;
        }

        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            await this._notificationDispatcher.CallHandlers(notification);
            await this._syncContextNotificationDispatcher.CallHandlers(notification);
        }
    }
}