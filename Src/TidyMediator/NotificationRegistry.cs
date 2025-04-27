using System;
using Microsoft.Extensions.DependencyInjection;
using TidyMediator.Internal;

namespace TidyMediator
{
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
