namespace MicroWrath
{
    internal static class Default
    {
        public static readonly Kingmaker.ElementsSystem.ActionList ActionLists =
            new Kingmaker.ElementsSystem.ActionList() { Actions = new Kingmaker.ElementsSystem.GameAction[0] };
        public static readonly Kingmaker.DialogSystem.CharacterSelection CharacterSelection = new Kingmaker.DialogSystem.CharacterSelection();
        public static readonly Kingmaker.ElementsSystem.ConditionsChecker ConditionsChecker =
            new Kingmaker.ElementsSystem.ConditionsChecker() { Conditions = new Kingmaker.ElementsSystem.Condition[0] };
        public static readonly Kingmaker.UnitLogic.Mechanics.ContextDiceValue ContextDiceValue = new Kingmaker.UnitLogic.Mechanics.ContextDiceValue()
        {
            DiceType = Kingmaker.RuleSystem.DiceType.Zero,
            DiceCountValue = 0,
            BonusValue = 0
        };
        public static readonly Kingmaker.DialogSystem.CueSelection CueSelection = new Kingmaker.DialogSystem.CueSelection();
        public static readonly Kingmaker.DialogSystem.DialogSpeaker DialogSpeaker = new Kingmaker.DialogSystem.DialogSpeaker() { NoSpeaker = true };
        public static readonly Kingmaker.Localization.LocalizedString LocalizedString = new Kingmaker.Localization.LocalizedString();
        public static readonly Kingmaker.ResourceLinks.PrefabLink PrefabLink = new Kingmaker.ResourceLinks.PrefabLink();
        public static readonly Kingmaker.DialogSystem.Blueprints.ShowCheck ShowCheck = new Kingmaker.DialogSystem.Blueprints.ShowCheck();
    }
}
