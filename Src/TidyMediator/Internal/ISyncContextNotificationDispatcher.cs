namespace TidyMediator.Internal
{
    public interface ISyncContextNotificationDispatcher<TNotification> : INotificationDispatcherBase<TNotification>
        where TNotification : INotification { }
}
