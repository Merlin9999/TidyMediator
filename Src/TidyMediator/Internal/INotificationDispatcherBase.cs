using System;
using System.Threading;
using System.Threading.Tasks;

namespace TidyMediator.Internal
{
    public interface INotificationDispatcherBase<TNotification> 
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
        int RegisteredDelegateCount { get; }
    }

    public class NotificationRegistration<TNotification> where TNotification : INotification
    {
        public Action<TNotification> Handler { get; set; }
        public SynchronizationContext SyncContext { get; set; } = null;
    }

    public class AsyncNotificationRegistration<TNotification> where TNotification : INotification
    {
        public Func<TNotification, Task> Handler { get; set; }
        public SynchronizationContext SyncContext { get; set; } = null;
    }
}
