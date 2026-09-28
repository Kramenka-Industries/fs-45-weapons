using System;
using System.Collections.Generic;
using System.Text;

namespace Fs45Meridian
{
    /// <summary>Default hand-curated loadouts for the FS-45 Specter's internal bays.</summary>
    internal static class LoadoutCatalog
    {
        internal const string DefaultInternalBay = "Internal Weapon Bays";
        internal const string DefaultIrmBay = "IRM Bays";
        internal const string DefaultIgnored = "Tail Hook";

        // One round per slot unless a rack is named explicitly.
        internal const string DefaultInternalWeapons =
            "MeridianBlackArrow_internal," +
            "MeridianBlackArrow_internalx2," +
            "MeridianAGM84_internal," +
            "MeridianGBP500_internal," +
            "MeridianScreamer_internal," +
            "MeridianARAD200_internal," +
            "MeridianSRM8_internal," +
            "bomb_cluster1_single_internal," +
            "Aryx_FastAGM1_Internal_x2," +
            "Aryx_Wep_ARM2_single_internal," +
            "Aryx_GlideBomb400_Single_Internal";

        internal const string DefaultIrmWeapons =
            "MeridianSRM8_internal," +
            "Aryx_SRIRM1_Single_Internal";

        /// <summary>Strip spaces and punctuation and lower-case a hardpoint set name.</summary>
        internal static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            }
            return builder.ToString();
        }

        /// <summary>Split a comma-separated config value into trimmed, non-empty entries.</summary>
        internal static string[] Split(string value)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(value)) return list.ToArray();

            foreach (var part in value.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0) list.Add(trimmed);
            }
            return list.ToArray();
        }

        internal static bool IsAryxKey(string key)
        {
            return !string.IsNullOrEmpty(key) && key.StartsWith("Aryx", StringComparison.Ordinal);
        }

        internal static bool IsMeridianKey(string key)
        {
            return !string.IsNullOrEmpty(key) && key.StartsWith("Meridian", StringComparison.Ordinal);
        }
    }
}
