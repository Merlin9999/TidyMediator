using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TidyMediator;

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

            services.RegisterDerivedTypesAsTransient(typeof(IRequestHandler<,>));
            services.RegisterDerivedTypesAsTransient(typeof(INotificationHandler<>));

            var builder = new PipelineBuilder(services);
            if (configure != null)
                configure(builder);
            services.AddSingleton(builder);

            return services;
        }

        public static IServiceCollection RegisterDerivedTypesAsTransient(this IServiceCollection services, Type baseType)
        {
            IEnumerable<(Type Derived, Type Base)> derivedTypes = GetTypesThatImplementOrInheritFrom(baseType);

            foreach ((Type Derived, Type Base) type in derivedTypes)
                services.AddTransient(type.Base, type.Derived);

            return services;
        }

        private static IEnumerable<(Type Derived, Type Base)> GetTypesThatImplementOrInheritFrom(Type baseType)
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            return loadedAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.ImplementsOrInheritsFrom(baseType))
                .Select(t => (Derived: t, Base: GetClosedBaseType(t, baseType)));
        }

        private static Type GetClosedBaseType(Type derivedType, Type baseType)
        {
            if (!derivedType.ImplementsOrInheritsFrom(baseType))
                return null;

            if (!baseType.IsGenericType || !baseType.IsGenericTypeDefinition)
                return baseType;

            if (baseType.IsInterface)
                return derivedType.GetInterfaces().FirstOrDefault(i => i.GetGenericTypeDefinition() == baseType);

            Type type = derivedType.BaseType;
            while (type != null && type != typeof(object))
            {
                if (type.GetGenericTypeDefinition() == baseType)
                    return type;

                type = type.BaseType;
            }

            return null;
        }

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
    }
}
