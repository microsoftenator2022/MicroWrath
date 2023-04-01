using System;
using System.Collections.Generic;
using System.Text;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Localization;

using MicroWrath.Util;

namespace MicroWrath
{
    public interface IBlueprintWrapper<out TBlueprint> : IEquatable<IBlueprintWrapper<SimpleBlueprint>> where TBlueprint : SimpleBlueprint
    {
        BlueprintGuid BlueprintGuid { get; }
        TBlueprint? GetBlueprint();
        string Name { get; }
    }

    public interface IBlueprintWithDisplayName<TBlueprint> : IBlueprintWrapper<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        LocalizedString DisplayName { get; }
    }

    public interface IBlueprintWithDescription<TBlueprint> : IBlueprintWrapper<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        LocalizedString Description { get; }
    }

    public static class BlueprintWrapper
    {
        public static Option<TBlueprint> TryGetBlueprint<TBlueprint>(this IBlueprintWrapper<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint =>
            Option.OfObj(bpRef.GetBlueprint());

        public static TReference GetReference<TBlueprint, TReference>(this IBlueprintWrapper<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint
            where TReference : BlueprintReference<TBlueprint>, new() =>
            new() { deserializedGuid = bpRef.BlueprintGuid };
    }

    public readonly struct BlueprintWrapper<TBlueprint> : IBlueprintWrapper<TBlueprint>
        where TBlueprint : SimpleBlueprint
    {
        public BlueprintWrapper() { }

        public BlueprintWrapper(Guid guid, string name = "")
        {
            Name = name;
            Guid = guid;
        }

        public BlueprintWrapper(string guidString, string name = "") : this(Guid.Parse(guidString), name) { }

        public BlueprintWrapper(TBlueprint bp) : this(bp.AssetGuid.m_Guid, bp.name) { }

        public Guid Guid { get; init; }
        public BlueprintGuid BlueprintGuid => new(Guid);
        public string Name { get; init; } = "";

        TBlueprint? IBlueprintWrapper<TBlueprint>.GetBlueprint() =>
            this.GetReference<TBlueprint, BlueprintReference<TBlueprint>>().Get();

        public bool Equals(IBlueprintWrapper<SimpleBlueprint> other) => this.BlueprintGuid.Equals(other.BlueprintGuid);

        public override int GetHashCode() => -737073652 + Guid.GetHashCode();
        public override bool Equals(object obj) => base.Equals(obj);
        public override string ToString() => $"{{(name: {Name}) (guid: {Guid})}}";

        public static bool operator == (BlueprintWrapper<TBlueprint> a, IBlueprintWrapper<SimpleBlueprint> b) => a.Equals(b);
        public static bool operator != (BlueprintWrapper<TBlueprint> a, IBlueprintWrapper<SimpleBlueprint> b) => !a.Equals(b);
    }
}
