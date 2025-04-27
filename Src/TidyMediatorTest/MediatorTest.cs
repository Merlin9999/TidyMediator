using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Nito.AsyncEx;
using Shouldly;
using TidyMediator.FromTidyTime;

namespace TidyMediator.Test;

public class MediatorTest
{
    [Fact]
    public void GetMediatorFromDI()
    {
        IMediator mediator = BuildServiceProvider().GetRequiredService<IMediator>();
        
        mediator.ShouldNotBeNull();
        mediator.ShouldBeOfType<Mediator>();
    }

    [Fact]
    public async Task SendRequest()
    {
        IMediator mediator = BuildServiceProvider().GetRequiredService<IMediator>();
        TestResponse response = await mediator.Send(new TestUniversalRequest());

        response.ShouldNotBeNull();
        response.Answer.ShouldBe(42);
    }

    [Fact]
    public async Task SendRequestWithSharedResponseType()
    {
        IMediator mediator = BuildServiceProvider().GetRequiredService<IMediator>();
        TestResponse response = await mediator.Send(new SpecifyValueRequest() { ValueToReturn = 3.14159m });

        response.ShouldNotBeNull();
        response.Answer.ShouldBe(3.14159m);
    }

    [Fact]
    public async Task SendRequestWithGlobalPipeline()
    {
        IServiceProvider serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddGlobalBehavior(typeof(RequestTrackingBehavior<,>));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();

        TestResponse response1 = await mediator.Send(new TestUniversalRequest());
        TestResponse response2 = await mediator.Send(new SpecifyValueRequest() { ValueToReturn = 3.14159m });

        response1.Answer.ShouldBe(42m);
        response2.Answer.ShouldBe(3.14159m);
        cache.RequestInSequence.ShouldBe(new[] { typeof(TestUniversalRequest), typeof(SpecifyValueRequest) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(TestUniversalRequest), typeof(SpecifyValueRequest) });
    }

    [Fact]
    public async Task SendRequestWithGlobalExceptPipeline()
    {
        IServiceProvider serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddGlobalBehaviorExcept(typeof(RequestTrackingBehavior<,>), typeof(SpecifyValueRequest));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();

        TestResponse response1 = await mediator.Send(new TestUniversalRequest());
        TestResponse response2 = await mediator.Send(new SpecifyValueRequest() { ValueToReturn = 3.14159m });

        response1.Answer.ShouldBe(42m);
        response2.Answer.ShouldBe(3.14159m);
        cache.RequestInSequence.ShouldBe(new[] { typeof(TestUniversalRequest) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(TestUniversalRequest) });
    }

    [Fact]
    public async Task SendRequestWithSpecificRequestPipeline()
    {
        IServiceProvider serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddBehaviorFor(typeof(RequestTrackingBehavior<,>), typeof(SpecifyValueRequest));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();

        TestResponse response1 = await mediator.Send(new TestUniversalRequest());
        TestResponse response2 = await mediator.Send(new SpecifyValueRequest() { ValueToReturn = 3.14159m });

        response1.Answer.ShouldBe(42m);
        response2.Answer.ShouldBe(3.14159m);
        cache.RequestInSequence.ShouldBe(new[] { typeof(SpecifyValueRequest) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(SpecifyValueRequest) });
    }

    [Fact]
    public async Task PublishNotification()
    {
        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        NotificationCounter notificationCounter = serviceProvider.GetRequiredService<NotificationCounter>();
        await mediator.Publish(new TestNotification());
        notificationCounter.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishNotificationWithGlobalPipeline()
    {
        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddGlobalBehavior(typeof(RequestTrackingBehavior<,>));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        NotificationCounter notificationCounter = serviceProvider.GetRequiredService<NotificationCounter>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();
        await mediator.Publish(new TestNotification());
        notificationCounter.Count.ShouldBe(2);
        cache.RequestInSequence.ShouldBe(new[] { typeof(TestNotification) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(TestNotification) });
    }

    [Fact]
    public async Task PublishNotificationWithGlobalExceptPipeline()
    {
        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddGlobalBehaviorExcept(typeof(RequestTrackingBehavior<,>), typeof(TestNotification));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        NotificationCounter notificationCounter = serviceProvider.GetRequiredService<NotificationCounter>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();
        await mediator.Publish(new TestNotification());
        notificationCounter.Count.ShouldBe(2);
        cache.RequestInSequence.ShouldBeEmpty();
        cache.RequestOutSequence.ShouldBeEmpty();
    }

    [Fact]
    public async Task PublishNotificationWithSpecificRequestPipeline()
    {
        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddBehaviorFor(typeof(RequestTrackingBehavior<,>), typeof(TestNotification));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        NotificationCounter notificationCounter = serviceProvider.GetRequiredService<NotificationCounter>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();
        await mediator.Publish(new TestNotification());
        notificationCounter.Count.ShouldBe(2);
        cache.RequestInSequence.ShouldBe(new[] { typeof(TestNotification) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(TestNotification) });
    }

    [Fact]
    public async Task PublishNotificationToRegisteredDelegate()
    {
        int notificationCount = 0;

        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
        });

