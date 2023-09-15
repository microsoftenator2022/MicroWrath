# MicroWrath

New common lib for WotR mods using Roslyn source generation aiming to automate some boilerplate.

## Get Started

Run the setup tool to set up a project with an empty, but functional, mod.

## Source generators

Most of the basic mod boilerplate is generated for you. You can see these files in Visual Studio by opening the nodes under Dependencies -> Analyzers -> MicroWrath.Generator

![image](https://github.com/microsoftenator2022/MicroWrath/assets/105488202/e67ea65e-f780-477e-8889-7ab20f9778f5)

### EmbeddedResources

These are simple embedded source files that comprise the majority of the MicroWrath API. They are internal to your mod assembly so that changes to the core MicroWrath library are less likely to affect consumers.

Many of these files' are partial classes whose implementation is filled out by other source generators

TODO: explain what these are for

### BlueprintConstructor

Contains static implementations for the BlueprintConstructor and ComponentConstructor classes

### BlueprintsDb

Generated Blueprint Name -> Blueprint Reference (`MicroWrath.OwlcatBlueprint<TBlueprint>`) mappings, grouped by blueprint type.

When you access properties under `BlueprintsDb.Owlcat`, those references are generated here.

### Conditions

Constructors for ConditionsChecker Conditions 

### GameActions

Constructors for GameActions

### GeneratedGuids

Entries in `guids.json` generate properties here. Invocations of `GeneratedGuid.Get` will also generate properties if no entry is present. After build, the `MicroWrath.Generator.GenerateGuidsFile` task will run and save them to `guids.json`.

### GeneratedMain

Contains a canonical implementation of the `IMicroMod` interface. If you provide your own implementation, this will not be generated.

### LocalizedStrings

`string` constants and `static readonly` fields and properties that are tagged with a `LocalizedStringAttribute` will cause members to be generated in the `LocalizedStrings` class. This class also adds these strings to the current `LocalizationPack` when `Triggers.LocaleChanged` fires (see below)

## Usage

TODO

### InitAttribute

Methods tagged with this attribute will be executed immediately after the mod is loaded by UMM.

```cs
[Init]
static void Init()
{
    // Initialization code here
}
```

### Triggers

There are a few default Observables that fire on common entry points:

`IObservable<Unit> Triggers.BlueprintsCache_Init_Prefix`: Triggers before `BlueprintsCache.Init`

`IObservable<Unit> Triggers.BlueprintsCache_Init`: Triggers after `BlueprintsCache.Init`

`IObservable<Unit> Triggers.BlueprintsCache_Init_Early`: Triggers after `BlueprintsCache.Init` but before the previous event. Useful for eg. initializing settings

`IObservable<Locale> Triggers.LocaleChanged`: Triggers before `LocalizationManager.OnLocaleChanged`

### BlueprintInitializationContext

TODO
