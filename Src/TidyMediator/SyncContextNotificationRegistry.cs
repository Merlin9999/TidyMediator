using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using TidyMediator.Internal;

namespace TidyMediator
{
    /// <summary>
    /// Notification registry for registering and dispatching notifications to notification handlers where the
    /// handlers are delegates attached to objects that are singletons or are NOT maintained by the DI container
    /// and the handlers must be called within a synchronization context. Commonly, this is used to schedule
    /// notification handlers on a UI thread associated with a synchronization context.
    /// </summary>
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
