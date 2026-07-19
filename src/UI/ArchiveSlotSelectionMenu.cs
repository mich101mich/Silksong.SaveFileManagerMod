using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public partial class ArchiveSlotSelectionMenu : MonoBehaviour
{
    public ArchiveMenuScreen? m_screen;

    public int m_selectedSlotIndex = -1;
    public bool m_selectedSlotIsEmpty = false;

    public bool IsOpen = false;

    public Coroutine? m_openRoutine = null;
    public Coroutine? m_closeRoutine = null;

    public Queue<ArchiveMenuEntry?> m_newEntries = new Queue<ArchiveMenuEntry?>();
    public int m_numFilledEntries = 0;

    public SaveSlotButton m_templateSlotButton = null!;

    public bool m_isClosing = false;

    public void OnDestroy()
    {
        Close();
    }

    public void OpenForSlot(SaveSlotButton slot)
    {
        if (IsOpen)
        {
            return;
        }

        if (m_closeRoutine != null)
        {
            StopCoroutine(m_closeRoutine);
            m_closeRoutine = null;
        }

        m_selectedSlotIndex = slot.SaveSlotIndex;
        m_selectedSlotIsEmpty = slot.saveFileState == SaveSlotButton.SaveFileStates.Empty;
        m_templateSlotButton = slot;
        IsOpen = true;
        m_isClosing = false;

        m_openRoutine = StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (!IsOpen || m_isClosing)
        {
            return;
        }

        m_isClosing = true;
        if (m_openRoutine != null)
        {
            StopCoroutine(m_openRoutine);
            m_openRoutine = null;
        }
        m_closeRoutine = StartCoroutine(CloseRoutine());
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
        // Prevent input (mostly cancelling) until the menu is open.
        // Input will be resumed by ui.ShowMenu();
        GameManager.instance.inputHandler.StopUIInput();

        m_screen?.Dispose();

        var title = m_selectedSlotIsEmpty
            ? Strings.TitleLoadIntoEmpty // "Load from archive"
            : Strings.TitleArchiveSwap; // "Archive / Swap"

        var slotInfo = SaveName.TryGetSlotName(m_selectedSlotIndex, out string slotName)
            ? Strings.NamedSlotInfo(name: slotName, index: m_selectedSlotIndex) // $"Target: Slot {index}. \"{name}\""
            : Strings.UnnamedSlotInfo(index: m_selectedSlotIndex); // $"Target: Slot {index}."
        var statusMessage = $"{slotInfo}\n{Strings.LoadingText}"; // "Loading save slots..."

        m_screen = new ArchiveMenuScreen(title, statusMessage, Close, out Text statusText);

        m_newEntries.Clear();
        m_numFilledEntries = 0;

        // Add the first entries immediately so that there is something to select
        for (int i = 1; i <= 4; i++)
        {
            AddEntry(i);
        }
        yield return new WaitUntil(() => m_newEntries.Count == 4);
        for (int i = 1; i <= 4; i++)
        {
            if (m_newEntries.Dequeue() is ArchiveMenuEntry entry)
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

        yield return StartCoroutine(m_screen.Show());

        for (int i = 5; i <= 50; i++)
        {
            AddEntry(i);
            yield return new WaitUntil(() => m_newEntries.Count > 0);
            if (m_newEntries.Dequeue() is ArchiveMenuEntry entry)
            {
                m_screen.Add(entry);
            }

            yield return new WaitForSeconds(0.01f); // slight delay to avoid freezing the game
        }

        if (m_numFilledEntries == 0)
        {
            statusText.text = $"{slotInfo}\n{Strings.NoLoadableSavesFound}"; // "No loadable saves found"
        }
        else
        {
            statusText.text = $"{slotInfo}";
        }
        m_openRoutine = null;
    }

    public void AddEntry(int slotIndex)
    {
        if (slotIndex == m_selectedSlotIndex)
        {
            m_newEntries.Enqueue(null);
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
            m_newEntries.Enqueue(null);
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
        m_newEntries.Enqueue(entry);
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
            yield return StartCoroutine(m_screen.Hide());

            m_screen.Dispose();
            m_screen = null;
        }
        IsOpen = false;

        yield return StartCoroutine(ui.GoToProfileMenu());
        m_isClosing = false;
        m_closeRoutine = null;
    }
}
