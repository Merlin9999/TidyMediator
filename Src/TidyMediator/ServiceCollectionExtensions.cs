using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TidyMediator;
using TidyMediator.Internal;

namespace TidyMediator
{
    public static class ServiceCollectionExtensions
    {
        public static void LoadAssemblyByContainedType(this IServiceCollection services, Type type)
        {
            // By passing the type parameter, we know the assembly is loaded. Nothing else to do.
        }

        public static IServiceCollection AddMediatorServices(this IServiceCollection services, Action<PipelineBuilder> configure = null)
        {
            services.AddSingleton<IMediator, Mediator>();

            var builder = new PipelineBuilder(services);
            if (configure != null)
                configure(builder);
            services.AddSingleton(builder);

            services.RegisterDerivedTypesAsTransient(typeof(IRequestHandler<,>));
            services.RegisterDerivedTypesAsTransient(typeof(IStreamRequestHandler<,>));
            services.RegisterDerivedTypesAsTransient(typeof(INotificationHandler<>));

            services.AddSingleton(typeof(INotificationDispatcher<>), typeof(NotificationDispatcher<>));
            services.AddSingleton(typeof(ISyncContextNotificationDispatcher<>), typeof(SyncContextNotificationDispatcher<>));

            return services;
        }

        /// <summary>
        /// Returns true if the type implements or inherits from the specified base type.
        /// The derived type and the base type can both be generic type definitions (e.g.
        /// with generic type parameters unspecified).
        /// </summary>
        public static bool ImplementsOrInheritsFrom(this Type type, Type baseType)
        {
            if (type == null || baseType == null)
                return false;

            if (baseType.IsGenericTypeDefinition)
            {
                if (baseType.IsInterface)
                    return type.GetInterfaces().Any(t =>
                        t.IsGenericType && t.GetGenericTypeDefinition() == baseType);

                type = type.BaseType;
                while (type != null && type != typeof(object))
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                        return true;

                    type = type.BaseType;
                }

                return false;
            }

            if (baseType.IsInterface)
                return type.GetInterfaces().Any(t => t == baseType);

            type = type.BaseType;
            while (type != null && type != typeof(object))
            {
                if (type == baseType)
                    return true;

                type = type.BaseType;
            }

            return false;
        }

        private static IServiceCollection RegisterDerivedTypesAsTransient(this IServiceCollection services, Type baseType, 
            Action<IServiceCollection, (Type Derived, Type Base)> registrationAction = null)
        {
            IEnumerable<(Type Derived, Type Base)> derivedTypes = GetTypesThatImplementOrInheritFrom(baseType);

            foreach ((Type Derived, Type Base) typeInfo in derivedTypes)
            {
                services.AddTransient(typeInfo.Base, typeInfo.Derived);
                registrationAction?.Invoke(services, typeInfo);
            }

            return services;
        }

        private static IEnumerable<(Type Derived, Type Base)> GetTypesThatImplementOrInheritFrom(Type baseType)
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            return loadedAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.ImplementsOrInheritsFrom(baseType))
                .SelectMany(t => GetClosedBaseType(t, baseType),
                    (derivedType, closedBaseType) => (Derived: derivedType, Base: closedBaseType));
        }

        private static IEnumerable<Type> GetClosedBaseType(Type derivedType, Type baseType)
        {
            if (!derivedType.ImplementsOrInheritsFrom(baseType))
                return Enumerable.Empty<Type>();

            if (!baseType.IsGenericType || !baseType.IsGenericTypeDefinition)
                return Enumerable.Empty<Type>().Append(baseType);

            if (derivedType.IsGenericTypeDefinition)
                return Enumerable.Empty<Type>().Append(baseType);

            if (baseType.IsInterface)
                return derivedType.GetInterfaces().Where(i => i.GetGenericTypeDefinition() == baseType);

            Type type = derivedType.BaseType;
            while (type != null && type != typeof(object))
            {
                if (type.GetGenericTypeDefinition() == baseType)
                    return Enumerable.Empty<Type>().Append(type);

                type = type.BaseType;
            }

            return Enumerable.Empty<Type>();
        }
    }
}
