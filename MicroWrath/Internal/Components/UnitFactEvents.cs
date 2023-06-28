using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;

using UniRx;

namespace MicroWrath.Components
{
    internal abstract class UnitFactEvent : Kingmaker.UnitLogic.UnitFactComponentDelegate
    {
        protected event Action ActivateEvent = () => { };
        public override void OnActivate() => ActivateEvent();

        protected event Action DeactivateEvent = () => { };
        public override void OnDeactivate() => DeactivateEvent();

        protected event Action DisposeEvent = () => { };
        public override void OnDispose() => DisposeEvent();

        protected event Action FactAttachedEvent = () => { };
        public override void OnFactAttached() => FactAttachedEvent();

        protected event Action InitializeEvent = () => { };
        public override void OnInitialize() => InitializeEvent();

        protected event Action ApplyPostLoadFixesEvent = () => { };
        public override void OnApplyPostLoadFixes() => ApplyPostLoadFixesEvent();

        protected event Action PostLoadEvent = () => { };
        public override void OnPostLoad() => PostLoadEvent();

        protected event Action PreSaveEvent = () => { };
        public override void OnPreSave() => PreSaveEvent();

        protected event Action RecalculateEvent = () => { };
        public override void OnRecalculate() => RecalculateEvent();

        protected event Action TurnOffEvent = () => { };
        public override void OnTurnOff() => TurnOffEvent();

        protected event Action TurnOnEvent = () => { };
        public override void OnTurnOn() => TurnOnEvent();

        protected event Action ViewDidAttachEvent = () => { };
        public override void OnViewDidAttach() => ViewDidAttachEvent();

        protected event Action ViewWillDetachEvent = () => { };
        public override void OnViewWillDetach() => ViewWillDetachEvent();
    }

    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowedOn(typeof(BlueprintUnit))]
    internal class UnitFactActivateEvent : UnitFactEvent
    {
        public UnitFactActivateEvent(Action<UnitFactActivateEvent> handler)
        {
            this.handler = () => handler(this);

            ActivateEvent += this.handler;
        }

        protected readonly Action handler;
    }
}
