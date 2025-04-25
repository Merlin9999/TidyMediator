using System;
using Microsoft.Extensions.DependencyInjection;

namespace TidyMediator.FromTidyTime
{
    public class NotificationRegistry : AbstractNotificationRegistry<NotificationRegistry>
    {
        public NotificationRegistry(IServiceProvider sp)
            : base(sp)
        {
        }

        protected override void SubscribeToDispatcher<TNotification>(NotificationRegistration<TNotification> registration)
        {
            var dispatcher = this.Sp.GetRequiredService<INotificationDispatcher<TNotification>>();
            dispatcher.Subscribe(registration);
        }

        protected override void SubscribeToAsyncDispatcher<TNotification>(AsyncNotificationRegistration<TNotification> asyncRegistration)
        {
            var sink = this.Sp.GetRequiredService<INotificationDispatcher<TNotification>>();
            sink.Subscribe(asyncRegistration);
        }
    }
}
