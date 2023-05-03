using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;

using MicroWrath.Util;

namespace MicroWrath.Constructors
{
    internal interface IReflectionInitializer { public Type ForType { get; } }

    internal abstract class ReflectionInitializer<T> : IReflectionInitializer
    {
        public Type ForType => typeof(T);

        internal ReflectionInitializer(Type defaultInitializersType)
        {
            defaults = defaultInitializersType;
            FieldInitializers = GetFieldInitializers().ToArray();
            PropertyInitializers = GetPropertyInitializers().ToArray();
            TypeInitializer = GetTypeInitializers();
        }

        protected readonly Type defaults;
        protected Option<object> GetDefaultMemberValue(Type memberType)
        {
            var field = defaults
                .GetFields(BindingFlags.Static)
                .Where(fi => fi.FieldType == memberType)
                .FirstOrDefault();

            return (field?.GetValue(null)).ToOption();
        }

        protected IEnumerable<Action<T>> GetFieldInitializers()
        {
            var fieldDefaults = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(fi => GetDefaultMemberValue(fi.FieldType).Map(v => (fi, v)))
                .Where(v => v.IsSome)
                .Select(v => v.Value!);

            foreach (var (fi, value) in fieldDefaults)
            {
                yield return obj => fi.SetValue(obj, value);
            }
        }

        protected Action<T>[] FieldInitializers;

        protected IEnumerable<Action<T>> GetPropertyInitializers()
        {
            var propertyDefaults = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(pi => GetDefaultMemberValue(pi.PropertyType).Map(v => (pi, v)))
                .Where(v => v.IsSome)
                .Select(v => v.Value!);

            foreach (var (pi, value) in propertyDefaults)
            {
                yield return obj => pi.SetValue(obj, value);
            }
        }

        protected readonly Action<T>[] PropertyInitializers;

        protected Func<T, T> GetTypeInitializers()
        {
            var methods = defaults
                .GetMethods(BindingFlags.Static)
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

        protected readonly Func<T, T> TypeInitializer;
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

            foreach (var f in FieldInitializers) f(bp);
            foreach (var p in PropertyInitializers) p(bp);
            TypeInitializer(bp);

            return bp;
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

            foreach (var f in FieldInitializers) f(component);
            foreach (var p in PropertyInitializers) p(component);
            TypeInitializer(component);

            return component;
        }
    }
}