        var registry = new NotificationRegistry(serviceProvider)
            .Subscribe<TestNotification>(notification => notificationCount++ );

        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(new TestNotification());
        notificationCount.ShouldBe(1);

        var dispatcher = serviceProvider.GetRequiredService<INotificationDispatcher<TestNotification>>();
        dispatcher.RegisteredDelegateCount.ShouldBe(1);
        registry.Dispose();
        dispatcher.RegisteredDelegateCount.ShouldBe(0);
    }

    [Fact]
    public async Task PublishNotificationToMultipleRegisteredDelegate()
    {
        int notificationCount = 0;

        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
        });

        var registry1 = new NotificationRegistry(serviceProvider)
            .Subscribe<TestNotification>(notification => notificationCount++)
            .Subscribe<TestNotification2>(notification => notificationCount++)
            .Subscribe<TestNotification2>(notification =>
            {
                notificationCount++;
                return Task.CompletedTask;
            });

        var registry2 = new NotificationRegistry(serviceProvider)
            .Subscribe<TestNotification2>(notification => notificationCount++);

        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(new TestNotification());
        notificationCount.ShouldBe(1);
        await mediator.Publish(new TestNotification2());
        notificationCount.ShouldBe(4);

        var dispatcher1 = serviceProvider.GetRequiredService<INotificationDispatcher<TestNotification>>();
        var dispatcher2 = serviceProvider.GetRequiredService<INotificationDispatcher<TestNotification2>>();

        dispatcher1.RegisteredDelegateCount.ShouldBe(1);
        dispatcher2.RegisteredDelegateCount.ShouldBe(3);
        registry1.Dispose();
        registry2.Dispose();
        dispatcher1.RegisteredDelegateCount.ShouldBe(0);
        dispatcher2.RegisteredDelegateCount.ShouldBe(0);
    }
    
    [Fact]
    public async Task PublishNotificationToRegisteredSyncContextSyncDelegate()
    {
        int notificationCount = 0;

        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
        });

        var registry = await RegisterAndPublishNotificationWithSyncContextAsync(serviceProvider, new TestNotification(),
            null,
            n => notificationCount++);

        notificationCount.ShouldBe(1);

        var dispatcher = serviceProvider.GetRequiredService<ISyncContextNotificationDispatcher<TestNotification>>();
        dispatcher.RegisteredDelegateCount.ShouldBe(1);
        registry.ShouldNotBeNull();
        registry.Dispose();
        dispatcher.RegisteredDelegateCount.ShouldBe(0);
    }

    [Fact]
    public async Task PublishNotificationToRegisteredSyncContextAsyncDelegate()
    {
        int notificationCount = 0;

        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
        });

        var registry = await RegisterAndPublishNotificationWithSyncContextAsync(serviceProvider, new TestNotification(),
            n =>
            {
                notificationCount++;
                return Task.CompletedTask;
            });

        notificationCount.ShouldBe(1);

        var dispatcher = serviceProvider.GetRequiredService<ISyncContextNotificationDispatcher<TestNotification>>();
        dispatcher.RegisteredDelegateCount.ShouldBe(1);
        registry.ShouldNotBeNull();
        registry.Dispose();
        dispatcher.RegisteredDelegateCount.ShouldBe(0);
    }

    [Fact]
    public async Task PublishNotificationToMultipleRegisteredSyncContextDelegate()
    {
        int notificationCount = 0;

        var serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<NotificationCounter>();
        });

        var registry = await RegisterAndPublishNotificationWithSyncContextAsync(serviceProvider, new TestNotification(),
            n =>
            {
                notificationCount++;
                return Task.CompletedTask;
            },
            n => notificationCount++,
            n => notificationCount++);

        notificationCount.ShouldBe(3);

        var dispatcher = serviceProvider.GetRequiredService<ISyncContextNotificationDispatcher<TestNotification>>();
        dispatcher.RegisteredDelegateCount.ShouldBe(3);
        registry.ShouldNotBeNull();
        registry.Dispose();
        dispatcher.RegisteredDelegateCount.ShouldBe(0);
    }

    private static async Task<SyncContextNotificationRegistry?> RegisterAndPublishNotificationWithSyncContextAsync<TNotification>(
        IServiceProvider serviceProvider,
        TNotification notification, 
        Func<TNotification, Task>? asyncDelegateToRegister,
        params Action<TNotification>[] delegatesToRegister) 
        where TNotification : INotification
    {
        SyncContextNotificationRegistry? registry = null;

        var contextThread = new AsyncContextThread();
        try
        {
            await contextThread.Factory.StartNew(() =>
            {
                // Subscribe to a notification using the current synchronization context
                registry = new SyncContextNotificationRegistry(serviceProvider);
                if (asyncDelegateToRegister != null)
                    registry.Subscribe<TNotification>(asyncDelegateToRegister);

                foreach (Action<TNotification> delegateToRegister in delegatesToRegister)
                    registry.Subscribe<TNotification>(delegateToRegister);
            });

            IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

            // Signal the notification while the async context thread is running so the delegate can be scheduled on that thread.
            await mediator.Publish(notification);
        }
        finally
        {
            // End the AsyncContextThread by joining that thread with this one.
            await contextThread.JoinAsync();
        }

        return registry;
    }

    [Fact]
    public async Task StreamRequest()
    {
        IMediator mediator = BuildServiceProvider().GetRequiredService<IMediator>();
        IAsyncEnumerable<TestAsyncItem> stream = mediator.Stream(new TestStreamUniversalRequest());

        var list = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream)
            list.Add(item);

        list.ShouldNotBeNull();
        list.ShouldNotBeEmpty();
        list.Select(x => x.Value).ShouldBe([41m, 42m, 43m]);
    }

    [Fact]
    public async Task StreamRequestWithSharedResponseType()
    {
        IMediator mediator = BuildServiceProvider().GetRequiredService<IMediator>();
        IAsyncEnumerable<TestAsyncItem> stream = mediator.Stream(new SpecifyStreamValuesRequest() { RangeStart = 40, RangeCount = 3 });

        var list = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream)
            list.Add(item);

        list.ShouldNotBeNull();
        list.ShouldNotBeEmpty();
        list.Select(x => x.Value).ShouldBe([40m, 41m, 42m]);
    }

    [Fact]
    public async Task StreamRequestWithGlobalPipeline()
    {
        IServiceProvider serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddGlobalBehavior(typeof(RequestStreamTrackingBehavior<,>));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();

        IAsyncEnumerable<TestAsyncItem> stream1 = mediator.Stream(new TestStreamUniversalRequest());
        IAsyncEnumerable<TestAsyncItem> stream2 = mediator.Stream(new SpecifyStreamValuesRequest() { RangeStart = 40, RangeCount = 3 });

        var list1 = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream1)
            list1.Add(item);
        var list2 = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream2)
            list2.Add(item);

        list1.ShouldNotBeNull();
        list1.ShouldNotBeEmpty();
        list1.Select(x => x.Value).ShouldBe([41m, 42m, 43m]);

        list2.ShouldNotBeNull();
        list2.ShouldNotBeEmpty();
        list2.Select(x => x.Value).ShouldBe([40m, 41m, 42m]);

        cache.RequestInSequence.ShouldBe(new[] { typeof(TestStreamUniversalRequest), typeof(SpecifyStreamValuesRequest) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(TestStreamUniversalRequest), typeof(SpecifyStreamValuesRequest) });
    }

    [Fact]
    public async Task StreamRequestWithGlobalExceptPipeline()
    {
        IServiceProvider serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddGlobalBehaviorExcept(typeof(RequestStreamTrackingBehavior<,>), typeof(SpecifyStreamValuesRequest));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();

        IAsyncEnumerable<TestAsyncItem> stream1 = mediator.Stream(new TestStreamUniversalRequest());
        IAsyncEnumerable<TestAsyncItem> stream2 = mediator.Stream(new SpecifyStreamValuesRequest() { RangeStart = 40, RangeCount = 3 });

        var list1 = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream1)
            list1.Add(item);
        var list2 = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream2)
            list2.Add(item);

        list1.ShouldNotBeNull();
        list1.ShouldNotBeEmpty();
        list1.Select(x => x.Value).ShouldBe([41m, 42m, 43m]);

        list2.ShouldNotBeNull();
        list2.ShouldNotBeEmpty();
        list2.Select(x => x.Value).ShouldBe([40m, 41m, 42m]);

        cache.RequestInSequence.ShouldBe(new[] { typeof(TestStreamUniversalRequest) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(TestStreamUniversalRequest) });
    }

    [Fact]
    public async Task StreamRequestWithSpecificRequestPipeline()
    {
        IServiceProvider serviceProvider = BuildServiceProvider(cfg =>
        {
            cfg.ServiceCollection.AddSingleton<RequestSequenceCache>();
            cfg.AddBehaviorFor(typeof(RequestStreamTrackingBehavior<,>), typeof(SpecifyStreamValuesRequest));
        });
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        RequestSequenceCache cache = serviceProvider.GetRequiredService<RequestSequenceCache>();

        IAsyncEnumerable<TestAsyncItem> stream1 = mediator.Stream(new TestStreamUniversalRequest());
        IAsyncEnumerable<TestAsyncItem> stream2 = mediator.Stream(new SpecifyStreamValuesRequest() { RangeStart = 40, RangeCount = 3 });

        var list1 = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream1)
            list1.Add(item);
        var list2 = new List<TestAsyncItem>();
        await foreach (TestAsyncItem item in stream2)
            list2.Add(item);

        list1.ShouldNotBeNull();
        list1.ShouldNotBeEmpty();
        list1.Select(x => x.Value).ShouldBe([41m, 42m, 43m]);

        list2.ShouldNotBeNull();
        list2.ShouldNotBeEmpty();
        list2.Select(x => x.Value).ShouldBe([40m, 41m, 42m]);

        cache.RequestInSequence.ShouldBe(new[] { typeof(SpecifyStreamValuesRequest) });
        cache.RequestOutSequence.ShouldBe(new[] { typeof(SpecifyStreamValuesRequest) });
    }

    public static IServiceProvider BuildServiceProvider(Action<PipelineBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddMediatorServices(configure);
        return services.BuildServiceProvider();
    }
}

