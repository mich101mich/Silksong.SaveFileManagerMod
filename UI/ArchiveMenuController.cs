using GlobalEnums;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Screens;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public sealed class ArchiveMenuController : IDisposable
{
    private readonly SaveFileManagerPlugin m_plugin;
    private readonly ArchiveMenuUpdater m_updater;

    private ScrollingMenuScreen? m_screen;
    private TextLabel? m_statusLabel;

    private static readonly MethodInfo? s_invokeOnShow = typeof(AbstractMenuScreen)
        .GetMethod("InvokeOnShow", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo? s_invokeOnHide = typeof(AbstractMenuScreen)
        .GetMethod("InvokeOnHide", BindingFlags.Instance | BindingFlags.NonPublic);

    private bool m_isOpen;
    private bool m_isTransitioning;

    public bool IsOpen => m_isOpen || m_isTransitioning;

    public ArchiveMenuController(SaveFileManagerPlugin plugin)
    {
        m_plugin = plugin;
        m_updater = plugin.gameObject.AddComponent<ArchiveMenuUpdater>();
        m_updater.Initialize(this);
    }

    public void Dispose()
    {
        if (m_screen != null)
        {
            m_screen.Dispose();
            m_screen = null;
            m_statusLabel = null;
        }

        m_isOpen = false;
        m_isTransitioning = false;

        if (m_updater != null)
        {
            m_updater.ClearOwner();
            UnityEngine.Object.Destroy(m_updater);
        }
    }

    public void OpenForSlot(SaveSlotButton slot)
    {
        if (IsOpen)
        {
            return;
        }

        BuildScreen(slot);
        if (m_screen == null)
        {
            return;
        }

        m_plugin.StartCoroutine(OpenRoutine());
    }

    private void Close()
    {
        if (!m_isOpen || m_isTransitioning)
        {
            return;
        }

        m_plugin.StartCoroutine(CloseRoutine());
    }

    internal void Tick()
    {
        if (!m_isOpen || m_isTransitioning)
        {
            return;
        }

        InputHandler inputHandler = GameManager.instance.inputHandler;
        if (!inputHandler.acceptingInput || !inputHandler.inputActions.MenuCancel.WasPressed)
        {
            return;
        }

        inputHandler.inputActions.MenuCancel.ClearInputState();
        Close();
    }

    private void BuildScreen(SaveSlotButton slot)
    {
        m_screen?.Dispose();

        ScrollingMenuScreen screen = new("Save Archive");
        screen.AllowGoBack = false;
        screen.OnGoBack += Close;

        string slotSummary = slot.saveFileState == SaveSlotButton.SaveFileStates.Empty
            ? $"Empty slot {slot.SaveSlotIndex}"
            : $"Slot {slot.SaveSlotIndex}";

        screen.Add(new TextLabel(slotSummary));

        TextButton slotAction = new(
            slot.saveFileState == SaveSlotButton.SaveFileStates.Empty
                ? "Import Selected Archive"
                : "Archive Current Save",
            "Frontend-only placeholder"
        )
        {
            OnSubmit = () =>
            {
                SetStatus(slot.saveFileState == SaveSlotButton.SaveFileStates.Empty
                    ? "Placeholder: would import selected archive entry into this slot."
                    : "Placeholder: would move this save into archive storage.");
            }
        };
        screen.Add(slotAction);

        foreach (SaveFileManagerPlugin.MockArchiveEntry entry in m_plugin.MockArchiveEntries)
        {
            SaveFileManagerPlugin.MockArchiveEntry item = entry;
            screen.Add(new TextButton(item.Label, item.Details)
            {
                OnSubmit = () =>
                {
                    SetStatus(slot.saveFileState == SaveSlotButton.SaveFileStates.Empty
                        ? $"Placeholder: would import '{item.Label}' into this slot."
                        : $"Placeholder: would replace this slot with '{item.Label}'.");
                }
            });
        }

        m_statusLabel = new TextLabel("Select an archive action.");
        screen.Add(m_statusLabel);

        m_screen = screen;
    }

    private void SetStatus(string message)
    {
        if (m_statusLabel != null)
        {
            m_statusLabel.Text.text = message;
        }
    }

    private IEnumerator OpenRoutine()
    {
        if (m_screen == null)
        {
            yield break;
        }

        m_isTransitioning = true;

        UIManager ui = UIManager.instance;
        if (ui.menuState == MainMenuState.SAVE_PROFILES)
        {
            yield return ui.StartCoroutine(ui.HideSaveProfileMenu(updateBlackThread: true));
        }

        InvokeScreenOnShow(m_screen);

        yield return ui.StartCoroutine(ui.ShowMenu(m_screen.MenuScreen));

        m_isOpen = true;
        m_isTransitioning = false;
    }

    private IEnumerator CloseRoutine()
    {
        if (m_screen == null)
        {
            yield break;
        }

        m_isTransitioning = true;

        UIManager ui = UIManager.instance;
        yield return ui.StartCoroutine(ui.HideMenu(m_screen.MenuScreen));

        InvokeScreenOnHide(m_screen);

        m_screen.Dispose();
        m_screen = null;
        m_statusLabel = null;
        m_isOpen = false;

        yield return ui.StartCoroutine(ui.GoToProfileMenu());

        m_isTransitioning = false;
    }

    private static void InvokeScreenOnShow(AbstractMenuScreen screen)
    {
        if (s_invokeOnShow == null)
        {
            return;
        }

        s_invokeOnShow.Invoke(screen, new object[] { MenuScreenNavigation.NavigationType.Forwards });
    }

    private static void InvokeScreenOnHide(AbstractMenuScreen screen)
    {
        if (s_invokeOnHide == null)
        {
            return;
        }

        s_invokeOnHide.Invoke(screen, new object[] { MenuScreenNavigation.NavigationType.Backwards });
    }

    private sealed class ArchiveMenuUpdater : UnityEngine.MonoBehaviour
    {
        private ArchiveMenuController? m_owner;

        public void Initialize(ArchiveMenuController owner)
        {
            m_owner = owner;
        }

        public void ClearOwner()
        {
            m_owner = null;
        }

        private void Update()
        {
            m_owner?.Tick();
        }
    }
}
