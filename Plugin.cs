using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace Fs45Meridian
{
    /// <summary>
    /// Offers a hand-curated Meridian Works / Aryx Weaponry Expansion loadout on
    /// the FS-45 Specter by adding the named mounts to its internal bay sets.
    /// Inert unless both Meridian Works and the FS-45 Specter are installed.
    /// </summary>
    [BepInPlugin(Guid, "FS-45 Meridian Loadout", "1.1.0")]
    [BepInDependency(MeridianGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(BlueprinterGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Fs45MeridianPlugin : BaseUnityPlugin
    {
        public const string Guid = "ki.fs45meridian";
        public const string MeridianGuid = "com.meridianworks";
        public const string BlueprinterGuid = "com.nikkorap.blueprinter";
        public const string AryxGuid = "AryxWeaponryExpansion";

        internal static Fs45MeridianPlugin Instance;

        /// <summary>Shared log source; Logger itself is protected on BaseUnityPlugin.</summary>
        internal ManualLogSource ModLog;

        internal ConfigEntry<string> InternalBayName;
        internal ConfigEntry<string> IrmBayName;
        internal ConfigEntry<float> InternalBayLength;
        internal ConfigEntry<float> IrmBayLength;
        internal ConfigEntry<bool> EnforceBayLength;
        internal ConfigEntry<string> IgnoredSetNames;
        internal ConfigEntry<string> InternalWeapons;
        internal ConfigEntry<string> IrmWeapons;
        internal ConfigEntry<bool> EnableMeridian;
        internal ConfigEntry<bool> EnableAryx;
        internal ConfigEntry<bool> LogHardpoints;

        private Harmony harmony;
        private readonly HashSet<string> logged = new HashSet<string>(StringComparer.Ordinal);

        private void Awake()
        {
            Instance = this;
            ModLog = Logger;

            var meridianPresent = Guard.MeridianOnDisk();
            var specterPresent = Guard.SpecterOnDisk();
            if (!meridianPresent || !specterPresent)
            {
                Logger.LogWarning("[FS-45 Meridian] Disabled: " +
                    (!meridianPresent ? "Meridian Works was not found" : "the FS-45 Specter bundle was not found") +
                    " under the plugins folder. Install the missing mod and restart.");
                return;
            }

            BindConfig();

            harmony = new Harmony(Guid);
            var afterLoad = typeof(Encyclopedia).GetMethod(
                "AfterLoad", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (afterLoad == null)
            {
                Logger.LogError("[FS-45 Meridian] Encyclopedia.AfterLoad() was not found; loadout options were not installed.");
                return;
            }

            harmony.Patch(afterLoad, postfix: new HarmonyMethod(typeof(Fs45MeridianPlugin), nameof(OnEncyclopediaLoaded)));
            PatchBlueprinter();
            Logger.LogInfo("[FS-45 Meridian] Watching the Encyclopedia for the FS-45 Specter.");
        }

        private void BindConfig()
        {
            InternalBayName = Config.Bind("Bays", "InternalBayName", LoadoutCatalog.DefaultInternalBay,
                "Name of the FS-45 hardpoint set that holds the long internal weapon bays.");
            IrmBayName = Config.Bind("Bays", "IrmBayName", LoadoutCatalog.DefaultIrmBay,
                "Name of the FS-45 hardpoint set that holds the short IRM bays.");
            InternalBayLength = Config.Bind("Bays", "InternalBayLength", 4.1f,
                new ConfigDescription("Usable length of the internal weapon bays in metres.",
                    new AcceptableValueRange<float>(0f, 20f)));
            IrmBayLength = Config.Bind("Bays", "IrmBayLength", 1.5f,
                new ConfigDescription("Usable length of the IRM bays in metres.",
                    new AcceptableValueRange<float>(0f, 20f)));
            EnforceBayLength = Config.Bind("Bays", "EnforceBayLength", true,
                "Skip a configured round whose definition length exceeds its bay length.");
            IgnoredSetNames = Config.Bind("Bays", "IgnoredSetNames", LoadoutCatalog.DefaultIgnored,
                "Comma-separated hardpoint set names that never receive loadout options.");
            InternalWeapons = Config.Bind("Loadout", "InternalWeapons", LoadoutCatalog.DefaultInternalWeapons,
                "Comma-separated mount jsonKeys offered in the internal weapon bays. One round per slot unless a rack is named.");
            IrmWeapons = Config.Bind("Loadout", "IrmWeapons", LoadoutCatalog.DefaultIrmWeapons,
                "Comma-separated mount jsonKeys offered in the IRM bays.");
            EnableMeridian = Config.Bind("Sources", "EnableMeridian", true,
                "Offer the Meridian Works keys in the loadout lists.");
            EnableAryx = Config.Bind("Sources", "EnableAryx", true,
                "Offer the Aryx Weaponry Expansion keys in the loadout lists.");
            LogHardpoints = Config.Bind("Diagnostics", "LogHardpoints", true,
                "Include the FS-45's hardpoint set names in the registration log line.");
        }

        internal void LogOnce(string message)
        {
            if (logged.Add(message)) Logger.LogInfo("[FS-45 Meridian] " + message);
        }

        private void PatchBlueprinter()
        {
            var runnerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Blueprinter.PatchRunner", false))
                .FirstOrDefault(type => type != null);
            if (runnerType == null) return;

            var applyAll = AccessTools.Method(runnerType, "ApplyAllOps");
            if (applyAll == null)
            {
                Logger.LogWarning("[FS-45 Meridian] Blueprinter.PatchRunner.ApplyAllOps() was not found; only the Encyclopedia hook is active.");
                return;
            }

            harmony.Patch(applyAll, postfix: new HarmonyMethod(typeof(Fs45MeridianPlugin), nameof(OnBlueprinterOpsApplied)));
        }

        private static void OnEncyclopediaLoaded(Encyclopedia __instance)
        {
            try { HardpointInjector.Inject(__instance); }
            catch (Exception error) { Instance?.ModLog.LogError("[FS-45 Meridian] " + error); }
        }

        private static void OnBlueprinterOpsApplied(Encyclopedia __0)
        {
            try { HardpointInjector.Inject(__0); }
            catch (Exception error) { Instance?.ModLog.LogError("[FS-45 Meridian] " + error); }
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
            Instance = null;
        }
    }
}