public record TestResponse
{
    public decimal Answer { get; init; }
}

public record TestUniversalRequest : IRequest<TestResponse>
{
}

public class TestUniversalRequestHandler : IRequestHandler<TestUniversalRequest, TestResponse>
{
    public Task<TestResponse> Handle(TestUniversalRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse() { Answer = 42m });
    }
}

public record SpecifyValueRequest : IRequest<TestResponse>
{
    public decimal ValueToReturn { get; init; }
}

public class SpecifyValueHandler : IRequestHandler<SpecifyValueRequest, TestResponse>
{
    public Task<TestResponse> Handle(SpecifyValueRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse() { Answer = request.ValueToReturn });
    }
}

public record NotificationCounter
{
    public void Increment() => this.Count++;
    public int Count { get; private set; } = 0;
}

public class TestNotification : INotification { }

public class TestNotification2 : INotification { }

public class Test1NotificationHandler(NotificationCounter notificationCounter) : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        notificationCounter.Increment();
        return Task.CompletedTask;
    }
}
public class Test2NotificationHandler(NotificationCounter notificationCounter) : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        notificationCounter.Increment();
        return Task.CompletedTask;
    }
}

public class RequestSequenceCache
{
    public void AddInType(Type type) => this.RequestInSequence = this.RequestInSequence.Add(type);
    public void AddOutType(Type type) => this.RequestOutSequence = this.RequestOutSequence.Add(type);

