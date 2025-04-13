using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TidyMediator
{
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly PipelineBuilder _pipelineBuilder;

        public Mediator(IServiceProvider serviceProvider, PipelineBuilder pipelineBuilder)
        {
            this._serviceProvider = serviceProvider;
            this._pipelineBuilder = pipelineBuilder;
        }

        public Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResult));
            var handler = (dynamic)this._serviceProvider.GetRequiredService(handlerType);

            var behaviorInfos = this._pipelineBuilder.ResolveForRequest(requestType, isAsync: false);

            var behaviors = behaviorInfos
                .Select(info => this._serviceProvider.GetRequiredService(info.behaviorType.MakeGenericType(requestType, typeof(TResult))))
                .Cast<dynamic>()
                .ToList();

            async Task<TResult> HandlerDelegate() => await handler.Handle((dynamic)request, cancellationToken);

            var pipeline = behaviors
                .AsEnumerable()
                .Reverse()
                .Aggregate((RequestHandlerDelegate<TResult>)HandlerDelegate,
                    (next, behavior) => () => behavior.Handle((dynamic)request, next, cancellationToken));

            return pipeline();
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            var handlers = this._serviceProvider.GetServices<INotificationHandler<TNotification>>().ToList();
            var behaviorInfos = this._pipelineBuilder.ResolveForRequest(typeof(TNotification), isAsync: false);
            var behaviors = behaviorInfos
                .Select(info => this._serviceProvider.GetRequiredService(info.behaviorType.MakeGenericType(typeof(TNotification), typeof(Unit))))
                .Cast<dynamic>()
                .ToList();

            var pipeline = behaviors
                .AsEnumerable()
                .Reverse()
                .Aggregate((RequestHandlerDelegate<Unit>)HandlerDelegate,
                    (next, behavior) => () => behavior.Handle(notification, next, cancellationToken));
            
            async Task<Unit> HandlerDelegate()
            {
                var tasks = handlers.Select(handler => handler.Handle(notification, cancellationToken));
                await Task.WhenAll(tasks);
                return Unit.Value;
            }

            await pipeline();
        }

        //public IAsyncEnumerable<TItem> Stream<TQuery, TItem>(TQuery request, CancellationToken cancellationToken = default)
        //    where TRequest : IRequest<IAsyncEnumerable<TItem>>
        //{
        //    var handler = this._serviceProvider.GetRequiredService<IAsyncQueryHandler<TQuery, TItem>>();
        //    var behaviors = this._serviceProvider.GetServices<IAsyncPipelineBehavior<TQuery, TItem>>().ToList();

        //    Func<IAsyncEnumerable<TItem>> next = () => handler.Handle(request, cancellationToken);

        //    foreach (var behavior in behaviors.AsEnumerable().Reverse())
        //    {
        //        var current = next;
        //        next = () => behavior.Handle(request, current, cancellationToken);
        //    }

        //    return next();
        //}

        //public IAsyncEnumerable<TItem> Stream<TRequest, TItem>(TRequest request, CancellationToken cancellationToken = default)
        //    where TRequest : IRequest<IAsyncEnumerable<TItem>>
        //{
        //    var handler = _provider.GetRequiredService<IAsyncQueryHandler<TRequest, TItem>>();
        //    var behaviors = _provider.GetServices<IAsyncPipelineBehavior<TRequest, TItem>>().ToList();

        //    Func<IAsyncEnumerable<TItem>> next = () => handler.Handle(request, cancellationToken);

        //    foreach (var behavior in behaviors.AsEnumerable().Reverse())
        //    {
        //        var current = next;
        //        next = () => behavior.Handle(request, current, cancellationToken);
        //    }

        //    return next();
        //}
    }
}