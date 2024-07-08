using System;
using System.IO;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;

using MicroWrath;
using MicroWrath.Util;

namespace MicroWrath
{
    /// <summary>
    /// Extensions for <see cref="IMicroBlueprint{TBlueprint}"/>
    /// </summary>
    internal static class MicroBlueprint
    {
        public static IMicroBlueprint<TBlueprint> ToMicroBlueprint<TBlueprint>(this TBlueprint blueprint)
            where TBlueprint : SimpleBlueprint => new MicroBlueprint<TBlueprint>(blueprint.AssetGuid);

        public static IMicroBlueprint<TBlueprint> ToMicroBlueprint<TBlueprint>(this BlueprintReference<TBlueprint> reference)
            where TBlueprint : SimpleBlueprint => new MicroBlueprint<TBlueprint>(reference.guid);
    }

    /// <summary>
    /// A safe(r) wrapper/proxy for <see cref="BlueprintReference{TBlueprint}"/>
    /// </summary>
    internal readonly record struct MicroBlueprint<TBlueprint> : IMicroBlueprint<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        public MicroBlueprint(string assetId)
        {
            this.AssetId = assetId;
            this.BlueprintGuid = BlueprintGuid.Parse(AssetId);
        }

        public MicroBlueprint(BlueprintGuid guid)
        {
            this.BlueprintGuid = guid;
            this.AssetId = guid.ToString();
        }

        public readonly string AssetId;

        /// <summary>
        /// Create a <typeparamref name="TReference"/> from this <see langword="object"/>.
        /// </summary>
        public TReference ToReference<TReference>() where TReference : BlueprintReference<TBlueprint>, new() =>
            this.ToReference<TBlueprint, TReference>();

        private TBlueprint? MaybeBlueprint { get; init; } = null;

        /// <summary>
        /// Blueprint name or <see cref="BlueprintReference{T}.NameSafe"/> if it is not loaded.
        /// </summary>
        public string Name => this.MaybeBlueprint?.name ?? this.ToReference<BlueprintReference<TBlueprint>>().NameSafe();

        /// <summary>
        /// Guid for this blueprint
        /// </summary>
        public BlueprintGuid BlueprintGuid { get; }

        /// <inheritdoc cref="IMicroBlueprint{TBlueprint}.GetBlueprint"/>
        public TBlueprint? GetBlueprint() => this.ToReference<BlueprintReference<TBlueprint>>().Get();

        public override string ToString()
        {
            if (this.MaybeBlueprint is not null)
                return $"{typeof(TBlueprint)} {this.BlueprintGuid} ({this.Name})";

            return $"{typeof(TBlueprint)} {this.BlueprintGuid}";
        }

        //public static implicit operator MicroBlueprint<TBlueprint>(TBlueprint blueprint) =>
        //    new(blueprint.AssetGuid) { MaybeBlueprint = blueprint };

        /// <summary>
        /// Implicit conversion from <see cref="BlueprintReference{TBlueprint}"/>
        /// </summary>
        public static implicit operator MicroBlueprint<TBlueprint>(BlueprintReference<TBlueprint> blueprintReference) =>
            new(blueprintReference.Guid);
    }

    internal readonly record struct OwlcatBlueprint<TBlueprint> : IMicroBlueprint<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        public OwlcatBlueprint(string guidString)
        {
            this.BlueprintGuid = BlueprintGuid.Parse(guidString);
        }

        /// <inheritdoc cref="MicroBlueprint{TBlueprint}.BlueprintGuid"/>
        public BlueprintGuid BlueprintGuid { get; init; }

        /// <inheritdoc cref="MicroBlueprint{TBlueprint}.ToReference{TReference}"/>
        public TReference ToReference<TReference>() where TReference : BlueprintReference<TBlueprint>, new() =>
            this.ToReference<TBlueprint, TReference>();

        /// <summary>
        /// Get this blueprint. This should only return null if <see cref="BlueprintsCache.Init"/> has not yet run.
        /// </summary>
        /// <returns>Referenced blueprint</returns>
        public TBlueprint Blueprint => this.ToReference<BlueprintReference<TBlueprint>>().Get();

        /// <summary>
        /// Fetches the original blueprint from the blueprints pack with no patches applied by any mod (including this one)
        /// </summary>
        /// <returns>The original blueprint</returns>
        public TBlueprint GetOriginalBlueprint()
        {
            SimpleBlueprint? blueprint = null;

            lock (ResourcesLibrary.BlueprintsCache.m_Lock)
            {
                try
                {
                    var cacheEntry = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints[this.BlueprintGuid];
                    ResourcesLibrary.BlueprintsCache.m_PackFile.Seek(cacheEntry.Offset, SeekOrigin.Begin);

                    ResourcesLibrary.BlueprintsCache.m_PackSerializer.Blueprint(ref blueprint);
                }
                catch (Exception ex)
                {
                    MicroLogger.Error($"Failed to load blueprint {this.BlueprintGuid}", ex);
                }
            }

            return (blueprint as TBlueprint)!;
        }

        /// <inheritdoc cref="MicroBlueprint{TBlueprint}.Name"/>
        public string Name => this.ToReference<BlueprintReference<TBlueprint>>().NameSafe();

        /// <inheritdoc cref="IMicroBlueprint{TBlueprint}.GetBlueprint"/>
        TBlueprint? IMicroBlueprint<TBlueprint>.GetBlueprint() => this.Blueprint;

        public override string ToString() => $"{typeof(TBlueprint)} {this.BlueprintGuid} ({this.Name})";
    }
}
