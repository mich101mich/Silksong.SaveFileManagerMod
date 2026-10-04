using HarmonyLib;
using SaveFileManagerMod.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod;

// ================================================================================
// Button Navigation Fixes
// ================================================================================

public partial class SaveFileManagerPlugin
{
    // Patch: Team Cherry added logic to the ClearSaveButton to automatically move on to the next button when navigating left/right, skipping over disabled buttons.
    // However, they did not add this logic to the RestoreSaveButton, so this mod's custom button setup breaks.

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    public static void RestoreSaveButton_OnMove_Prefix(Selectable __instance, AxisEventData eventData, ref bool __runOriginal)
    {
        // Called any time a directional key was pressed on any UI element

        if (__instance is not RestoreSaveButton)
        {
            return;
        }

        if (!__runOriginal)
        {
            return; // Another prefix has already handled this event
        }

        if (SaveSlotActionButton.TryNavigateSkippingDisabled(eventData, __instance))
        {
            // we have successfully navigated to a new button => skip original logic
            __runOriginal = false;
        }
        // else: continue with default behavior
    }
}
