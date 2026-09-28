using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fs45Meridian
{
    /// <summary>Adds the curated loadouts to the FS-45 Specter's internal bays.</summary>
    internal static class HardpointInjector
    {
        private static readonly string[] LookupKeys = { "f117a", "f-117a", "fs45", "fs-45", "fs_45", "f117" };

        internal static void Inject(Encyclopedia encyclopedia)
        {
            var plugin = Fs45MeridianPlugin.Instance;
            if (plugin == null) return;

            var enc = encyclopedia ?? Encyclopedia.i;
            if (enc == null) return;

            if (!Guard.MeridianLoaded())
            {
                plugin.LogOnce("Meridian Works is not loaded; nothing to add.");
                return;
            }

            var definition = FindSpecter(enc);
            if (definition == null || definition.unitPrefab == null)
            {
                plugin.LogOnce("The FS-45 Specter aircraft was not found in the Encyclopedia; nothing to add.");
                return;
            }

            var aircraft = definition.unitPrefab.GetComponent<Aircraft>();
            var manager = aircraft != null
                ? aircraft.weaponManager
                : definition.unitPrefab.GetComponentInChildren<WeaponManager>(true);
            if (manager == null || manager.hardpointSets == null || manager.hardpointSets.Length == 0)
            {
                plugin.LogOnce("The FS-45 Specter prefab has no WeaponManager hardpoint sets; nothing to add.");
                return;
            }

            var internalBay = LoadoutCatalog.Normalize(plugin.InternalBayName.Value);
            var irmBay = LoadoutCatalog.Normalize(plugin.IrmBayName.Value);
            var ignored = LoadoutCatalog.Split(plugin.IgnoredSetNames.Value);
            for (var i = 0; i < ignored.Length; i++) ignored[i] = LoadoutCatalog.Normalize(ignored[i]);

            var internalMounts = Resolve(enc, plugin.InternalWeapons.Value, plugin.InternalBayLength.Value,
                plugin.EnforceBayLength.Value, plugin);
            var irmMounts = Resolve(enc, plugin.IrmWeapons.Value, plugin.IrmBayLength.Value,
                plugin.EnforceBayLength.Value, plugin);

            var added = 0;
            var setsTouched = 0;
            var names = new List<string>();
            foreach (var set in manager.hardpointSets)
            {
                if (set == null) continue;
                names.Add(set.name);

                var normalized = LoadoutCatalog.Normalize(set.name);
                if (IsIgnored(ignored, normalized)) continue;

                List<WeaponMount> wanted = null;
                if (normalized.Length > 0 && normalized == internalBay) wanted = internalMounts;
                else if (normalized.Length > 0 && normalized == irmBay) wanted = irmMounts;
                if (wanted == null) continue;

                if (set.weaponOptions == null) set.weaponOptions = new List<WeaponMount>();
                var touched = false;
                foreach (var mount in wanted)
                {
                    if (mount == null || ContainsKey(set.weaponOptions, mount.jsonKey)) continue;
                    set.weaponOptions.Add(mount);
                    added++;
                    touched = true;
                }
                if (touched) setsTouched++;
            }

            if (added == 0)
            {
                plugin.LogOnce("The FS-45 Specter already carries every configured loadout option.");
                return;
            }

            plugin.ModLog.LogInfo("[FS-45 Meridian] Added " + added + " loadout option(s) across " +
                setsTouched + " of " + manager.hardpointSets.Length + " hardpoint set(s)" +
                (plugin.LogHardpoints.Value ? " [" + string.Join(", ", names.ToArray()) + "]" : "") + ".");
        }

        private static List<WeaponMount> Resolve(Encyclopedia enc, string list, float bayLength, bool enforce,
            Fs45MeridianPlugin plugin)
        {
            var mounts = new List<WeaponMount>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var key in LoadoutCatalog.Split(list))
            {
                if (!seen.Add(key)) continue;

                if (LoadoutCatalog.IsAryxKey(key))
                {
                    if (!plugin.EnableAryx.Value) continue;
                    if (!Guard.AryxLoaded())
                    {
                        plugin.LogOnce("Aryx Weaponry Expansion is not loaded; skipping '" + key + "'.");
                        continue;
                    }
                }
                else if (LoadoutCatalog.IsMeridianKey(key))
                {
                    if (!plugin.EnableMeridian.Value) continue;
                }

                WeaponMount mount;
                if (Encyclopedia.WeaponLookup == null || !Encyclopedia.WeaponLookup.TryGetValue(key, out mount) || mount == null)
                {
                    plugin.LogOnce("Configured mount '" + key + "' was not found in the Encyclopedia; skipping.");
                    continue;
                }

                if (enforce && !FitsBay(mount, bayLength, key, plugin)) continue;
                mounts.Add(mount);
            }
            return mounts;
        }

        private static bool FitsBay(WeaponMount mount, float bayLength, string key, Fs45MeridianPlugin plugin)
        {
            if (bayLength <= 0f || mount.info == null || mount.info.weaponPrefab == null) return true;

            var missile = mount.info.weaponPrefab.GetComponent<Missile>();
            var definition = missile != null ? missile.definition : null;
            if (definition == null || definition.length <= 0f) return true;

            if (definition.length <= bayLength) return true;

            plugin.LogOnce("Skipping '" + key + "': round length " + definition.length.ToString("0.00") +
                " m exceeds the bay's " + bayLength.ToString("0.00") + " m.");
            return false;
        }

        private static bool IsIgnored(string[] ignored, string normalized)
        {
            for (var i = 0; i < ignored.Length; i++)
            {
                if (ignored[i].Length > 0 && ignored[i] == normalized) return true;
            }
            return false;
        }

        private static bool ContainsKey(List<WeaponMount> options, string key)
        {
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option != null && string.Equals(option.jsonKey, key, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        internal static AircraftDefinition FindSpecter(Encyclopedia enc)
        {
            if (enc == null) return null;

            if (Encyclopedia.Lookup != null)
            {
                for (var i = 0; i < LookupKeys.Length; i++)
                {
                    if (Encyclopedia.Lookup.TryGetValue(LookupKeys[i], out var definition) &&
                        definition is AircraftDefinition aircraft)
                    {
                        return aircraft;
                    }
                }
            }

            if (enc.aircraft == null) return null;
            for (var i = 0; i < enc.aircraft.Count; i++)
            {
                var aircraft = enc.aircraft[i];
                if (aircraft == null) continue;
                if (Contains(aircraft.jsonKey, "f117") ||
                    Contains(aircraft.jsonKey, "fs45") ||
                    Contains(aircraft.unitName, "specter") ||
                    Contains(aircraft.unitName, "f117") ||
                    Contains(aircraft.unitName, "f-117") ||
                    Contains(aircraft.unitName, "fs-45") ||
                    Contains(aircraft.unitName, "fs45"))
                {
                    return aircraft;
                }
            }
            return null;
        }

        private static bool Contains(string value, string token)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
