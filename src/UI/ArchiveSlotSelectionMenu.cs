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

    public List<ArchiveMenuEntry?> m_rawEntries = new List<ArchiveMenuEntry?>();
    public int m_numFilledEntries = 0;

    public SaveSlotButton m_templateSlotButton = null!;

    private readonly bool[] m_entryReady = new bool[51];
    private int m_openGeneration = 0;
    private bool m_isClosing = false;

    public void OnDestroy()
    {
        m_openGeneration++;
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
        IsOpen = true;
        m_isClosing = false;

        int generation = ++m_openGeneration;
        m_openRoutine = StartCoroutine(OpenRoutine(generation));
    }

    public void Close()
    {
        if (!IsOpen || m_isClosing)
        {
            return;
        }

        m_isClosing = true;
        m_openGeneration++;
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

    public IEnumerator OpenRoutine(int generation)
    {
        // Prevent input (mostly cancelling) until the menu is open.
        // Input will be resumed by ui.ShowMenu();
        GameManager.instance.inputHandler.StopUIInput();

        m_screen?.Dispose();

        var title = m_selectedSlotIsEmpty
            ? Strings.TitleLoadIntoEmpty // "Load from archive"
            : Strings.TitleArchiveSwap; // "Archive / Swap"

        m_screen = new ArchiveMenuScreen(title, Close);

        var slotInfo = SaveName.TryGetSlotName(m_selectedSlotIndex, out string slotName)
            ? Strings.NamedSlotInfo(name: slotName, index: m_selectedSlotIndex) // $"Target: Slot {index}. \"{name}\""
            : Strings.UnnamedSlotInfo(index: m_selectedSlotIndex); // $"Target: Slot {index}."

        var statusText = m_screen.AddStatusLabel($"{slotInfo}\n{Strings.LoadingText}"); // "Loading save slots..."

        m_rawEntries.Clear();
        m_entryReady[0] = true;
        for (int i = 0; i <= 50; i++)
        {
            m_rawEntries.Add(null);
            m_entryReady[i] = i == 0;
        }
        m_numFilledEntries = 0;

        // Add the first entries immediately so that there is something to select
        for (int i = 1; i <= 4; i++)
        {
            AddEntry(i, generation);
        }
        yield return new WaitUntil(() => generation != m_openGeneration || FirstEntriesReady());
        if (generation != m_openGeneration)
        {
            yield break;
        }
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

        if (generation != m_openGeneration)
        {
            yield break;
        }

        m_screen.Show();
        yield return StartCoroutine(ui.ShowMenu(m_screen.MenuScreen));

        for (int i = 5; i <= 50; i++)
        {
            AddEntry(i, generation);
            int pendingSlot = i;
            yield return new WaitUntil(() => generation != m_openGeneration || m_entryReady[pendingSlot]);
            if (generation != m_openGeneration)
            {
                yield break;
            }
            if (m_rawEntries[i] is ArchiveMenuEntry entry)
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

    public void AddEntry(int slotIndex, int generation)
    {
        if (slotIndex == m_selectedSlotIndex)
        {
            m_entryReady[slotIndex] = true;
            return;
        }

        GameManager.instance.GetSaveStatsForSlot(slotIndex,
            (stats, message) => OnSaveStatsReceived(generation, slotIndex, stats, message));
    }

    public void OnSaveStatsReceived(int generation, int slotIndex, SaveStats? stats, string? message)
    {
        if (generation != m_openGeneration)
        {
            return;
        }

        bool isEmpty = stats == null && message == null;
        if (isEmpty && m_selectedSlotIsEmpty)
        {
            // Can't load from an empty slot => don't add it to the list
            m_entryReady[slotIndex] = true;
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
        m_rawEntries[slotIndex] = entry;
        m_entryReady[slotIndex] = true;
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
            if (m_screen.Container.activeSelf)
            {
                yield return StartCoroutine(ui.HideMenu(m_screen.MenuScreen));
            }

            m_screen.Dispose();
            m_screen = null;
        }
        IsOpen = false;

        yield return StartCoroutine(ui.GoToProfileMenu());
        m_isClosing = false;
        m_closeRoutine = null;
    }

    private bool FirstEntriesReady() =>
        m_entryReady[1] && m_entryReady[2] && m_entryReady[3] && m_entryReady[4];
}
