using System.Threading;
using System.Threading.Tasks;

namespace TidyMediator.FromTidyTime
{
    public class NotificationDispatcherTrigger<TNotification> : INotificationHandler<TNotification>
        where TNotification : INotification
    {
        private readonly INotificationDispatcher<TNotification> _notificationDispatcher;

        public NotificationDispatcherTrigger(INotificationDispatcher<TNotification> notificationDispatcher)
        {
            this._notificationDispatcher = notificationDispatcher;
        }

        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            await this._notificationDispatcher.CallHandlers(notification);
        }
    }
}