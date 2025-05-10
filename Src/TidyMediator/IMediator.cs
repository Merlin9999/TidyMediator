using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TidyMediator
{
    public interface IRequest<TResult> { }

    public interface IRequestHandler<in TRequest, TResult> 
        where TRequest : IRequest<TResult>
    {
        Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public interface INotification { }

    public interface INotificationHandler<in TNotification> where TNotification : INotification
    {
        Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }

    public interface IStreamRequest<TItem> { }

    public interface IStreamRequestHandler<in TRequest, out TItem> where TRequest : IStreamRequest<TItem>
    {
        IAsyncEnumerable<TItem> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    public interface IPipelineBehavior<in TRequest, TResponse>
    {
        Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    public interface IStreamPipelineBehavior<in TRequest, TItem>
    {
        IAsyncEnumerable<TItem> HandleAsync(TRequest request, Func<IAsyncEnumerable<TItem>> next, CancellationToken cancellationToken);
    }

    public interface IMediator
    {
        Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default);
        Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
        IAsyncEnumerable<TItem> StreamAsync<TItem>(IStreamRequest<TItem> request, CancellationToken cancellationToken = default);
    }

    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = new Unit();
        public override string ToString() => "()";
        public override bool Equals(object obj) => obj is Unit;
        public bool Equals(Unit other) => true;
        public override int GetHashCode() => 0;
        public static bool operator ==(Unit left, Unit right) => true;
        public static bool operator !=(Unit left, Unit right) => false;
    }
}
