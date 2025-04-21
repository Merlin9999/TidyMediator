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
        Task<TResult> Handle(TRequest request, CancellationToken cancellationToken);
    }

    public interface INotification { }

    public interface INotificationHandler<in TNotification> where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }

    public interface IStreamRequest<TItem> { }

    public interface IStreamRequestHandler<in TRequest, out TItem> where TRequest : IStreamRequest<TItem>
    {
        IAsyncEnumerable<TItem> Handle(TRequest request, CancellationToken cancellationToken);
    }

    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    public interface IPipelineBehavior<in TRequest, TResponse>
    {
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    public interface IStreamPipelineBehavior<in TRequest, TItem>
    {
        IAsyncEnumerable<TItem> Handle(TRequest request, Func<IAsyncEnumerable<TItem>> next, CancellationToken cancellationToken);
    }

    public interface IMediator
    {
        Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default);
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
        IAsyncEnumerable<TItem> Stream<TItem>(IStreamRequest<TItem> request, CancellationToken cancellationToken = default);
    }
}
