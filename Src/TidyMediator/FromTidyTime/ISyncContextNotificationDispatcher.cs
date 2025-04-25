namespace TidyMediator.FromTidyTime
{
    public interface ISyncContextNotificationDispatcher<TNotification> : INotificationDispatcherBase<TNotification>
        where TNotification : INotification { }
}
