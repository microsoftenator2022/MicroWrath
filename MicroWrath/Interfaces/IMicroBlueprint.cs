using System;

using Kingmaker.Blueprints;

using MicroWrath.Util;

namespace MicroWrath
{
    /// <summary>
    /// Extension methods for <see cref="IMicroBlueprint{TBlueprint}"/>
    /// </summary>
    public static class MicroBlueprintExtensions
    {
        /// <returns><see cref="Option{TBlueprint}.Some"/> if blueprint is not null. Otherwise, <see cref="Option{TBlueprint}.None"/></returns>
        public static Option<TBlueprint> TryGetBlueprint<TBlueprint>(this IMicroBlueprint<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint =>
            bpRef.GetBlueprint().ToOption();

        /// <summary>
        /// Converts to <typeparamref name="TReference"/>
        /// </summary>
        public static TReference ToReference<TBlueprint, TReference>(this IMicroBlueprint<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint
            where TReference : BlueprintReference<TBlueprint>, new() =>
            new() { deserializedGuid =
                (bpRef as IMicroBlueprintReference)?.ToReference().deserializedGuid
                ?? bpRef.BlueprintGuid };

        //public static BlueprintReference<TBlueprint> ToReference<TBlueprint>(this IMicroBlueprint<TBlueprint> bpRef)
        //    where TBlueprint : SimpleBlueprint =>
        //    bpRef.ToReference<TBlueprint, BlueprintReference<TBlueprint>>();
    }

    /// <summary>
    /// Reference to a blueprint. This blueprint may not exist for the current initialization state.
    /// </summary>
    /// <typeparam name="TBlueprint">Blueprint type</typeparam>
    public interface IMicroBlueprint<out TBlueprint> where TBlueprint : SimpleBlueprint
    {
        /// <summary>
        /// Blueprint's <see cref="BlueprintGuid"/>.<br/>
        /// <br/>
        /// See also: <seealso cref="SimpleBlueprint.AssetGuid"/>.
        /// </summary>
        BlueprintGuid BlueprintGuid { get; }

        /// <summary>
        /// Retrieves the blueprint (<see cref="ResourcesLibrary.TryGetBlueprint(BlueprintGuid)"/>). Returns <see langword="null"/> if the blueprint is not present.
        /// </summary>
        TBlueprint? GetBlueprint();
    }

    /// <summary>
    /// <typeparamref name="TReference"/> constructor for <typeparamref name="TBlueprint"/>
    /// </summary>
    /// <typeparam name="TReference">Reference type</typeparam>
    /// <typeparam name="TBlueprint">Blueprint type</typeparam>
    public interface IMicroBlueprintReference<out TReference, out TBlueprint>
        where TBlueprint : SimpleBlueprint
        where TReference : BlueprintReferenceBase
    {
        /// <summary>
        /// Create a <typeparamref name="TReference"/> from this <see cref="IMicroBlueprintReference{TReference, TBlueprint}"/>.
        /// </summary>
        /// <returns><typeparamref name="TReference"/></returns>
        TReference ToReference();
    }
}
