using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TidyMediator.Extensions;

namespace TidyMediator.FromTidyTime
{
    public static class SyncContextNotificationSink
    {
        public static SynchronizationContext SyncContext { get; private set; }

        public static void CaptureUISynchronizationContext()
        {
            SyncContext = SyncContext ?? SynchronizationContext.Current;
        }
    }

    public sealed class SyncContextNotificationDispatcher<TNotification> : AbstractNotificationDispatcher<TNotification>, 
        ISyncContextNotificationDispatcher<TNotification> 
        where TNotification : INotification
    {
        public async Task CallHandlers(TNotification notification)
        {
            NotificationRegistrations registrations = this.GetNotificationRegistrations();

            SynchronizationContext defaultSyncContext = SyncContextNotificationSink.SyncContext;

            foreach (NotificationRegistration<TNotification> registration in registrations.Registrations)
                (registration.SyncContext ?? defaultSyncContext)?.Send(_ => registration.Handler(notification), null);

            await Task.WhenAll(registrations.AsyncRegistrations.Select(registration =>
                (registration.SyncContext ?? defaultSyncContext)?.SendAsync(_ => registration.Handler(notification), null)));
        }
    }
}