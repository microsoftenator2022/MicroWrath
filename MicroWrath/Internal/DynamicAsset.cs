using System;
using System.Collections.Generic;

using HarmonyLib;

using Kingmaker.BundlesLoading;
using Kingmaker.ResourceLinks;

using UnityEngine;


namespace MicroWrath.Util.Assets
{
    internal static class Dynamic
    {
        private interface IDynamicAssetLink
        {
            WeakResourceLink Link { get; }
            Action<UnityEngine.Object> Init { get; }
            UnityEngine.Object CreateObject();
            Type AssetType { get; }
            Type LinkType { get; }
        }

        private abstract class DynamicAssetLink<T, TLink> : IDynamicAssetLink
            where T : UnityEngine.Object
            where TLink : WeakResourceLink<T>, new()
        {
            public virtual Type AssetType => typeof(T);
            public virtual Type LinkType => typeof(TLink);

            public virtual TLink Link { get; }
            
            WeakResourceLink IDynamicAssetLink.Link => Link;
            public virtual Action<T> Init { get; }
            Action<UnityEngine.Object> IDynamicAssetLink.Init => obj =>
            {
                if (obj is not T t)
                    throw new InvalidCastException();

                    Init(t);
            };

            public DynamicAssetLink(TLink assetLink, Action<T> init)
            {
                Init = init;
                Link = assetLink;
            }

            protected abstract T CloneObject(T obj);

            public virtual UnityEngine.Object CreateObject()
            {
                if (Link.LoadObject() is not T obj)
                    throw new Exception($"Failed to instantiate asset from {Link.AssetId}");

                var copy = CloneObject(obj);
                Init(copy);

                return copy;
            }
        }

        private class DynamicGameObjectLink<TLink> : DynamicAssetLink<GameObject, TLink>
            where TLink : WeakResourceLink<GameObject>, new()
        {
            protected override GameObject CloneObject(GameObject obj)
            {
                var copy = GameObject.Instantiate(obj);

                UnityEngine.Object.DontDestroyOnLoad(copy);

                return copy;
            }

            public DynamicGameObjectLink(TLink link, Action<GameObject> init) : base(link, init) { }
        }

        private class DynamicMonobehaviourLink<T, TLink> : DynamicAssetLink<T, TLink>
            where T : MonoBehaviour
            where TLink : WeakResourceLink<T>, new()
        {
            protected override T CloneObject(T obj)
            {
                MicroLogger.Debug(() => $"Trying to clone {obj.gameObject}");

                var copy = GameObject.Instantiate(obj.gameObject);
                copy.SetActive(false);

                UnityEngine.Object.DontDestroyOnLoad(copy);

                var component = copy.GetComponent<T>();

                MicroLogger.Debug(() => $"GetComponent |{typeof(T)}| = {component?.ToString() ?? "<null>"}");

                return component;
            }

            public DynamicMonobehaviourLink(TLink link, Action<T> init) : base(link, init) { }
        }

        private static readonly Dictionary<string, IDynamicAssetLink> DynamicAssetLinks = new();

        private static TLink CreateDynamicAssetLinkProxy<TLink>(IDynamicAssetLink proxy, string? assetId = null)
            where TLink : WeakResourceLink, new()
        {
            if (string.IsNullOrEmpty(assetId))
                assetId = null;

            assetId ??= Guid.NewGuid().ToString("N").ToLowerInvariant();

            DynamicAssetLinks.Add(assetId, proxy);

            return new() { AssetId = assetId };
        }

        /// <summary>
        /// Creates a dynamic proxy for a WeakResourceLink&lt;GameObject&gt; (eg. PrefabLink)
        /// </summary>
        /// <param name="link">A <typeparamref name="TLink"/> link.</param>
        /// <param name="init">Initialization function to be executed on asset load.</param>
        /// <param name="assetId">Asset ID for the new link. Will be set to a new guid if absent or null.</param>
        /// <returns></returns>
        public static TLink CreateDynamicProxy<TLink>(this TLink link, Action<GameObject> init, string? assetId = null)
            where TLink : WeakResourceLink<GameObject>, new() =>
            CreateDynamicAssetLinkProxy<TLink>(new DynamicGameObjectLink<TLink>(link, init), assetId);

        /// <summary>
        /// Creates a dynamic proxy for a MonoBehaviour WeakResourceLink.
        /// This is for assets that are components rather than GameObjects.
        /// <list>
        ///     <item>
        ///         <term>FamiliarLink</term>
        ///         <description><typeparamref name="T"/> = Familiar</description>
        ///     </item>
        ///     <item>
        ///         <term>Link</term>
        ///         <description><typeparamref name="T"/> = TacticalMapObstacle</description>
        ///     </item>
        ///     <item>
        ///         <term>ProjectileLink</term>
        ///         <description><typeparamref name="T"/> = ProjectileView</description>
        ///     </item>
        ///     <item>
        ///         <term>UnitViewLink</term>
        ///         <description><typeparamref name="T"/> = UnitEntityView</description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Asset Type.</typeparam>
        /// <param name="link">A <typeparamref name="TLink"/> link.</param>
        /// <param name="init">Initialization function to be executed on asset load.</param>
        /// <param name="assetId">Asset ID for the new link. Will be set to a new guid if absent or null.</param>
        /// <returns></returns>
        public static TLink CreateDynamicMonobehaviourProxy<T, TLink>(this TLink link, Action<T> init, string? assetId = null)
            where T : MonoBehaviour
            where TLink : WeakResourceLink<T>, new() =>
            CreateDynamicAssetLinkProxy<TLink>(new DynamicMonobehaviourLink<T, TLink>(link, init), assetId);

        [HarmonyPatch]
        static class Patches
        {
            [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string), typeof(Type))]
            [HarmonyPrefix]
            static bool LoadAsset_Prefix(string name, ref UnityEngine.Object __result)
            {
                try
                {
                    if (DynamicAssetLinks.ContainsKey(name))
                    {
                        var assetProxy = DynamicAssetLinks[name];
                        MicroLogger.Log($"Creating dynamic asset: {name} -> {assetProxy.Link.AssetId}");
                        
                        var copy = assetProxy.CreateObject();
                        
                        if (copy is MonoBehaviour mb)
                            __result = mb.gameObject;
                        else
                            __result = copy;

                        return false;
                    }
                }
                catch (Exception e)
                {
                    MicroLogger.Error($"Failed to load asset: {name}", e);
                }

                return true;
            }

            [HarmonyPatch(typeof(BundlesLoadService), nameof(BundlesLoadService.GetBundleNameForAsset))]
            [HarmonyPrefix]
            static bool GetBundleNameForAsset_Prefix(string assetId, ref string __result, BundlesLoadService __instance)
            {
                try
                {
                    if (DynamicAssetLinks.ContainsKey(assetId))
                    {
                        var assetProxy = DynamicAssetLinks[assetId];

                        MicroLogger.Log($"Getting bundle for dynamic asset {assetId} -> {assetProxy.Link.AssetId}");

                        __result = __instance.GetBundleNameForAsset(assetProxy.Link.AssetId);
                        return false;
                    }
                }
                catch (Exception e)
                {
                    MicroLogger.Error($"Failed to fetch bundle name for {assetId}.", e);
                }
                return true;
            }
        }
    }
}
