using HarmonyLib;
using UnityEngine.UI;

namespace SaveFileManagerMod;

// ================================================================================
// Load Name Handler
// ================================================================================

public partial class SaveFileManagerPlugin
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.Prepare))]
    public static void SaveSlotButton_Prepare_Postfix(SaveSlotButton __instance)
    {
        // Called on startup and when changing pages with the MoreSaves mod

        if (!s_instance.IsModCompatible())
        {
            return;
        }

        SaveName.ReloadSlot(__instance.SaveSlotIndex);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.UpdateSaveFileState))]
    public static void SaveSlotButton_UpdateSaveFileState_Postfix(SaveSlotButton __instance)
    {
        // Called when the select profile menu is opened

        if (!s_instance.IsModCompatible())
        {
            return;
        }

        SaveName.ReloadSlot(__instance.SaveSlotIndex);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.ChangeSaveFileState))]
    public static void SaveSlotButton_ChangeSaveFileState_Prefix(SaveSlotButton __instance, SaveSlotButton.SaveFileStates nextSaveFileState)
    {
        // Called to change the state of the save file slot

        if (!s_instance.IsModCompatible())
        {
            return;
        }

        if (nextSaveFileState == SaveSlotButton.SaveFileStates.Empty)
        {
            // Save was erased, or file is empty/nonexistent. Clear the name for this slot.
            SaveName.ClearSlot(__instance.SaveSlotIndex);
        }
    }
}