using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;

using MicroWrath.Util;

namespace MicroWrath.Constructors
{
    internal interface IReflectionInitializer { public Type ForType { get; } }

    internal class ReflectionInitializer<T> : IReflectionInitializer
    {
        public Type ForType => typeof(T);

        internal ReflectionInitializer(Type defaultInitializersType)
        {
            defaults = defaultInitializersType;

            FieldInitializers = GetFieldInitializers().ToArray();
            PropertyInitializers = GetPropertyInitializers().ToArray();
            TypeInitializerMethod = GetTypeInitializerMethods();
        }

        protected readonly Type defaults;

        protected Option<Func<object>> GetDefaultMemberValueGetter(Type memberType)
        {
            var property = defaults
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(pi => pi.PropertyType == memberType && pi.CanRead)
                .FirstOrDefault();

            var getValue = (property?.GetGetMethod()).ToOption().Map<MethodInfo, Func<object>>(mi => () => mi.Invoke(null, null));

            if (property is not null) MicroLogger.Debug(() => $"{typeof(T)}: Initializing {memberType} members from: {property!.Name}");

            return getValue;
        }

        protected IEnumerable<Action<T>> GetFieldInitializers()
        {
            var fieldDefaults = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(fi =>
                {
                    //MicroLogger.Debug(() => $"Looking for default value for field {fi.FieldType} {fi.Name}");
                    return GetDefaultMemberValueGetter(fi.FieldType).Map(v => (fi, v));
                })
                .Where(v => v.IsSome)
                .Select(v => v.Value!);

            foreach (var (fi, getValue) in fieldDefaults)
            {
                yield return obj => fi.SetValue(obj, getValue());
            }
        }

        protected Action<T>[] FieldInitializers;

        protected IEnumerable<Action<T>> GetPropertyInitializers()
        {
            var propertyDefaults = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(pi => pi.CanWrite)
                .Select(pi =>
                {
                    //MicroLogger.Debug(() => $"Looking for default value for property {pi.PropertyType} {pi.Name}");
                    return GetDefaultMemberValueGetter(pi.PropertyType).Map(v => (pi, v));
                })
                .Where(v => v.IsSome)
                .Select(v => v.Value!);

            foreach (var (pi, getValue) in propertyDefaults)
            {
                yield return obj => pi.SetValue(obj, getValue());
            }
        }

        protected readonly Action<T>[] PropertyInitializers;

        protected Func<T, T> GetTypeInitializerMethods()
        {
            var methods = defaults
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(mi =>
                    mi.ReturnType == typeof(T) &&
                    mi.GetParameters().Length == 1 &&
                    mi.GetParameters()[0].ParameterType == typeof(T));

            if (methods.Count() == 0)
                return Functional.Identity<T>;

            return methods
                .Select(mi => (Func<T, T>)mi.CreateDelegate(typeof(Func<T, T>)))
                .Aggregate((acc, f) => (T x) => f(acc(x)));
        }

        protected readonly Func<T, T> TypeInitializerMethod;

        public virtual T Initialize(T obj)
        {
            foreach (var f in FieldInitializers) f(obj);
            foreach (var p in PropertyInitializers) p(obj);

            return TypeInitializerMethod(obj);
        }
    }

    internal class BlueprintReflectionInitializer<TBlueprint> : ReflectionInitializer<TBlueprint>,
        Construct.IBlueprintConstructor<TBlueprint>
        where TBlueprint : SimpleBlueprint, new()
    {
        internal BlueprintReflectionInitializer(Type defaultInitializersType) : base(defaultInitializersType) { }

        public TBlueprint New(string assetId, string name)
        {
            TBlueprint bp = new();

            bp.AssetGuid = BlueprintGuid.Parse(assetId);
            bp.name = name;

            return Initialize(bp);
        }
    }

    internal class ComponentReflectionInitializer<TComponent> : ReflectionInitializer<TComponent>,
        Construct.IComponentConstructor<TComponent>
        where TComponent : BlueprintComponent, new()
    {
        public ComponentReflectionInitializer(Type defaultInitializersType) : base(defaultInitializersType) { }

        public TComponent New()
        {
            TComponent component = new();

            return Initialize(component);
        }
    }
}
