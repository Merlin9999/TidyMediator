using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TidyMediator.Extensions;

namespace TidyMediator.FromTidyTime
{
    public class NotificationDispatcher<TNotification> : INotificationDispatcher<TNotification>
        where TNotification : INotification
    {
        protected readonly object LockObject = new object();

        private volatile ImmutableList<WeakReference<NotificationRegistration<TNotification>>> 
            _registrations = ImmutableList<WeakReference<NotificationRegistration<TNotification>>>.Empty;

        private volatile ImmutableList<WeakReference<AsyncNotificationRegistration<TNotification>>>
            _asyncRegistrations = ImmutableList<WeakReference<AsyncNotificationRegistration<TNotification>>>.Empty;

        public virtual void Subscribe(NotificationRegistration<TNotification> registration)
        {
            lock (this.LockObject)
            {
                this._registrations =
                    this._registrations.Add(new WeakReference<NotificationRegistration<TNotification>>(registration));
            }
        }

        public void Unsubscribe(NotificationRegistration<TNotification> registration)
        {
            lock (this.LockObject)
            {
                WeakReference<NotificationRegistration<TNotification>> referenceToRemove = this._registrations
                    .SingleOrDefault(x => x.TryGetTarget() == registration);

                if (referenceToRemove != null)
                    this._registrations = this._registrations.Remove(referenceToRemove);
            }
        }

        public void Subscribe(AsyncNotificationRegistration<TNotification> registration)
        {
            lock (this.LockObject)
            {
                this._asyncRegistrations =
                    this._asyncRegistrations.Add(
                        new WeakReference<AsyncNotificationRegistration<TNotification>>(registration));
            }
        }

        public void Unsubscribe(AsyncNotificationRegistration<TNotification> registration)
        {
            lock (this.LockObject)
            {
                WeakReference<AsyncNotificationRegistration<TNotification>> referenceToRemove = this._asyncRegistrations
                    .SingleOrDefault(x => x.TryGetTarget() == registration);

                if (referenceToRemove != null)
                    this._asyncRegistrations = this._asyncRegistrations.Remove(referenceToRemove);
            }
        }

        public virtual async Task CallHandlers(TNotification notification)
        {
            NotificationHandlers handlers = this.GetNotificationHandlers();

            foreach (Action<TNotification> handler in handlers.Notifications)
                handler(notification);

            await Task.WhenAll(handlers.AsyncNotifications.Select(func => func(notification)));
        }

        /// <summary>
        /// Gets the number of registered notification handlers. Use for unit tests.
        /// </summary>
        public int RegisteredNotificationCount
        {
            get
            {
                NotificationHandlers handlers = this.GetNotificationHandlers();
                return handlers.Notifications.Count + handlers.AsyncNotifications.Count;
            }
        }

        private NotificationHandlers GetNotificationHandlers()
        {
            lock (this.LockObject)
            {
                var registrations = this._registrations
                    .Select(x => new { Reference = x, Target = x.TryGetTarget() })
                    .ToImmutableList();

                var registrationsToRemove = registrations
                    .Where(x => x.Target == null)
                    .Select(x => x.Reference)
                    .ToImmutableList();

                this._registrations = this._registrations.RemoveRange(registrationsToRemove);

                var asyncRegistrations = this._asyncRegistrations
                    .Select(x => new { Reference = x, Target = x.TryGetTarget() })
                    .ToImmutableList();

                var asyncRegistrationsToRemove = asyncRegistrations
                    .Where(x => x.Target == null)
                    .Select(x => x.Reference)
                    .ToImmutableList();

                this._asyncRegistrations = this._asyncRegistrations.RemoveRange(asyncRegistrationsToRemove);

                return new NotificationHandlers
                {
                    Notifications = registrations
                        .Where(x => x.Target != null)
                        .Select(x => x.Target.Notification)
                        .ToImmutableList(),
                    AsyncNotifications = asyncRegistrations
                        .Where(x => x.Target != null)
                        .Select(x => x.Target.Notification)
                        .ToImmutableList(),
                };
            }
        }

        protected class NotificationHandlers
        {
            public ImmutableList<Action<TNotification>> Notifications { get; set; }
            public ImmutableList<Func<TNotification, Task>> AsyncNotifications { get; set; }
        }
    }
}
