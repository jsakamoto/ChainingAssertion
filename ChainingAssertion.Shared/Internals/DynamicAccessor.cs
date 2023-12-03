using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace ChainingAssertion.Shared.Internals
{
    internal class DynamicAccessor<T> : DynamicObject
    {
        private readonly T target;
        private static readonly BindingFlags TransparentFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public DynamicAccessor(T target)
        {
            this.target = target;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            try
            {
                typeof(T).InvokeMember("Item", TransparentFlags | BindingFlags.SetProperty, null, this.target, indexes.Concat(new[] { value }).ToArray());
                return true;
            }
            catch (MissingMethodException) { throw new ArgumentException(string.Format("indexer not found : Type <{0}>", typeof(T).Name)); };
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            try
            {
                result = typeof(T).InvokeMember("Item", TransparentFlags | BindingFlags.GetProperty, null, this.target, indexes);
                return true;
            }
            catch (MissingMethodException) { throw new ArgumentException(string.Format("indexer not found : Type <{0}>", typeof(T).Name)); };
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var accessor = new ReflectAccessor<T>(this.target, binder.Name);
            accessor.SetValue(value);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var accessor = new ReflectAccessor<T>(this.target, binder.Name);
            result = accessor.GetValue();
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var csharpBinder = binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
            if (csharpBinder == null) throw new ArgumentException("is not csharp code");

            var typeArgs = (csharpBinder.GetProperty("TypeArguments").GetValue(binder, null) as IList<Type>).ToArray();
            var method = this.MatchMethod(binder.Name, args, typeArgs);
            result = method.Invoke(this.target, args);

            return true;
        }

        private Type AssignableBoundType(Type left, Type right)
        {
            return (left == null || right == null) ? null
                : left.IsAssignableFrom(right) ? left
                : right.IsAssignableFrom(left) ? right
                : null;
        }

        private class TypedMethod
        {
            public MethodInfo MethodInfo { get; }

            public Dictionary<string, Type> GenericParamTypes { get; } = new Dictionary<string, Type>();

            public Type[] GenericArguments { get; }

            public int Score { get; }

            public TypedMethod(MethodInfo methodInfo, Type[] typeArgs, Type[] parameterTypes)
            {
                this.MethodInfo = methodInfo;
                this.GenericArguments = methodInfo.GetGenericArguments();
                this.Score = -1;

                var rawParamTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
                if (rawParamTypes.Length != parameterTypes.Length) return;

                // Instantiate generic type argument types
                if (typeArgs.Any())
                {
                    foreach (var typeArg in this.GenericArguments.Select((ga, index) => Tuple.Create(ga.Name, typeArgs[index])))
                    {
                        this.GenericParamTypes.Add(typeArg.Item1, typeArg.Item2);
                    }
                }

                // Instantiate method parameter types
                var methodParamTypes = rawParamTypes
                    .Select((methodParamType, index) =>
                    {
                        return this.InstantinateType(methodParamType, parameterTypes[index]);
                    });

                // Scoring
                var scores = methodParamTypes.Zip(parameterTypes, Tuple.Create).Select((pair, index) =>
                {
                    var methodParamType = pair.Item1;
                    var parameterType = pair.Item2;
                    var isGenericParam = rawParamTypes[index].IsGenericParameter;
                    if (parameterType == null && methodParamType?.IsValueType == false) return isGenericParam ? 1 : 10;
                    if (parameterType == methodParamType) return isGenericParam ? 100 : 1000;
                    if (methodParamType?.IsAssignableFrom(parameterType) == true) return isGenericParam ? 1 : 10;
                    return -1;
                });
                this.Score = scores.Any(s => s == -1) ? -1 : scores.DefaultIfEmpty(0).Sum();
            }

            private Type InstantinateType(Type srcType, Type parameterType)
            {
                if (!srcType.IsGenericParameter)
                {
                    if (!srcType.ContainsGenericParameters) return srcType;

                    if (this.TryMakeGenericType(srcType, out var result)) return result;

                    var underlieingType =
                        (parameterType.ContainsGenericParameters && srcType.GenericTypeArguments.Length == parameterType.GenericTypeArguments.Length) ? parameterType :
                        srcType.IsInterface ? (parameterType.IsInterface ? parameterType : parameterType.GetInterface(srcType.Name)) :
                        null;

                    if (underlieingType == null) return srcType;

                    var typeArgs = Enumerable.Zip(srcType.GenericTypeArguments, underlieingType.GenericTypeArguments, Tuple.Create)
                        .Select(pair => this.InstantinateType(pair.Item1, pair.Item2))
                        .ToArray();

                    return srcType.GetGenericTypeDefinition().MakeGenericType(typeArgs);
                }

                if (this.GenericParamTypes.TryGetValue(srcType.Name, out var type)) return type;
                if (parameterType != null) this.GenericParamTypes.Add(srcType.Name, parameterType);
                return parameterType;
            }

            private bool TryMakeGenericType(Type srcType, out Type result)
            {
                var typeArgs = srcType.GenericTypeArguments
                    .Select(t => this.GenericParamTypes.TryGetValue(t.Name, out var typeArg) ? typeArg : null)
                    .ToArray();
                if (!typeArgs.Any(t => t == null))
                {
                    result = srcType.GetGenericTypeDefinition().MakeGenericType(typeArgs);
                    return true;
                }

                result = null;
                return false;
            }
        }

        private MethodInfo MatchMethod(string methodName, object[] args, Type[] typeArgs)
        {
            var parameterTypes = args.Select(a => a?.GetType()).ToArray();

            // name match
            var nameMatched = typeof(T).GetMethods(TransparentFlags)
                .Where(mi => mi.Name == methodName)
                .ToArray();
            if (!nameMatched.Any()) throw new ArgumentException(string.Format("\"{0}\" not found : Type <{1}>", methodName, typeof(T).Name));

            // type inference
            var typedMethods = nameMatched
                .Select(mi =>
                {
                    var genericArguments = mi.GetGenericArguments();

                    if (!typeArgs.Any() && !genericArguments.Any()) // non generic method
                    {
                        return new TypedMethod(mi, typeArgs, parameterTypes);
                    }
                    else if (!typeArgs.Any())
                    {
                        //var parameterGenericTypes = mi.GetParameters()
                        //    .Select(pi => pi.ParameterType)
                        //    .Zip(parameterTypes, Tuple.Create)
                        //    .GroupBy(a => a.Item1, a => a.Item2)
                        //    .Where(g => g.Key.IsGenericParameter)
                        //    .Select(g => new { g.Key, Type = g.Aggregate(this.AssignableBoundType) })
                        //    .Where(a => a.Type != null);

                        //var typeParams = genericArguments
                        //    .GroupJoin(parameterGenericTypes, x => x, x => x.Key, (_, Args) => Args);
                        //if (!typeParams.All(xs => xs.Any())) return null; // types short

                        return new TypedMethod(mi, typeArgs, parameterTypes);
                    }
                    else
                    {
                        if (genericArguments.Length != typeArgs.Length) return null;

                        return new TypedMethod(mi, typeArgs, parameterTypes);
                    }
                })
                .Where(a => a != null)
                .Where(a => a.Score >= 0)
                .OrderBy(a => a.GenericArguments.Length)
                .ThenByDescending(a => a.Score)
                .Take(2)
                .ToArray();

            if (!typedMethods.Any()) throw new ArgumentException(string.Format("\"{0}\" not match arguments : Type <{1}>", methodName, typeof(T).Name));

            var firstMethod = typedMethods[0];
            if (typedMethods.Length > 1)
            {
                var secondMethod = typedMethods[1];
                if (firstMethod.GenericArguments.Length == secondMethod.GenericArguments.Length && firstMethod.Score == secondMethod.Score)
                    throw new ArgumentException(string.Format("\"{0}\" ambiguous arguments : Type <{1}>", methodName, typeof(T).Name));
            }

            if (firstMethod.GenericArguments.Length == 0)
            {
                return firstMethod.MethodInfo;
            }
            else
            {
                var typeArguments = firstMethod.GenericArguments.Select(a => firstMethod.GenericParamTypes[a.Name]).ToArray();
                return firstMethod.MethodInfo.MakeGenericMethod(typeArguments);
            }
        }

        private class EqualsComparer<TX> : IEqualityComparer<TX>
        {
            private readonly Func<TX, TX, bool> equals;

            public EqualsComparer(Func<TX, TX, bool> equals)
            {
                this.equals = equals;
            }

            public bool Equals(TX x, TX y)
            {
                return this.equals(x, y);
            }

            public int GetHashCode(TX obj)
            {
                return 0;
            }
        }
    }

}
