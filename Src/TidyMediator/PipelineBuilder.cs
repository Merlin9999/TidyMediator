using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TidyMediator
{
    public class PipelineBuilder
    {
        private readonly Dictionary<Type, List<PipelineRegistration>> _syncRegistrations = new Dictionary<Type, List<PipelineRegistration>>();
        private readonly Dictionary<Type, List<PipelineRegistration>> _asyncRegistrations = new Dictionary<Type, List<PipelineRegistration>>();
        public IServiceCollection ServiceCollection { get; }

        public PipelineBuilder(IServiceCollection services)
        {
            this.ServiceCollection = services;
        }

        public PipelineBuilder AddGlobalBehavior(Type openGenericBehaviorType)
        {
            ValidateOpenGenericBehavior(openGenericBehaviorType);
            this.ServiceCollection.AddTransient(openGenericBehaviorType, openGenericBehaviorType);
            this._syncRegistrations[openGenericBehaviorType] = new List<PipelineRegistration> { PipelineRegistration.Global() };
            return this;
        }

        public PipelineBuilder AddGlobalBehaviorExcept(Type openGenericBehaviorType, params Type[] excludedRequestTypes)
        {
            ValidateOpenGenericBehavior(openGenericBehaviorType);
            this.ServiceCollection.AddTransient(openGenericBehaviorType, openGenericBehaviorType);
            this._syncRegistrations[openGenericBehaviorType] = new List<PipelineRegistration> { PipelineRegistration.Except(excludedRequestTypes) };
            return this;
        }

        public PipelineBuilder AddBehaviorFor(Type openGenericBehaviorType, params Type[] requestTypes)
        {
            ValidateOpenGenericBehavior(openGenericBehaviorType);
            this.ServiceCollection.AddTransient(openGenericBehaviorType, openGenericBehaviorType);
            this._syncRegistrations[openGenericBehaviorType] = requestTypes.Select(PipelineRegistration.For).ToList();
            return this;
        }

        //public PipelineBuilder AddAsyncGlobalBehavior(Type openGenericBehaviorType)
        //{
        //    ValidateOpenGenericAsyncBehavior(openGenericBehaviorType);
        //    this.ServiceCollection.AddTransient(typeof(IAsyncPipelineBehavior<,>), openGenericBehaviorType);
        //    this._asyncRegistrations[openGenericBehaviorType] = new List<PipelineRegistration> { PipelineRegistration.Global() };
        //    return this;
        //}

        //public PipelineBuilder AddAsyncGlobalBehaviorExcept(Type openGenericBehaviorType, params Type[] excludedRequestTypes)
        //{
        //    ValidateOpenGenericAsyncBehavior(openGenericBehaviorType);
        //    this.ServiceCollection.AddTransient(typeof(IAsyncPipelineBehavior<,>), openGenericBehaviorType);
        //    this._asyncRegistrations[openGenericBehaviorType] = new List<PipelineRegistration> { PipelineRegistration.Except(excludedRequestTypes) };
        //    return this;
        //}

        //public PipelineBuilder AddAsyncBehaviorFor(Type openGenericBehaviorType, params Type[] requestTypes)
        //{
        //    ValidateOpenGenericAsyncBehavior(openGenericBehaviorType);
        //    this.ServiceCollection.AddTransient(typeof(IAsyncPipelineBehavior<,>), openGenericBehaviorType);
        //    this._asyncRegistrations[openGenericBehaviorType] = requestTypes.Select(PipelineRegistration.For).ToList();
        //    return this;
        //}

        public IEnumerable<(Type behaviorType, Type requestType)> ResolveForRequest(Type requestType, bool isAsync = false)
        {
            var source = isAsync ? this._asyncRegistrations : this._syncRegistrations;

            foreach (var kvp in source)
            {
                var behavior = kvp.Key;
                var configs = kvp.Value;

                foreach (var config in configs)
                {
                    if (config.AppliesTo(requestType))
                        yield return (behavior, requestType);
                }
            }
        }

        private static void ValidateOpenGenericBehavior(Type type)
        {
            if (!type.IsGenericTypeDefinition || type.GetGenericArguments().Length != 2 || !type.ImplementsOrInheritsFrom(typeof(IPipelineBehavior<,>)))
                throw new ArgumentException("Invalid Pipeline Behavior!", nameof(type));
        }

        //private static void ValidateOpenGenericAsyncBehavior(Type type)
        //{
        //    if (!type.IsGenericTypeDefinition || type.GenericTypeArguments.Length != 2 || !type.ImplementsOrInheritsFrom(typeof(IAsyncPipelineBehavior<,>)))
        //        throw new ArgumentException("Invalid Async Pipeline Behavior!", nameof(type));
        //}
    }

    public class PipelineRegistration
    {
        public enum Mode { Global, Include, Exclude }

        public Mode RegistrationMode { get; }
        public HashSet<Type> TargetTypes { get; }

        private PipelineRegistration(Mode mode, IEnumerable<Type> types = null)
        {
            this.RegistrationMode = mode;
            this.TargetTypes = types != null ? new HashSet<Type>(types) : new HashSet<Type>();
        }

        public static PipelineRegistration Global() => new PipelineRegistration(Mode.Global);
        public static PipelineRegistration For(Type requestType) => new PipelineRegistration(Mode.Include, new[] { requestType });
        public static PipelineRegistration Except(IEnumerable<Type> excludedTypes) => new PipelineRegistration(Mode.Exclude, excludedTypes);

        public bool AppliesTo(Type requestType)
        {
            switch (this.RegistrationMode)
            {
                case Mode.Global:
                    return true;
                case Mode.Include:
                    return this.TargetTypes.Contains(requestType);
                case Mode.Exclude:
                    return !this.TargetTypes.Contains(requestType);
                default:
                    return false;
            }
        }
    }

}