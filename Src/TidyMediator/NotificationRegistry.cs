using System;
using Microsoft.Extensions.DependencyInjection;
using TidyMediator.Internal;

namespace TidyMediator
{
    /// <summary>
    /// Notification registry for registering and dispatching notifications to notification handlers where the
    /// handlers are delegates attached to objects that are NOT maintained by the DI container.
    /// </summary>
    public class NotificationRegistry : AbstractNotificationRegistry<NotificationRegistry>
    {
        public NotificationRegistry(IServiceProvider sp)
            : base(sp)
        {
        }

        protected override void SubscribeToDispatcher<TNotification>(NotificationRegistration<TNotification> registration)
        {
            INotificationDispatcherBase<TNotification> dispatcher = this.GetNotificationDispatcher<TNotification>();
            dispatcher.Subscribe(registration);
        }

        protected override void SubscribeToAsyncDispatcher<TNotification>(AsyncNotificationRegistration<TNotification> asyncRegistration)
        {
            INotificationDispatcherBase<TNotification> dispatcher = this.GetNotificationDispatcher<TNotification>();
            dispatcher.Subscribe(asyncRegistration);
        }

        protected override INotificationDispatcherBase<TNotification> GetNotificationDispatcher<TNotification>()
        {
            return this.Sp.GetRequiredService<INotificationDispatcher<TNotification>>();
        }
    }
}
