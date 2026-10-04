using HarmonyLib;

namespace SaveFileManagerMod;

// ================================================================================
// Init/Delete handling
// ================================================================================

public partial class SaveFileManagerPlugin
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIManager), nameof(UIManager.UIGoToProfileMenu))]
    public static void UIManager_UIGoToProfileMenu_Prefix(UIManager __instance)
    {
        // Called whenever the game transitions from the main menu to the profile menu.

        s_instance.InitUi();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIManager), nameof(UIManager.MakeMenuLean))]
    public static void UIManager_MakeMenuLean_Prefix(UIManager __instance)
    {
        // Called when the player loads into a file.

        // The menu objects are destroyed here to improve performance, so we need to clear our references to them.
        s_instance.ClearUiReferences();
    }
}