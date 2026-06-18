using System.Collections.Generic;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.UI;

using SaveFileManagerMod.UI;

namespace SaveFileManagerMod;

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
    internal static ManualLogSource s_logger { get; private set; } = null!;
    private Harmony m_harmony = null!;

    private Dictionary<SaveSlotButton, SaveOptions> m_saveSlotOptions = new();
    internal ArchiveMenuController ArchiveMenu { get; private set; } = null!;

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
        s_logger = base.Logger;
        s_logger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");

        ArchiveMenu = new ArchiveMenuController(this);


        m_harmony = new Harmony($"harmony-{Id}");
        m_harmony.PatchAll(typeof(SaveFileManagerPlugin));
    }

    private void OnDestroy()
    {
        s_logger.LogInfo($"Plugin {Name} ({Id}) is unloading...");
        m_harmony.UnpatchSelf();

        foreach (var saveSlotOption in m_saveSlotOptions.Values)
        {
            saveSlotOption.Dispose();
        }
        m_saveSlotOptions.Clear();

        ArchiveMenu.Dispose();

        s_instance = null!;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveSlotButton), "ShowRelevantModeForSaveFileState")]
    public static void SaveSlotButton_ShowRelevantModeForSaveFileState_Prefix(SaveSlotButton __instance)
    {
        if (!s_instance.m_saveSlotOptions.ContainsKey(__instance))
        {
            s_instance.m_saveSlotOptions[__instance] = new SaveOptions(__instance);
        }

        s_instance.m_saveSlotOptions[__instance].SyncFromSlotState();
    }
}
