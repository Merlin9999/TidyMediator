﻿using System;
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

        public Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResult));
            var handler = (dynamic)this._serviceProvider.GetRequiredService(handlerType);

            var behaviorInfos = this._pipelineBuilder.ResolveForRequest(requestType, isAsync: false);

            var behaviors = behaviorInfos
                .Select(info => this._serviceProvider.GetRequiredService(info.BehaviorType.MakeGenericType(requestType, typeof(TResult))))
                .Cast<dynamic>()
                .ToList();

            async Task<TResult> HandlerDelegate() => await handler.HandleAsync((dynamic)request, cancellationToken);

            var pipeline = behaviors
                .AsEnumerable()
                .Reverse()
                .Aggregate((RequestHandlerDelegate<TResult>)HandlerDelegate,
                    (next, behavior) => async () => await behavior.HandleAsync((dynamic)request, next, cancellationToken));

            return pipeline();
        }

        public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            var handlers = this._serviceProvider.GetServices<INotificationHandler<TNotification>>().ToList();
            var behaviorInfos = this._pipelineBuilder.ResolveForRequest(typeof(TNotification), isAsync: false);
            var behaviors = behaviorInfos
                .Select(info => this._serviceProvider.GetRequiredService(info.BehaviorType.MakeGenericType(typeof(TNotification), typeof(Unit))))
                .Cast<dynamic>()
                .ToList();

            var pipeline = behaviors
                .AsEnumerable()
                .Reverse()
                .Aggregate((RequestHandlerDelegate<Unit>)HandlerDelegate,
                    (next, behavior) => async () => await behavior.HandleAsync(notification, next, cancellationToken));
            
            async Task<Unit> HandlerDelegate()
            {
                var tasks = handlers.Select(handler => handler.HandleAsync(notification, cancellationToken));
                await Task.WhenAll(tasks);
                return Unit.Value;
            }

            await pipeline();
        }

        public IAsyncEnumerable<TItem> StreamAsync<TItem>(IStreamRequest<TItem> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();
            var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, typeof(TItem));
            var handler = (dynamic)this._serviceProvider.GetRequiredService(handlerType);

            IEnumerable<(Type BehaviorType, Type RequestType)> behaviorInfos = this._pipelineBuilder.ResolveForRequest(requestType, isAsync: true);

            List<dynamic> behaviors = behaviorInfos
                .Select(info => this._serviceProvider.GetRequiredService(info.BehaviorType.MakeGenericType(requestType, typeof(TItem))))
                .Cast<dynamic>()
                .ToList();

            Func<IAsyncEnumerable<TItem>> pipeline = () => handler.HandleAsync((dynamic)request, cancellationToken);

            foreach (dynamic behavior in behaviors.AsEnumerable().Reverse())
            {
                Func<IAsyncEnumerable<TItem>> current = pipeline;
                pipeline = () => behavior.HandleAsync((dynamic)request, current, cancellationToken);
            }

            return pipeline();
        }
    }
}
