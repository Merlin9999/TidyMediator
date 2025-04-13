using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

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

