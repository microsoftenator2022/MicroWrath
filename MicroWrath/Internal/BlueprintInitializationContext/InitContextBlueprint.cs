using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;

using MicroWrath;
using MicroWrath.Constructors;

namespace MicroWrath.BlueprintInitializationContext
{
    internal partial class BlueprintInitializationContext
    {
        private interface IInitContextBlueprint
        {
            string Name { get; }
            BlueprintGuid BlueprintGuid { get; }
            SimpleBlueprint CreateNew();
            SimpleBlueprint Blueprint { get; }
        }

        private class InitContextBlueprint<TBlueprint> : IMicroBlueprint<TBlueprint>, IInitContextBlueprint
            where TBlueprint : SimpleBlueprint, new()
        {
            public readonly string AssetId;
            public readonly string Name;

            public BlueprintGuid BlueprintGuid { get; }

            TBlueprint? blueprint = null;

            SimpleBlueprint IInitContextBlueprint.Blueprint
            {
                get
                {
                    if (blueprint is null)
                        MicroLogger.Warning(
                            $"{typeof(InitContextBlueprint<TBlueprint>)} {AssetId} ({Name}) " +
                            $"blueprint accessed before it is created");

                    return blueprint ??= CreateNew();
                }
            }

            string IInitContextBlueprint.Name => Name;
            public TBlueprint CreateNew()
            {
                MicroLogger.Debug(() => $"Create new {typeof(TBlueprint)} {AssetId} {Name}");

                blueprint ??= Construct.New.Blueprint<TBlueprint>(AssetId, Name);

                return blueprint;
            }

            SimpleBlueprint IInitContextBlueprint.CreateNew() => this.CreateNew();

            internal InitContextBlueprint(string assetId, string name)
            {
                AssetId = assetId;
                Name = name;
                BlueprintGuid = BlueprintGuid.Parse(assetId);
            }

            internal InitContextBlueprint(BlueprintGuid guid, string name)
            {
                Name = name;
                BlueprintGuid = guid;
                AssetId = guid.ToString();
            }

            BlueprintGuid IMicroBlueprint<TBlueprint>.BlueprintGuid => BlueprintGuid;

            TBlueprint? IMicroBlueprint<TBlueprint>.GetBlueprint() => this.TryGetBlueprint().Value;
        }
    }
}
