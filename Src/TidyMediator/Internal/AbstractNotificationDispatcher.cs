using System;
using System.Collections.Immutable;
using System.Linq;
using TidyMediator.Extensions;

namespace TidyMediator.Internal
{
    public abstract class AbstractNotificationDispatcher<TNotification>
        where TNotification : INotification
    {
        private readonly object _lockObject = new object();

        private volatile ImmutableList<WeakReference<NotificationRegistration<TNotification>>> 
            _registrations = ImmutableList<WeakReference<NotificationRegistration<TNotification>>>.Empty;

        private volatile ImmutableList<WeakReference<AsyncNotificationRegistration<TNotification>>>
            _asyncRegistrations = ImmutableList<WeakReference<AsyncNotificationRegistration<TNotification>>>.Empty;

        public void Subscribe(NotificationRegistration<TNotification> registration)
        {
            lock (this._lockObject)
            {
                this._registrations =
                    this._registrations.Add(new WeakReference<NotificationRegistration<TNotification>>(registration));
            }
        }

        public void Unsubscribe(NotificationRegistration<TNotification> registration)
        {
            lock (this._lockObject)
            {
                WeakReference<NotificationRegistration<TNotification>> referenceToRemove = this._registrations
                    .SingleOrDefault(x => WeakReferenceExtensions.TryGetTarget<NotificationRegistration<TNotification>>(x) == registration);

                if (referenceToRemove != null)
                    this._registrations = this._registrations.Remove(referenceToRemove);
            }
        }

        public void Subscribe(AsyncNotificationRegistration<TNotification> registration)
        {
            lock (this._lockObject)
            {
                this._asyncRegistrations =
                    this._asyncRegistrations.Add(
                        new WeakReference<AsyncNotificationRegistration<TNotification>>(registration));
            }
        }

        public void Unsubscribe(AsyncNotificationRegistration<TNotification> registration)
        {
            lock (this._lockObject)
            {
                WeakReference<AsyncNotificationRegistration<TNotification>> referenceToRemove = this._asyncRegistrations
                    .SingleOrDefault(x => x.TryGetTarget() == registration);

                if (referenceToRemove != null)
                    this._asyncRegistrations = this._asyncRegistrations.Remove(referenceToRemove);
            }
        }

        /// <summary>
        /// Gets the number of registered notification handlers. Use for unit tests.
        /// </summary>
        public int RegisteredDelegateCount
        {
            get
            {
                NotificationRegistrations registrations = this.GetNotificationRegistrations();
                return registrations.Registrations.Count + registrations.AsyncRegistrations.Count;
            }
        }

        protected NotificationRegistrations GetNotificationRegistrations()
        {
            lock (this._lockObject)
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

                return new NotificationRegistrations
                {
                    Registrations = registrations
                        .Where(x => x.Target != null)
                        .Select(x => x.Target)
                        .ToImmutableList(),
                    AsyncRegistrations = asyncRegistrations
                        .Where(x => x.Target != null)
                        .Select(x => x.Target)
                        .ToImmutableList(),
                };
            }
        }

        protected class NotificationRegistrations
        {
            public ImmutableList<NotificationRegistration<TNotification>> Registrations { get; set; }
            public ImmutableList<AsyncNotificationRegistration<TNotification>> AsyncRegistrations { get; set; }
        }
    }
}