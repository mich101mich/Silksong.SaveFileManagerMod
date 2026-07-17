using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public partial class ArchiveSlotSelectionMenu : MonoBehaviour
{
    public ScrollingMenuScreen? m_screen;

    public int m_selectedSlotIndex = -1;
    public bool m_selectedSlotIsEmpty = false;

    public bool IsOpen = false;

    public Coroutine? m_openRoutine = null;

    public List<ArchiveMenuEntry?> m_rawEntries = new List<ArchiveMenuEntry?>();
    public int m_numFilledEntries = 0;

    public SaveSlotButton m_templateSlotButton = null!;

    public void OnDestroy()
    {
        if (m_screen != null)
        {
            m_screen.Dispose();
            m_screen = null;
        }

        IsOpen = false;
    }

    public void OpenForSlot(SaveSlotButton slot)
    {
        if (IsOpen)
        {
            return;
        }

        m_selectedSlotIndex = slot.SaveSlotIndex;
        m_selectedSlotIsEmpty = slot.saveFileState == SaveSlotButton.SaveFileStates.Empty;
        m_templateSlotButton = slot;

        m_openRoutine = StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (m_openRoutine != null)
        {
            StopCoroutine(m_openRoutine);
            m_openRoutine = null;
        }
        StartCoroutine(CloseRoutine());
    }

    public void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        InputHandler inputHandler = GameManager.instance.inputHandler;
        if (inputHandler.acceptingInput && inputHandler.inputActions.MenuCancel.WasPressed)
        {
            inputHandler.inputActions.MenuCancel.ClearInputState();
            UIManager.instance.uiAudioPlayer.PlayCancel();
            Close();
        }
    }

    public IEnumerator OpenRoutine()
    {
        IsOpen = true;

        // Prevent input (mostly cancelling) until the menu is open.
        // Input will be resumed by ui.ShowMenu();
        GameManager.instance.inputHandler.StopUIInput();

        m_screen?.Dispose();

        var title = m_selectedSlotIsEmpty
            ? Strings.TitleLoadIntoEmpty // "Load from archive"
            : Strings.TitleArchiveReplace; // "Archive / Replace"

        m_screen = new ScrollingMenuScreen(title);
        m_screen.Container.name = "SFM-ArchiveSlotSelectionMenu";
        m_screen.AllowGoBack = false;
        m_screen.OnGoBack += Close;
        m_screen.Content.VerticalSpacing = ArchiveMenuEntry.SLOT_TOTAL_HEIGHT;

        var slotInfo = SaveName.TryGetSlotName(m_selectedSlotIndex, out string slotName)
            ? Strings.NamedSlotInfo(name: slotName, index: m_selectedSlotIndex) // $"Target: Slot {index}. \"{name}\""
            : Strings.UnnamedSlotInfo(index: m_selectedSlotIndex); // $"Target: Slot {index}."

        var statusLabel = new TextLabel("SFM-StatusLabel");

        // By default, text has a component that changes the line spacing to -0.33f, which is bad for multiline strings.
        SfmUtil.RemoveComponent<FixVerticalAlign>(statusLabel.Text.gameObject);

        statusLabel.Text.lineSpacing = 1f;
        statusLabel.Text.text = $"{slotInfo}\n{Strings.LoadingText}"; // "Loading save slots..."
        m_screen.Add(statusLabel);

        m_rawEntries.Clear();
        m_rawEntries.Add(null); // index 0 is unused

        // Add the first entries immediately so that there is something to select
        for (int i = 1; i <= 4; i++)
        {
            AddEntry(i);
        }
        yield return new WaitUntil(() => m_rawEntries.Count == 5);
        for (int i = 1; i <= 4; i++)
        {
            if (m_rawEntries[i] is ArchiveMenuEntry entry)
            {
                m_screen.Add(entry);
            }
        }

        UIManager ui = UIManager.instance;
        if (ui.menuState == GlobalEnums.MainMenuState.SAVE_PROFILES)
        {
            // ui.uiAudioPlayer.PlayOpenProfileSelect();
            yield return StartCoroutine(ui.HideSaveProfileMenu(updateBlackThread: true));
        }

        InvokeScreenOnShow(m_screen);

        yield return StartCoroutine(ui.ShowMenu(m_screen.MenuScreen));

        for (int i = 5; i <= 50; i++)
        {
            AddEntry(i);
            yield return new WaitUntil(() => m_rawEntries.Count > i);
            if (m_rawEntries[i] is ArchiveMenuEntry entry)
            {
                m_screen.Add(entry);
            }

            yield return new WaitForSeconds(0.01f); // slight delay to avoid freezing the game
        }

        if (m_numFilledEntries == 0)
        {
            statusLabel.Text.text = $"{slotInfo}\n{Strings.NoLoadableSavesFound}"; // "No loadable saves found"
        }
        else
        {
            statusLabel.Text.text = $"{slotInfo}";
        }
    }

    public void AddEntry(int slotIndex)
    {
        if (slotIndex == m_selectedSlotIndex)
        {
            m_rawEntries.Add(null);
            return;
        }

        GameManager.instance.GetSaveStatsForSlot(slotIndex,
            (stats, message) => OnSaveStatsReceived(slotIndex, stats, message));
    }

    public void OnSaveStatsReceived(int slotIndex, SaveStats? stats, string? message)
    {
        bool isEmpty = stats == null && message == null;
        if (isEmpty && m_selectedSlotIsEmpty)
        {
            // Can't load from an empty slot => don't add it to the list
            m_rawEntries.Add(null);
            return;
        }

        var entry = new ArchiveMenuEntry(slotIndex, stats, message, m_templateSlotButton)
        {
            OnSubmit = () =>
            {
                if (m_selectedSlotIsEmpty)
                {
                    SaveArchive.LoadSlot(m_selectedSlotIndex, slotIndex);
                }
                else if (isEmpty)
                {
                    SaveArchive.ArchiveSlot(m_selectedSlotIndex, slotIndex);
                }
                else
                {
                    SaveArchive.SwapSlots(m_selectedSlotIndex, slotIndex);
                }
                Close();
                UIManager.instance.ReloadSaves();
            }
        };

        // Don't add the entry immediately, because that causes visual glitches.
        // Only modify UI from the routine.
        m_rawEntries.Add(entry);
        m_numFilledEntries++;
    }

    public IEnumerator CloseRoutine()
    {
        // Prevent input (mostly double cancel) until the menu is closed.
        // Input will be resumed by ui.GoToProfileMenu();
        GameManager.instance.inputHandler.StopUIInput();

        UIManager ui = UIManager.instance;

        if (m_screen != null)
        {
            yield return StartCoroutine(ui.HideMenu(m_screen.MenuScreen));

            InvokeScreenOnHide(m_screen);

            m_screen.Dispose();
            m_screen = null;
        }
        IsOpen = false;

        yield return StartCoroutine(ui.GoToProfileMenu());
    }

    public static readonly MethodInfo s_invokeOnShow = typeof(AbstractMenuScreen)
        .GetMethod("InvokeOnShow", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static readonly MethodInfo s_invokeOnHide = typeof(AbstractMenuScreen)
        .GetMethod("InvokeOnHide", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static void InvokeScreenOnShow(AbstractMenuScreen screen)
    {
        s_invokeOnShow.Invoke(screen, new object[] { MenuScreenNavigation.NavigationType.Forwards });
    }

    public static void InvokeScreenOnHide(AbstractMenuScreen screen)
    {
        s_invokeOnHide.Invoke(screen, new object[] { MenuScreenNavigation.NavigationType.Backwards });
    }
}
