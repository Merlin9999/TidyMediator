namespace TidyMediator.FromTidyTime
{
    public interface INotificationDispatcher<TNotification> : INotificationDispatcherBase<TNotification>
        where TNotification : INotification { }
}