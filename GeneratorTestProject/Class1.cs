using System;
using System.Linq;
using System.Collections.Generic;

using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;

using MicroWrath.Localization;
using MicroWrath.BlueprintsDb;
using MicroWrath.Constructors;

namespace GeneratorTestProject
{
    public class Class1
    {
        [LocalizedString(Locale = Locale.enGB)]
        internal const string TestString = "This is a string";
        [LocalizedString(Key = "SomeKey", Name = "TestString1")]
        internal const string TestString1 = "This is a string";
        [LocalizedString(Key = "SomeKey", Locale = Locale.enGB)]
        internal const string TestString2 = "This is a string";
        [LocalizedString]
        internal const string TestString3 = "This is a string";

        private string privateString = "";

        private void P()
        {
            var alchemist = BlueprintsDb.Owlcat.BlueprintCharacterClass.AlchemistClass;
            var adamantine = BlueprintsDb.Owlcat.BlueprintWeaponEnchantment.AdamantineWeaponEnchantment;
            var halfElf = BlueprintsDb.Owlcat.BlueprintRace.HalfElfRace;

            Construct.New.Blueprint<SimpleBlueprint>("", "");
            Construct.New.Blueprint<BlueprintFeature>("", "");
            Construct.New.Blueprint<BlueprintArchetype>("", "");
        }
    }
}
