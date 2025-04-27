using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using TidyMediator.Internal;

namespace TidyMediator
{
    public class SyncContextNotificationRegistry : AbstractNotificationRegistry<SyncContextNotificationRegistry>
    {
        public static void CaptureUISynchronizationContext()
        {
            SyncContextNotificationSink.CaptureUISynchronizationContext();
        }

        public SyncContextNotificationRegistry(IServiceProvider sp)
            : base(sp)
        {
        }

        protected override void SubscribeToDispatcher<TNotification>(NotificationRegistration<TNotification> registration)
        {
            registration.SyncContext = SynchronizationContext.Current;
            INotificationDispatcherBase<TNotification> dispatcher = this.GetNotificationDispatcher<TNotification>();
            dispatcher.Subscribe(registration);
        }

        protected override void SubscribeToAsyncDispatcher<TNotification>(AsyncNotificationRegistration<TNotification> asyncRegistration)
        {
            asyncRegistration.SyncContext = SynchronizationContext.Current;
            INotificationDispatcherBase<TNotification> dispatcher = this.GetNotificationDispatcher<TNotification>();
            dispatcher.Subscribe(asyncRegistration);
        }

        protected override INotificationDispatcherBase<TNotification> GetNotificationDispatcher<TNotification>()
        {
            return this.Sp.GetRequiredService<ISyncContextNotificationDispatcher<TNotification>>();
        }
    }
}
