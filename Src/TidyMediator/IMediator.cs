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

    public interface IAsyncRequest<TItem> { }

    //public interface IAsyncRequestHandler<in TRequest, out TItem> where TRequest : IAsyncRequest<TItem>
    //{
    //    IAsyncEnumerable<TItem> Handle(TRequest query, CancellationToken cancellationToken);
    //}



    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    public interface IPipelineBehavior<in TRequest, TResponse>
    {
        Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    //public interface IAsyncPipelineBehavior<in TRequest, TItem>
    //{
    //    IAsyncEnumerable<TItem> Handle(TRequest request, Func<IAsyncEnumerable<TItem>> next, CancellationToken cancellationToken);
    //}

    public struct Unit
    {
        public static readonly Unit Value = new Unit();
    }

    public interface IMediator
    {
        Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default);
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
        //IAsyncEnumerable<TItem> Stream<TRequest, TItem>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IAsyncRequest<TItem>;
    }
}
