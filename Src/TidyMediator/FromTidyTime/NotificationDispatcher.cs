using System;
using System.Linq;
using System.Threading.Tasks;

namespace TidyMediator.FromTidyTime
{
    public class NotificationDispatcher<TNotification> : AbstractNotificationDispatcher<TNotification>, INotificationDispatcher<TNotification>
        where TNotification : INotification
    {
        public async Task CallHandlers(TNotification notification)
        {
            NotificationHandlers handlers = this.GetNotificationHandlers();

            foreach (Action<TNotification> handler in handlers.Notifications)
                handler(notification);

            await Task.WhenAll(handlers.AsyncNotifications.Select(func => func(notification)));
        }
    }
}
