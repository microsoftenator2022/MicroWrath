using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;

using Newtonsoft.Json;

namespace MicroWrath.Components
{
    /// <summary>
    /// Add temporary HP with <see cref="ContextValue"/> value.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff))]
    internal class ContextAddTemporaryHP : UnitBuffComponentDelegate<ContextAddTemporaryHP.ComponentData>
    {
        public ContextValue Value = null!;
        public ModifierDescriptor Descriptor;

        public override void OnActivate()
        {
            base.OnActivate();

            var value = Value?.Calculate(base.Context) ?? 0;

            if (value == 0) return;

            base.Data.Modifier = base.Owner.Stats.TemporaryHitPoints.AddModifier(value, base.Runtime, this.Descriptor);
        }

        public override void OnTurnOff()
        {
            base.OnTurnOff();

            base.Data.Modifier?.Remove();
            base.Data.Modifier = null;
        }

        public class ComponentData
        {
            [JsonProperty]
            public ModifiableValue.Modifier? Modifier;
        }
    }
}
