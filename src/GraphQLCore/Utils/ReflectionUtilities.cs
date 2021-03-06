﻿namespace GraphQLCore.Utils
{
    using Exceptions;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class ReflectionUtilities
    {
        public static IEnumerable<TResult> ConvertEnumerable<TResult>(IEnumerable source)
        {
            foreach (var element in source)
                yield return (TResult)ChangeValueType(element, typeof(TResult));
        }

        public static object ConvertEnumerable(object source, Type targetType)
        {
            var cast = typeof(ReflectionUtilities).GetRuntimeMethod("ConvertEnumerable", new System.Type[] { typeof(IEnumerable) })
                .MakeGenericMethod(targetType);

            return cast.Invoke(null, new object[] { source });
        }

        public static Type GetCollectionMemberType(System.Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            return collectionType.GenericTypeArguments.Single();
        }

        public static Type GetGenericArgumentsEagerly(Type type)
        {
            var argument = type.GenericTypeArguments.FirstOrDefault();

            if (argument != null)
                return argument;

            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null)
                return GetGenericArgumentsEagerly(baseType);

            return argument;
        }

        public static ParameterExpression[] GetParameters(LambdaExpression resolver) => resolver.Parameters.ToArray();

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var type = typeof(TSource);

            var member = propertyLambda.Body as MemberExpression;

            if (member == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;

            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

            return propInfo;
        }

        public static System.Type GetReturnValueFromLambdaExpression(LambdaExpression expression)
        {
            return expression.Type.GenericTypeArguments.LastOrDefault();
        }

        public static object ChangeToArrayCollection(object input, Type parameterType)
        {
            var elementType = parameterType.GetElementType();

            return ToArray(elementType, ConvertEnumerable(input, elementType));
        }

        public static object ChangeToCollection(object input, Type parameterType)
        {
            if (parameterType.IsArray)
                return ChangeToArrayCollection(input, parameterType);

            return ChangeToListCollection(input, parameterType);
        }

        public static object ChangeToListCollection(object input, Type parameterType)
        {
            var elementType = parameterType.GenericTypeArguments.Single();

            return ToList(elementType, ConvertEnumerable(input, elementType));
        }

        public static object ChangeValueType(object input, Type target)
        {
            if (input == null || target == null)
                return null;

            if (input.GetType() == target)
                return input;

            var underlyingNullableType = Nullable.GetUnderlyingType(target);
            if (underlyingNullableType != null)
                return ChangeValueType(input, underlyingNullableType);

            if (IsCollection(target))
                return ChangeToCollection(input, target);

            if (IsEnum(target))
                return Enum.Parse(target, input as string);

            try
            {
                return Convert.ChangeType(input, target);
            }
            catch (Exception ex)
            {
                throw new GraphQLException($"Can't convert input of type {input.GetType().Name} to {target.Name}.", ex);
            }
        }

        public static bool IsEnum(Type type) => type.GetTypeInfo().IsEnum;

        public static bool IsStruct(Type type)
        {
            return type.GetTypeInfo().IsValueType &&
                !type.GetTypeInfo().IsPrimitive &&
                !type.Namespace.StartsWith("System") &&
                !type.GetTypeInfo().IsEnum;
        }

        public static object ToArray(Type type, object input)
        {
            var toArray = typeof(Enumerable).GetRuntimeMethods()
                .SingleOrDefault(e => e.Name == "ToArray")
                .MakeGenericMethod(type);

            return toArray.Invoke(null, new object[] { input });
        }

        public static object ToList(Type type, object input)
        {
            var toList = typeof(Enumerable).GetRuntimeMethods()
                .SingleOrDefault(e => e.Name == "ToList")
                .MakeGenericMethod(type);

            return toList.Invoke(null, new object[] { input });
        }

        internal static Type CreateListTypeOf(Type type)
        {
            var listType = typeof(List<>);
            return listType.MakeGenericType(type);
        }

        internal static List<Type> GetAllImplementingInterfaces(Type type)
        {
            var types = new List<Type>();
            while (type != null)
            {
                types.AddRange(type.GetTypeInfo().GetInterfaces());
                type = type.GetTypeInfo().BaseType;
            }

            return types;
        }

        internal static bool IsCollection(Type type)
        {
            return (type.IsArray || typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo())) && type != typeof(string);
        }

        internal static bool IsInterface(Type type) => type.GetTypeInfo().IsInterface;
    }
}