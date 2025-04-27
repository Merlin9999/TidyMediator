namespace TidyMediator.Internal
{
    public interface INotificationDispatcher<TNotification> : INotificationDispatcherBase<TNotification>
        where TNotification : INotification { }
}