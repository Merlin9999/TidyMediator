using System;
using System.Threading.Tasks;

namespace TidyMediator.FromTidyTime
{
    public interface INotificationDispatcher<TNotification> 
        where TNotification : INotification
    {
        void Subscribe(NotificationRegistration<TNotification> registration);

        void Unsubscribe(NotificationRegistration<TNotification> registration);

        void Subscribe(AsyncNotificationRegistration<TNotification> registration);

        void Unsubscribe(AsyncNotificationRegistration<TNotification> registration);

        Task CallHandlers(TNotification notification);

        /// <summary>
        /// Gets the number of registered notification handlers. Use for unit tests.
        /// </summary>
        int RegisteredNotificationCount { get; }

    }

    public class NotificationRegistration<TNotification> where TNotification : INotification
    {
        public Action<TNotification> Notification { get; set; }
    }

    public class AsyncNotificationRegistration<TNotification> where TNotification : INotification
    {
        public Func<TNotification, Task> Notification { get; set; }
    }
}
