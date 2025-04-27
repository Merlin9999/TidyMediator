using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using TidyMediator.Extensions;

namespace TidyMediator.Internal
{
    public abstract class AbstractNotificationRegistry<TRegistry> : IDisposable
        where TRegistry : AbstractNotificationRegistry<TRegistry>
    {
        private readonly object _lockObject = new object();
        protected readonly IServiceProvider Sp;

        // This registry holds strong references to registrations while the sink holds only weak references.
        ImmutableDictionary<Type, ImmutableList<object>> _registrations = ImmutableDictionary<Type, ImmutableList<object>>.Empty;
        ImmutableDictionary<Type, ImmutableList<object>> _asyncRegistrations = ImmutableDictionary<Type, ImmutableList<object>>.Empty;

        protected AbstractNotificationRegistry(IServiceProvider sp)
        {
            this.Sp = sp;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            //if (disposing)
            this.UnsubscribeAll();
        }

        public virtual AbstractNotificationRegistry<TRegistry> Subscribe<TNotification>(Action<TNotification> notificationAction)
            where TNotification : INotification
        {
            lock (this._lockObject)
            {
                var registration = new NotificationRegistration<TNotification>() { Handler = notificationAction };

                this._registrations = this._registrations.TryGetValue(typeof(TNotification), out ImmutableList<object> registrationList)
                    ? this._registrations.SetItem(typeof(TNotification), registrationList.Add(registration))
                    : this._registrations.Add(typeof(TNotification), ImmutableList<object>.Empty.Add(registration));

                this.SubscribeToDispatcher(registration);
            }

            return this;
        }

        protected abstract void SubscribeToDispatcher<TNotification>(NotificationRegistration<TNotification> registration)
            where TNotification : INotification;

        public virtual AbstractNotificationRegistry<TRegistry> Subscribe<TNotification>(Func<TNotification, Task> notificationFunc)
            where TNotification : INotification
        {
            lock (this._lockObject)
            {
                var asyncRegistration = new AsyncNotificationRegistration<TNotification>() { Handler = notificationFunc };

                this._asyncRegistrations = this._asyncRegistrations.TryGetValue(typeof(TNotification), out ImmutableList<object> asyncRegistrationList)
                    ? this._asyncRegistrations.SetItem(typeof(TNotification), asyncRegistrationList.Add(asyncRegistration))
                    : this._asyncRegistrations.Add(typeof(TNotification), ImmutableList<object>.Empty.Add(asyncRegistration));

                this.SubscribeToAsyncDispatcher(asyncRegistration);
            }

            return this;
        }

        protected abstract void SubscribeToAsyncDispatcher<TNotification>(AsyncNotificationRegistration<TNotification> asyncRegistration)
            where TNotification : INotification;

        public virtual AbstractNotificationRegistry<TRegistry> Unsubscribe<TNotification>()
            where TNotification : INotification
        {
            lock (this._lockObject)
            {
                this.UnsubscribeImpl<TNotification>();
            }

            return this;
        }

        public virtual AbstractNotificationRegistry<TRegistry> UnsubscribeAll()
        {
            lock (this._lockObject)
            {
                this.UnsubscribeAllImpl();
            }

            return this;
        }

        protected virtual void UnsubscribeAllImpl()
        {
            foreach (KeyValuePair<Type, ImmutableList<object>> registration in this._registrations)
                this.UnsubscribeImpl(registration.Key);

            foreach (KeyValuePair<Type, ImmutableList<object>> registration in this._asyncRegistrations)
                this.UnsubscribeImpl(registration.Key);
        }

        protected virtual void UnsubscribeImpl(Type notificationType)
        {
            var expression = Expression.Call(
                Expression.Constant(this),
                "UnsubscribeImpl",
                new[] { notificationType },
                null
            );

            var lambda = Expression.Lambda<Action>(expression);

            lambda.Compile()();
        }

        protected virtual void UnsubscribeImpl<TNotification>()
            where TNotification : INotification
        {
            ImmutableList<NotificationRegistration<TNotification>> registrations =
                (this._registrations.TryGetValue(typeof(TNotification)) ?? ImmutableList<object>.Empty)
                .Cast<NotificationRegistration<TNotification>>()
                .ToImmutableList();
            ImmutableList<AsyncNotificationRegistration<TNotification>> asyncRegistrations =
                (this._asyncRegistrations.TryGetValue(typeof(TNotification)) ?? ImmutableList<object>.Empty)
                .Cast<AsyncNotificationRegistration<TNotification>>()
                .ToImmutableList();

            if (!registrations.Any() && !asyncRegistrations.Any())
                return;

            var dispatcher = this.GetNotificationDispatcher<TNotification>();

            foreach (NotificationRegistration<TNotification> registration in registrations)
                dispatcher.Unsubscribe(registration);
            if (registrations.Any())
                this._registrations = this._registrations.Remove(typeof(TNotification));

            foreach (AsyncNotificationRegistration<TNotification> asyncRegistration in asyncRegistrations)
                dispatcher.Unsubscribe(asyncRegistration);
            this._asyncRegistrations = this._asyncRegistrations.Remove(typeof(TNotification));
        }

        protected abstract INotificationDispatcherBase<TNotification> GetNotificationDispatcher<TNotification>()
            where TNotification : INotification;
    }
}