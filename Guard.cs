using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;

namespace Fs45Meridian
{
    /// <summary>
    /// Load guard: the plugin stays inert unless both Meridian Works and the
    /// FS-45 Specter bundle are installed.
    /// </summary>
    internal static class Guard
    {
        private static readonly string[] MeridianFileTokens = { "meridian" };
        private static readonly string[] SpecterFileTokens = { "specter", "f117", "fs45", "fs-45" };

        internal static bool MeridianOnDisk()
        {
            return PluginsContainFile(MeridianFileTokens);
        }

        internal static bool SpecterOnDisk()
        {
            return PluginsContainFile(SpecterFileTokens);
        }

        /// <summary>True once the Aryx Weaponry Expansion has been loaded by BepInEx.</summary>
        internal static bool AryxLoaded()
        {
            return PluginLoaded(Fs45MeridianPlugin.AryxGuid, "AryxWeaponryExpansion.Plugin");
        }

        /// <summary>True once Meridian Works has actually been loaded by BepInEx.</summary>
        internal static bool MeridianLoaded()
        {
            return PluginLoaded(Fs45MeridianPlugin.MeridianGuid, "MeridianWorks.Plugin", "MeridianWorks.PluginInfo");
        }

        private static bool PluginLoaded(string guid, params string[] typeNames)
        {
            try
            {
                if (Chainloader.PluginInfos != null &&
                    Chainloader.PluginInfos.ContainsKey(guid))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // PluginInfos is not available this early; fall through to the assembly scan.
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                for (var i = 0; i < typeNames.Length; i++)
                {
                    try
                    {
                        if (assembly.GetType(typeNames[i], false) != null) return true;
                    }
                    catch (Exception)
                    {
                        // Reflection-only and dynamic assemblies can refuse type lookups.
                    }
                }
            }
            return false;
        }

        private static bool PluginsContainFile(string[] tokens)
        {
            try
            {
                var root = Paths.PluginPath;
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return false;

                foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(path);
                    for (var i = 0; i < tokens.Length; i++)
                    {
                        if (name.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    }
                }
            }
            catch (Exception)
            {
                // A missing or unreadable plugins folder counts as "not present".
            }
            return false;
        }
    }
}
