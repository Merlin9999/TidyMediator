using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TidyMediator.Extensions;

namespace TidyMediator.FromTidyTime
{
    public class SyncContextNotificationRegistry : AbstractNotificationRegistry<SyncContextNotificationRegistry>
    {
        public SyncContextNotificationRegistry(IServiceProvider sp)
            : base(sp)
        {
        }

        protected override void SubscribeToDispatcher<TNotification>(NotificationRegistration<TNotification> registration)
        {
            var sink = this.Sp.GetRequiredService<ISyncContextNotificationDispatcher<TNotification>>();
            sink.Subscribe(registration);
        }

        protected override void SubscribeToAsyncDispatcher<TNotification>(AsyncNotificationRegistration<TNotification> asyncRegistration)
        {
            var sink = this.Sp.GetRequiredService<ISyncContextNotificationDispatcher<TNotification>>();
            sink.Subscribe(asyncRegistration);
        }
    }
}
