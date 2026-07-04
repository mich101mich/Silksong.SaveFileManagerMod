using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SaveFileManagerMod.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod;

public class SfmLogger
{
    public static ManualLogSource? _logger;

    public static void LogInfo(string message)
    {
        if (_logger == null)
        {
            return;
        }
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string fullMessage = $"[{timestamp}] {message}";
        _logger?.LogInfo(fullMessage);
    }
};

[BepInAutoPlugin(id: "io.github.mich101mich.savefilemanagermod")]
[BepInDependency(Silksong.ModMenu.ModMenuPlugin.Id)]
public partial class SaveFileManagerPlugin : BaseUnityPlugin
{
    public static SaveFileManagerPlugin s_instance = null!;
    public Harmony m_harmony = null!;

    public List<SaveOptions> m_saveOptions = new();
    public ArchiveSlotSelectionMenu m_archiveMenu = null!;

    public void Awake()
    {
        s_instance = this;
        SfmLogger._logger = base.Logger;
        SfmLogger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");

        m_archiveMenu = gameObject.AddComponent<ArchiveSlotSelectionMenu>();

        m_harmony = new Harmony($"harmony-{Id}");
        m_harmony.PatchAll(typeof(SaveFileManagerPlugin));
    }

    public void OnDestroy()
    {
        SfmLogger.LogInfo($"Plugin {Name} ({Id}) is unloading...");
        m_harmony.UnpatchSelf();

        foreach (var saveOption in m_saveOptions)
        {
            UnityEngine.Object.Destroy(saveOption);
        }
        m_saveOptions.Clear();

        s_instance = null!;
        SfmLogger._logger = null;
    }

    // ================================================================================
    // Delete/Reinit handling
    // ================================================================================

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIManager), nameof(UIManager.UIGoToProfileMenu))]
    public static void UIManager_UIGoToProfileMenu_Prefix(UIManager __instance)
    {
        s_instance.initUi();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIManager), nameof(UIManager.MakeMenuLean))]
    public static void UIManager_MakeMenuLean_Prefix(UIManager __instance)
    {
        // Called when the player loads into a file. The menu objects are destroyed here to improve performance, so we need to clear our references to them.
        s_instance.m_saveOptions.Clear();
    }

    public void initUi()
    {
        if (m_saveOptions.Count > 0)
        {
            return;
        }

        SfmLogger.LogInfo("Initializing SaveOptions for all save slots...");

        var ui = UIManager.instance;
        var saveSlotButtons = new List<SaveSlotButton>()
        {
            ui.slotOne,
            ui.slotTwo,
            ui.slotThree,
            ui.slotFour,
        };
        foreach (var button in saveSlotButtons)
        {
            var options = button.gameObject.AddComponent<SaveOptions>();
            options.Initialize(button);
            m_saveOptions.Add(options);
        }

        for (int i = 0; i < m_saveOptions.Count; i++)
        {
            var prev = i - 1 >= 0 ? m_saveOptions[i - 1] : null;
            var next = i + 1 < m_saveOptions.Count ? m_saveOptions[i + 1] : null;
            m_saveOptions[i].SetupNavigation(prev, next);
        }
    }

    // ================================================================================
    // Load Name Handler
    // ================================================================================

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.Prepare))]
    public static void SaveSlotButton_Prepare_Postfix(SaveSlotButton __instance)
    {
        // Function called on startup and when changing pages with the MoreSaves mod
        SaveName.ReloadSlot(__instance.SaveSlotIndex);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.UpdateSaveFileState))]
    public static void SaveSlotButton_UpdateSaveFileState_Postfix(SaveSlotButton __instance)
    {
        // Function called when the select profile menu is opened
        SaveName.ReloadSlot(__instance.SaveSlotIndex);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveSlotButton), "ChangeSaveFileState")]
    public static void SaveSlotButton_ChangeSaveFileState_Prefix(SaveSlotButton __instance, SaveSlotButton.SaveFileStates nextSaveFileState)
    {
        if (nextSaveFileState == SaveSlotButton.SaveFileStates.Empty)
        {
            // Save was erased, or file is empty/nonexistent. Clear the name for this slot.
            SaveName.ClearSlot(__instance.SaveSlotIndex);
        }
    }

    // ================================================================================
    // Button Navigation Fixes
    // ================================================================================

    // Patch: Team Cherry added logic to the ClearSaveButton to automatically move on to the next button when navigating left/right, skipping over disabled buttons.
    // However, they did not add this logic to the RestoreSaveButton, so this mod's custom button setup breaks.

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    public static void RestoreSaveButton_OnMove_Prefix(Selectable __instance, AxisEventData eventData, ref bool __runOriginal)
    {
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
