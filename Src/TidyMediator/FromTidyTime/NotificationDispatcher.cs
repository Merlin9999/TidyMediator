using System;
using System.Linq;
using System.Threading.Tasks;

namespace TidyMediator.FromTidyTime
{
    public sealed class NotificationDispatcher<TNotification> : AbstractNotificationDispatcher<TNotification>, INotificationDispatcher<TNotification>
        where TNotification : INotification
    {
        public async Task CallHandlers(TNotification notification)
        {
            NotificationRegistrations registrations = this.GetNotificationRegistrations();

            foreach (NotificationRegistration<TNotification> registration in registrations.Registrations)
                registration.Handler(notification);

            await Task.WhenAll(registrations.AsyncRegistrations.Select(registration => registration.Handler(notification)));
        }
    }
}
