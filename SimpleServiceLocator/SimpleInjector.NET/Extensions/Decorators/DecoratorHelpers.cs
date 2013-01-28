﻿#region Copyright (c) 2010 S. van Deursen
/* The Simple Injector is an easy-to-use Inversion of Control library for .NET
 * 
 * Copyright (C) 2010 S. van Deursen
 * 
 * To contact me, please visit my blog at http://www.cuttingedge.it/blogs/steven/ or mail to steven at 
 * cuttingedge.it.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
 * associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the 
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO 
 * EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

namespace SimpleInjector.Extensions.Decorators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using SimpleInjector.Advanced;
    using SimpleInjector.Analysis;

    internal static class DecoratorHelpers
    {
        private static readonly MethodInfo EnumerableSelectMethod =
            ExtensionHelpers.GetGenericMethod(() => Enumerable.Select<int, int>(null, (Func<int, int>)null));

        // This method must be public because of restrictions of the Silverlight Sandbox.
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
            Justification = "The method is called using reflection.")]
        public static Func<TService, TService> BuildFuncInitializer<TService>(Container container)
        {
            Action<TService> instanceInitializer = container.GetInitializer<TService>();

            if (instanceInitializer != null)
            {
                return instance =>
                {
                    instanceInitializer(instance);

                    return instance;
                };
            }

            return null;
        }

        internal static Delegate GetFuncInitializer(this Container container, Type serviceType)
        {
            var instanceInitializer =
                typeof(DecoratorHelpers).GetMethod("BuildFuncInitializer")
                .MakeGenericMethod(serviceType)
                .Invoke(null, new object[] { container });

            return (Delegate)instanceInitializer;
        }

        internal static IDecoratableEnumerable CreateDecoratableEnumerable(Type serviceType,
            Container container, Type[] serviceTypes)
        {
            Type allInstancesEnumerableType = typeof(DecoratableEnumerable<>).MakeGenericType(serviceType);

            return (IDecoratableEnumerable)Activator.CreateInstance(allInstancesEnumerableType,
                new object[] { container, serviceTypes });
        }

        internal static IDecoratableEnumerable CreateDecoratableEnumerable(Type serviceType,
            DecoratorPredicateContext[] contexts)
        {
            Type allInstancesEnumerableType = typeof(DecoratableEnumerable<>).MakeGenericType(serviceType);

            return (IDecoratableEnumerable)Activator.CreateInstance(allInstancesEnumerableType,
                new object[] { contexts });
        }

        internal static IDecoratableEnumerable CreateDecoratableEnumerable(Type serviceType,
            Container container, IEnumerable<Expression> expressions)
        {
            Type allInstancesEnumerableType = typeof(DecoratableEnumerable<>).MakeGenericType(serviceType);

            return (IDecoratableEnumerable)Activator.CreateInstance(allInstancesEnumerableType,
                new object[] { container, expressions });
        }

        internal static bool IsDecoratableEnumerableExpression(Expression enumerableExpression)
        {
            var constantExpression = enumerableExpression as ConstantExpression;

            object enumerable = constantExpression != null ? constantExpression.Value : null;

            return enumerable is IEnumerable<Expression> || enumerable is IDecoratableEnumerable;
        }

        internal static IDecoratableEnumerable ConvertToDecoratableEnumerable(Type serviceType,
            Container container, object enumerable)
        {
            // The RegisterAll<TService>(TService[]) method in the core library registers a collection that
            // implements IEnumerable<Expression>, especially for use here.
            var expressions = enumerable as IEnumerable<Expression>;

            if (expressions != null)
            {
                enumerable = DecoratorHelpers.CreateDecoratableEnumerable(serviceType, container, expressions);
            }

            return enumerable as IDecoratableEnumerable;
        }

        internal static IEnumerable Select(this IEnumerable source, Type type, Delegate selector)
        {
            var selectMethod = EnumerableSelectMethod.MakeGenericMethod(type, type);

            return (IEnumerable)selectMethod.Invoke(null, new object[] { source, selector });
        }

        internal static MethodCallExpression Select(Expression collectionExpression, Type type,
            Delegate selector)
        {
            // We make use of .NET's built in Enumerable.Select to wrap the collection with the decorators.
            var selectMethod = EnumerableSelectMethod.MakeGenericMethod(type, type);

            return Expression.Call(selectMethod, collectionExpression, Expression.Constant(selector));
        }

        internal static ConstantExpression ToConstant(this Expression expression)
        {
            return Expression.Constant(expression.Invoke());
        }

        internal static object Invoke(this Expression expression)
        {
            Func<object> instanceCreator = Expression.Lambda<Func<object>>(expression).Compile();

            return instanceCreator();
        }

        internal static Expression BuildDecoratorExpression(Container container,
            ConstructorInfo decoratorConstructor, Expression[] parameters)
        {
            Type decoratorType = decoratorConstructor.DeclaringType;

            Expression expression = Expression.New(decoratorConstructor, parameters);

            var instanceInitializer = container.GetFuncInitializer(decoratorType);

            if (instanceInitializer != null)
            {
                // It's not possible to return a Expression that is as heavily optimized as the
                // Expression.New simply is, because the instance initializer must be called as well.
                expression = Expression.Invoke(Expression.Constant(instanceInitializer), expression);
            }

            return expression;
        }

        internal static bool DecoratesServiceType(Type serviceType, ConstructorInfo decoratorConstructor)
        {
            int numberOfServiceTypeDependencies =
                GetNumberOfServiceTypeDependencies(serviceType, decoratorConstructor);

            return numberOfServiceTypeDependencies == 1;
        }

        internal static int GetNumberOfServiceTypeDependencies(Type serviceType,
            ConstructorInfo decoratorConstructor)
        {
            var validServiceTypeArguments =
                from parameter in decoratorConstructor.GetParameters()
                where
                    IsDecorateeDependencyParameter(parameter.ParameterType, serviceType) ||
                    IsDecorateeFactoryDependencyParameter(parameter.ParameterType, serviceType)
                select parameter;

            return validServiceTypeArguments.Count();
        }

        internal static bool DecoratesBaseTypes(Type serviceType, ConstructorInfo decoratorConstructor)
        {
            var baseTypes = GetValidDecoratorConstructorArgumentTypes(serviceType,
                decoratorConstructor.DeclaringType);

            var constructorParameters = decoratorConstructor.GetParameters();

            // For a type to be a decorator, one of its constructor parameter types must exactly match with
            // one of the interfaces it implements or base types it inherits from.
            var decoratorParameters =
                from baseType in baseTypes
                from parameter in constructorParameters
                where parameter.ParameterType == baseType ||
                    parameter.ParameterType == typeof(Func<>).MakeGenericType(baseType)
                select parameter;

            return decoratorParameters.Any();
        }

        internal static Type[] GetValidDecoratorConstructorArgumentTypes(Type serviceType, Type decoratorType)
        {
            return (
                from baseType in decoratorType.GetBaseTypesAndInterfaces()
                where IsDecorateeDependencyParameter(baseType, serviceType)
                select baseType)
                .ToArray();
        }

        // Checks if the given parameterType can function as the decorated instance of the given service type.
        private static bool IsDecorateeFactoryDependencyParameter(Type parameterType, Type serviceType)
        {
            if (!parameterType.IsGenericType || parameterType.GetGenericTypeDefinition() != typeof(Func<>))
            {
                return false;
            }

            Type funcArgumentType = parameterType.GetGenericArguments()[0];

            return IsDecorateeDependencyParameter(funcArgumentType, serviceType);
        }

        // Checks if the given parameterType can function as the decorated instance of the given service type.
        private static bool IsDecorateeDependencyParameter(Type parameterType, Type serviceType)
        {
            if (parameterType == serviceType)
            {
                return true;
            }

            return
                serviceType.IsGenericType &&
                parameterType.IsGenericType &&
                serviceType.GetGenericTypeDefinition() == parameterType.GetGenericTypeDefinition();
        }
    }
}