    public ImmutableList<Type> RequestInSequence { get; private set; } = ImmutableList<Type>.Empty;
    public ImmutableList<Type> RequestOutSequence { get; private set; } = ImmutableList<Type>.Empty;
}

public class RequestTrackingBehavior<TRequest, TResponse>(RequestSequenceCache cache) : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cache.AddInType(typeof(TRequest));
        var response = await next();
        cache.AddOutType(typeof(TRequest));

        return response;
    }
}

public class RequestStreamTrackingBehavior<TRequest, TItem>(RequestSequenceCache cache) : IStreamPipelineBehavior<TRequest, TItem>
{
    public async IAsyncEnumerable<TItem> Handle(TRequest request, Func<IAsyncEnumerable<TItem>> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cache.AddInType(typeof(TRequest));

        // Await and yield items from the next delegate
        await foreach (var item in next().WithCancellation(cancellationToken))
        {
            yield return item;
        }

        cache.AddOutType(typeof(TRequest));
    }
}

public record TestAsyncItem
{
    public decimal Value { get; init; }
}

public record TestStreamUniversalRequest : IStreamRequest<TestAsyncItem>
{
}

public class TestStreamUniversalRequestHandler : IStreamRequestHandler<TestStreamUniversalRequest, TestAsyncItem>
{
    public async IAsyncEnumerable<TestAsyncItem> Handle(TestStreamUniversalRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IEnumerable<int> range = Enumerable.Range(41, 3);
        foreach (int i in range)
        {
            await Task.Delay(1, cancellationToken);
            yield return new TestAsyncItem() { Value = i };
        }
    }
}

public record SpecifyStreamValuesRequest : IStreamRequest<TestAsyncItem>
{
    public int RangeStart { get; init; }
    public int RangeCount { get; init; }
}

public class SpecifyStreamValuesHandler : IStreamRequestHandler<SpecifyStreamValuesRequest, TestAsyncItem>
{
    public async IAsyncEnumerable<TestAsyncItem> Handle(SpecifyStreamValuesRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IEnumerable<int> range = Enumerable.Range(request.RangeStart, request.RangeCount);
        foreach (int i in range)
        {
            await Task.Delay(1, cancellationToken);
            yield return new TestAsyncItem() { Value = i };
        }
    }
}
