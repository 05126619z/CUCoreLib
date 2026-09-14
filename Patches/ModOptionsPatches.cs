using System.Collections.Generic;
using CUCoreLib.Data;
using CUCoreLib.Helpers;
using CUCoreLib.Registries;
using HarmonyLib;
using UnityEngine;

namespace CUCoreLib.Patches
{
    [HarmonyPatch(typeof(Locale), nameof(Locale.GetOther), typeof(string))]
    internal static class LocalePatch
    {
        [HarmonyPrefix]
        private static bool HateGameset(string __0, ref string __result)
        {
            if (!ModOptionsRegistry.TryGetLocalizedText(__0, out var localizedText))
                return true;

            __result = localizedText;
            return false;
        }
    }

    [HarmonyPatch(typeof(Settings), nameof(Settings.DefaultSettings))]
    internal static class ModOptionsPatches
    {
        [HarmonyPostfix]
        private static void AppendRegisteredOptions(List<Setting> __result)
        {
            ModOptionsRegistry.AppendRegisteredOptions(__result);
            ModSettingsConfigSyncRegistry.RefreshLoadedConfigEntries();
        }
    }

    [HarmonyPatch(typeof(SettingsMenu), "Start")]
    internal static class SettingsMenuStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SettingsMenu __instance)
        {
            ModSettingsConfigSyncRegistry.RefreshLoadedConfigEntries();
            SettingsMenuCategoryExtender.EnsureAttached(__instance);
            var helper = __instance.GetComponent<SettingsMenuCategoryExtender>();
            helper?.OnTabSelected(Setting.SettingCategory.Video);
        }
    }

    [HarmonyPatch(typeof(SettingsMenu), nameof(SettingsMenu.SelectTab), typeof(Setting.SettingCategory))]
    internal static class SettingsMenuSelectTabPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SettingsMenu __instance, Setting.SettingCategory category,
            List<GameObject> ___spawnedSettings)
        {
            SettingsMenuCategoryExtender.EnsureAttached(__instance);
            var helper = __instance.GetComponent<SettingsMenuCategoryExtender>();
            helper?.OnTabSelected(category);

            if (__instance && __instance.content && helper)
                helper.FixDropdownsInContent(__instance.content);

            RestoreRegisteredKeybindTooltips(___spawnedSettings, category);
        }

        private static void RestoreRegisteredKeybindTooltips(List<GameObject> spawnedSettings, Setting.SettingCategory category)
        {
            if (spawnedSettings == null) return;

            // Content still contains the previous tab's rows until Unity finishes deferred destruction.
            var displayedSettingIndex = 0;
            foreach (var setting in Settings.GetAllSettings())
            {
                if (setting == null || setting.category != category) continue;
                if (displayedSettingIndex >= spawnedSettings.Count) return;

                if (setting is SettingKeybind)
                {
                    var option = ModOptionsRegistry.RegisteredOptions.Find(candidate =>
                        candidate != null && candidate.Id == setting.name && candidate.Kind == ModOptionKind.Keybind);
                    if (option != null && !string.IsNullOrWhiteSpace(option.Description))
                    {
                        var row = spawnedSettings[displayedSettingIndex];
                        if (!row || row.transform.childCount == 0)
                        {
                            displayedSettingIndex++;
                            continue;
                        }
                        var tooltipTarget = row.transform.GetChild(0);
                        var tooltip = tooltipTarget.GetComponent<UITooltip>();
                        if (tooltip != null)
                            tooltip.tipDesc = Locale.GetOther("gameset" + setting.name + "dsc");
                    }
                }

                displayedSettingIndex++;
            }
        }
    }

    [HarmonyPatch(typeof(KeyBinds), nameof(KeyBinds.GetBindName), typeof(string))]
    internal static class KeyBindsGetBindNamePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string action, ref string __result)
        {
            if (!CUCoreUtils.TryGetFriendlyBindDisplayName(action, out __result))
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(KeyBinds), nameof(KeyBinds.GetBind), typeof(string))]
    internal static class KeyBindsGetBindPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string action, ref KeyCode __result)
        {
            if (!CUCoreUtils.TryGetFriendlyBindKeyCode(action, out __result))
                return true;

            return false;
        }
    }
}
