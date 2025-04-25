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
            NotificationHandlers handlers = this.GetNotificationHandlers();

            SynchronizationContext syncContext = SyncContextNotificationSink.SyncContext;
            if (syncContext == null)
                return;

            foreach (Action<TNotification> handler in handlers.Notifications)
                syncContext.Send(_ => handler(notification), null);

            await Task.WhenAll(handlers.AsyncNotifications.Select(func =>
                syncContext.SendAsync(_ => func(notification), null)));
        }
    }
}