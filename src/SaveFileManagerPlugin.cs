using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SaveFileManagerMod.UI;
using System.Collections.Generic;

namespace SaveFileManagerMod;

public class SfmLogger
{
    public static ManualLogSource? _logger;

    public static void Log(LogLevel level, string message)
    {
        if (_logger == null)
        {
            return;
        }
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        string fullMessage = $"[{timestamp}] {message}";
        _logger?.Log(level, fullMessage);
    }

    public static void LogInfo(string message)
    {
        Log(LogLevel.Info, message);
    }
    public static void LogWarning(string message)
    {
        Log(LogLevel.Warning, message);
    }
    public static void LogError(string message)
    {
        Log(LogLevel.Error, message);
    }
};

[BepInAutoPlugin(id: "io.github.mich101mich.savefilemanagermod")]
[BepInDependency("org.silksong-modding.i18n", BepInDependency.DependencyFlags.SoftDependency)]
public partial class SaveFileManagerPlugin : BaseUnityPlugin
{
    public static SaveFileManagerPlugin s_instance = null!;
    public Harmony m_harmony = null!;

    public bool? m_isModCompatible = null;
    public List<SaveOptions> m_saveOptions = new List<SaveOptions>();
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

        SetBackButtonNavigation(); // reset the back button's navigation to the default

        s_instance = null!;
        SfmLogger._logger = null;
    }

    public bool IsModCompatible()
    {
        if (m_isModCompatible == null)
        {
            if (Platform.Current is DesktopPlatform platform)
            {
                if (platform.onlineSubsystem != null && platform.onlineSubsystem.HandlesGameSaves)
                {
                    SfmLogger.LogError($"SaveFileManagerMod: This mod is not compatible with the online subsystem {platform.onlineSubsystem}.");
                    m_isModCompatible = false;
                }
                m_isModCompatible = true;
            }
            else
            {
                SfmLogger.LogError($"SaveFileManagerMod: Unsupported platform {Platform.Current}. This mod only works on desktop platforms.");
                m_isModCompatible = false;
            }
        }
        return m_isModCompatible.Value;
    }

    public void InitUi()
    {
        if (m_saveOptions.Count > 0)
        {
            return; // Already initialized
        }

        if (!IsModCompatible())
        {
            return;
        }

        SfmLogger.LogInfo("Initializing SaveOptions for all save slots...");

        var ui = UIManager.instance;
        var saveSlotButtons = new[] { ui.slotOne, ui.slotTwo, ui.slotThree, ui.slotFour };
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

    public void ClearUiReferences()
    {
        // The objects in the UI are being destroyed, so we need to clear our references to them.
        // Note that we don't destroy any of the objects that we created. That is handled automatically by Unity
        // since we created them as components of the UI elements.
        m_saveOptions.Clear();
    }
}
