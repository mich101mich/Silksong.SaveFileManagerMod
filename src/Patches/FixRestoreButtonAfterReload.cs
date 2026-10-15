using System.Collections;
using HarmonyLib;
using SaveFileManagerMod.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod;

// ================================================================================
// RestoreSaveButton Fixes
// ================================================================================

// There is a bug in SaveSlotButton.ResetButton where it calls restoreSaveButton.Hide() as a coroutine that keeps
// running, while also (if the doAnimate parameter is true and the slot is filled) starting another coroutine with
// restoreSaveButton.ShowRestoreSaveButton(). The show routine actually finishes before the hide routine, and since
// the hide routine sets blocksRaycasts to false at the end, the restore button becomes unclickable even though it
// is visible.
// Our fix: Detect when Hide() is called from within a reload operation and skip the hide animation.

public partial class SaveFileManagerPlugin
{
    static bool s_isReloading = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.ResetButton))]
    public static void SaveSlotButton_ResetButton_Prefix()
    {
        s_isReloading = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.ResetButton))]
    public static void SaveSlotButton_ResetButton_Postfix()
    {
        s_isReloading = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RestoreSaveButton), nameof(RestoreSaveButton.Hide))]
    public static bool RestoreSaveButton_Hide_Prefix(RestoreSaveButton __instance, bool __runOriginal, ref IEnumerator __result)
    {
        if (!s_isReloading)
        {
            return true;
        }
        __runOriginal = false;
        __result = EmptyRoutine();

        SfmLogger.LogInfo("Skipping Hide animation for RestoreSaveButton during reload.");

        // Imitate the original behavior of Hide(), but instantly

        // Contents of Hide()
        if (__instance.currentTransition != null)
        {
            UIManager.instance.StopCoroutine(__instance.currentTransition);
            __instance.currentTransition = null;
        }

        __instance.isLoading = false;
        __instance.isVisible = false;
        __instance.state = RestoreSaveButton.RestoreState.None;

        // Contents of ExitState()
        __instance.saveSlotButton.controlsGroupBlocker.ignoreParentGroups = false;
        RestoreSaveButton.activeRestoreSaveButton = null;

        __instance.SetHidden(); // fast version to hide every member
        return false;
    }

    private static IEnumerator EmptyRoutine()
    {
        yield break;
    }
}
