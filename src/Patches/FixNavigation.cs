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
    public static Selectable? SaveScreenBackButton => UIManager.instance.slotOne?.backButton;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    public static void RestoreSaveButton_OnMove_Prefix(Selectable __instance, AxisEventData eventData, ref bool __runOriginal)
    {
        // Called any time a directional key was pressed on any UI element

        if (!__runOriginal)
        {
            return; // Another prefix has already handled this event
        }

        // Patch: Team Cherry added logic to the ClearSaveButton to automatically move on to the next button when navigating left/right, skipping over disabled buttons.
        // However, they did not add this logic to the RestoreSaveButton, so this mod's custom button setup breaks.
        if (__instance is RestoreSaveButton && SaveSlotActionButton.TryNavigateSkippingDisabled(eventData, __instance))
        {
            // we have successfully navigated to a new button => skip original logic
            __runOriginal = false;
        }

        // Patch: There are now up to 16 buttons in a row, and pressing up from the back button always jumps to the first button all the way on the left.
        // This is somewhat inconvenient when you just move around the menu.
        // So, we keep track of the last button and navigate to it when pressing up from the back button.
        if (eventData.moveDir == MoveDirection.Down && (__instance is RestoreSaveButton || __instance is SaveSlotActionButton || __instance is ClearSaveButton))
        {
            // store that we came from this button
            SetBackButtonNavigation(__instance);
        }
        else if (__instance == SaveScreenBackButton)
        {
            if (eventData.moveDir == MoveDirection.Up)
            {
                var target = __instance.navigation.selectOnUp;
                // check if the stored button is accessible or if some external factor requires us to reset it
                if (target == null || !target.interactable)
                {
                    SetBackButtonNavigation();
                }
            }
        }
        else
        {
            // reset the back button's navigation when moving away from it
            SetBackButtonNavigation();
        }
    }

    public static void SetBackButtonNavigation(Selectable? selectOnUp = null)
    {
        if (SaveScreenBackButton is Selectable backButton)
        {
            var nav = backButton.navigation;
            nav.selectOnUp = selectOnUp ?? UIManager.instance.slotOne.restoreSaveButton;
            backButton.navigation = nav;
        }
    }
}
