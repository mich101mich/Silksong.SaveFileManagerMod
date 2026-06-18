using System.Collections;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using Silksong.ModMenu.Models;
using UnityEngine.Events;
using UnityEngine.UI;
using GlobalEnums;

using SaveFileManagerMod.UI;

namespace SaveFileManagerMod;

[BepInAutoPlugin(id: "io.github.mich101mich.savefilemanagermod")]
[BepInDependency(Silksong.ModMenu.ModMenuPlugin.Id)]
public partial class SaveFileManagerPlugin : BaseUnityPlugin
{
    public static SaveFileManagerPlugin s_instance { get; private set; } = null!;
    internal static ManualLogSource s_logger { get; private set; } = null!;
    private Harmony m_harmony = null!;

    private Dictionary<SaveSlotButton, SaveOptions> m_saveSlotOptions = new();

    private void Awake()
    {
        s_instance = this;
        s_logger = base.Logger;
        s_logger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");


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
    }
}
