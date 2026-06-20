using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SaveFileManagerMod.UI;
using System.Collections;
using UnityEngine;

namespace SaveFileManagerMod;

internal class SfmLogger
{
    internal static ManualLogSource? _logger;

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
    public sealed class MockArchiveEntry
    {
        public MockArchiveEntry(string id, string label, string details)
        {
            Id = id;
            Label = label;
            Details = details;
        }

        public string Id { get; }
        public string Label { get; }
        public string Details { get; }
    }

    public static SaveFileManagerPlugin s_instance { get; private set; } = null!;
    private Harmony m_harmony = null!;

    private List<SaveOptions> m_saveOptions = new();
    private Dictionary<SaveSlotButton, SaveOptions> m_saveSlotOptionMap = new();
    internal ArchiveMenuController m_archiveMenu { get; private set; } = null!;

    private readonly Dictionary<int, string> m_customSlotNames = new();
    private readonly List<MockArchiveEntry> m_mockArchiveEntries = new()
    {
        new MockArchiveEntry("archive-01", "Archive A", "Moss Grotto - 03:21"),
        new MockArchiveEntry("archive-02", "Archive B", "Citadel - 12:44"),
        new MockArchiveEntry("archive-03", "Archive C", "Greymoor - 25:08")
    };

    internal IReadOnlyList<MockArchiveEntry> MockArchiveEntries => m_mockArchiveEntries;

    internal bool TryGetCustomName(int slotIndex, out string customName) =>
        m_customSlotNames.TryGetValue(slotIndex, out customName!);

    internal void SetCustomName(int slotIndex, string customName)
    {
        string normalized = customName.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            m_customSlotNames.Remove(slotIndex);
            return;
        }

        m_customSlotNames[slotIndex] = normalized;
    }

    private void Awake()
    {
        s_instance = this;
        SfmLogger._logger = base.Logger;
        SfmLogger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");

        m_archiveMenu = new ArchiveMenuController(this);

        m_harmony = new Harmony($"harmony-{Id}");
        m_harmony.PatchAll(typeof(SaveFileManagerPlugin));

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
            var options = new SaveOptions(button);
            m_saveOptions.Add(options);
            m_saveSlotOptionMap[button] = options;
        }

        for (int i = 0; i < m_saveOptions.Count; i++)
        {
            var prev = i - 1 >= 0 ? m_saveOptions[i - 1] : null;
            var next = i + 1 < m_saveOptions.Count ? m_saveOptions[i + 1] : null;
            m_saveOptions[i].SetupNavigation(prev, next);
        }
    }

    private void OnDestroy()
    {
        SfmLogger.LogInfo($"Plugin {Name} ({Id}) is unloading...");
        m_harmony.UnpatchSelf();

        foreach (var saveSlotOption in m_saveSlotOptionMap.Values)
        {
            saveSlotOption.Dispose();
        }
        m_saveSlotOptionMap.Clear();

        m_archiveMenu.Dispose();

        s_instance = null!;
        SfmLogger._logger = null;
    }

    // Patch: Team Cherry added logic to the ClearSaveButton to automatically move on to the next button when navigating left/right, skipping over disabled buttons.
    // However, they did not add this logic to the RestoreSaveButton, so this mod's custom button setup breaks.

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    private static void RestoreSaveButton_OnMove_Prefix(Selectable __instance, AxisEventData eventData, ref bool __runOriginal)
    {
        if (__instance is not RestoreSaveButton)
        {
            return;
        }

        bool moveRight;
        switch (eventData.moveDir)
        {
            case MoveDirection.Right:
                moveRight = true;
                break;
            case MoveDirection.Left:
                moveRight = false;
                break;
            default:
                return;
        }

        Selectable? target = TryNavigateSkippingDisabled(__instance, moveRight);
        if (target != null)
        {
            eventData.selectedObject = target.gameObject;
        }

        __runOriginal = false;
    }

    private static Selectable? TryNavigateSkippingDisabled(Selectable start, bool moveRight)
    {
        Selectable? current = start;

        while (true)
        {
            current = moveRight
                ? current.FindSelectableOnRight()
                : current.FindSelectableOnLeft();
            if (current == null)
            {
                return null;
            }

            if (current.IsActive() && current.IsInteractable())
            {
                return current;
            }
        }
    }
}
