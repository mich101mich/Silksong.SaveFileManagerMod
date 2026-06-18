using BepInEx.Logging;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public sealed class SaveOptions : IDisposable
{
    internal static ManualLogSource s_logger => SaveFileManagerPlugin.s_logger;

    private enum PanelMode
    {
        None,
        Rename,
        Archive,
    }

    private readonly SaveSlotButton m_saveSlotButton;
    private readonly RestoreSaveButton m_restoreSaveButton;
    private readonly InputHandler m_inputHandler;

    private readonly TextButton m_renameButton;
    private readonly TextButton m_archiveButton;
    private readonly Text m_customNameLabel;

    private readonly GameObject m_renamePanel;
    private readonly CanvasGroup m_renamePanelGroup;
    private readonly TextInput<string> m_renameInput;
    private readonly TextButton m_renameSaveButton;
    private readonly TextButton m_renameCancelButton;

    private readonly GameObject m_archivePanel;
    private readonly CanvasGroup m_archivePanelGroup;
    private readonly Text m_archiveHeader;
    private readonly Text m_archiveInfo;
    private readonly List<TextButton> m_archiveItemButtons = new();
    private readonly TextButton m_archiveCurrentButton;
    private readonly TextButton m_archiveBackButton;

    private readonly SaveOptionsUpdater m_updater;

    private SaveSlotButton.SaveFileStates m_lastSaveFileState;
    private SaveSlotButton.SlotState m_lastSlotState;
    private PanelMode m_panelMode;
    private Selectable? m_lastInvoker;
    private bool m_isDisposed;

    public SaveOptions(SaveSlotButton button)
    {
        m_saveSlotButton = button;
        m_inputHandler = GameManager.instance.inputHandler;

        m_restoreSaveButton = typeof(SaveSlotButton)
            .GetField("restoreSaveButton", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(m_saveSlotButton) as RestoreSaveButton
            ?? throw new Exception("Could not find restoreSaveButton");

        s_logger.LogInfo($"Creating SaveOptions for {button.name}");

        TextButton renameButton = new("Rename", "Set temporary custom save name");
        renameButton.OnSubmit = () =>
        {
            m_lastInvoker = renameButton.SelectableComponent;
            OpenRenamePanel();
        };
        m_renameButton = renameButton;
        AttachInlineElement(m_renameButton, new Vector2(0f, -470f));

        TextButton archiveButton = new("Archive", "Archive/replace placeholder actions");
        archiveButton.OnSubmit = () =>
        {
            m_lastInvoker = archiveButton.SelectableComponent;
            OpenArchivePanel();
        };
        m_archiveButton = archiveButton;
        AttachInlineElement(m_archiveButton, new Vector2(0f, -545f));

        m_customNameLabel = CreateCustomNameLabel();

        (
            m_renamePanel,
            m_renamePanelGroup,
            m_renameInput,
            m_renameSaveButton,
            m_renameCancelButton
        ) = BuildRenamePanel();

        (
            m_archivePanel,
            m_archivePanelGroup,
            m_archiveHeader,
            m_archiveInfo,
            m_archiveCurrentButton,
            m_archiveBackButton
        ) = BuildArchivePanel();

        m_updater = m_saveSlotButton.gameObject.AddComponent<SaveOptionsUpdater>();
        m_updater.Initialize(this);

        SetPanelVisible(m_renamePanel, m_renamePanelGroup, visible: false);
        SetPanelVisible(m_archivePanel, m_archivePanelGroup, visible: false);

        m_lastSaveFileState = SaveSlotButton.SaveFileStates.NotStarted;
        m_lastSlotState = SaveSlotButton.SlotState.Hidden;
        m_panelMode = PanelMode.None;

        SyncFromSlotState();
    }

    public void Dispose()
    {
        if (m_isDisposed)
        {
            return;
        }

        m_isDisposed = true;
        s_logger.LogInfo($"Disposing SaveOptions for {m_saveSlotButton.name}");

        m_updater.ClearOwner();

        m_renameButton.Dispose();
        m_archiveButton.Dispose();

        m_renameInput.Dispose();
        m_renameSaveButton.Dispose();
        m_renameCancelButton.Dispose();

        foreach (TextButton archiveItemButton in m_archiveItemButtons)
        {
            archiveItemButton.Dispose();
        }
        m_archiveCurrentButton.Dispose();
        m_archiveBackButton.Dispose();

        if (m_customNameLabel != null)
        {
            UnityEngine.Object.Destroy(m_customNameLabel.gameObject);
        }

        if (m_renamePanel != null)
        {
            UnityEngine.Object.Destroy(m_renamePanel);
        }

        if (m_archivePanel != null)
        {
            UnityEngine.Object.Destroy(m_archivePanel);
        }

        if (m_updater != null)
        {
            UnityEngine.Object.Destroy(m_updater);
        }
    }

    public void Tick()
    {
        if (m_isDisposed)
        {
            return;
        }

        if (m_panelMode != PanelMode.None
            && m_inputHandler.acceptingInput
            && m_inputHandler.inputActions.MenuCancel.WasPressed)
        {
            m_inputHandler.inputActions.MenuCancel.ClearInputState();
            ClosePanel(restoreSelection: true);
        }

        SyncFromSlotState();
    }

    public void SyncFromSlotState()
    {
        if (m_isDisposed || m_saveSlotButton == null || !m_saveSlotButton.gameObject.activeInHierarchy)
        {
            return;
        }

        SaveSlotButton.SaveFileStates saveFileState = m_saveSlotButton.saveFileState;
        SaveSlotButton.SlotState slotState = m_saveSlotButton.State;

        bool hasValidSave = saveFileState == SaveSlotButton.SaveFileStates.LoadedStats;
        bool hasAnySave = hasValidSave
            || saveFileState == SaveSlotButton.SaveFileStates.Corrupted
            || saveFileState == SaveSlotButton.SaveFileStates.Incompatible;

        bool blockedState = slotState == SaveSlotButton.SlotState.Hidden
            || slotState == SaveSlotButton.SlotState.OperationInProgress
            || slotState == SaveSlotButton.SlotState.ClearPrompt
            || slotState == SaveSlotButton.SlotState.ClearConfirm
            || slotState == SaveSlotButton.SlotState.RestoreSave
            || slotState == SaveSlotButton.SlotState.BlackThreadInfected;

        bool showInlineButtons = !blockedState;
        bool canRename = showInlineButtons && hasValidSave;
        bool canArchive = showInlineButtons && (hasAnySave || saveFileState == SaveSlotButton.SaveFileStates.Empty);

        m_renameButton.Container.SetActive(canRename);
        m_archiveButton.Container.SetActive(canArchive);

        if (hasValidSave && SaveFileManagerPlugin.s_instance.TryGetCustomName(m_saveSlotButton.SaveSlotIndex, out string customName))
        {
            m_customNameLabel.text = customName;
            m_customNameLabel.gameObject.SetActive(value: true);
        }
        else
        {
            m_customNameLabel.text = string.Empty;
            m_customNameLabel.gameObject.SetActive(value: false);
        }

        if ((!canRename && m_panelMode == PanelMode.Rename) || (!canArchive && m_panelMode == PanelMode.Archive))
        {
            ClosePanel(restoreSelection: false);
        }

        bool stateChanged = saveFileState != m_lastSaveFileState || slotState != m_lastSlotState;
        bool panelOpen = m_panelMode != PanelMode.None;

        if (stateChanged || panelOpen)
        {
            UpdateInlineButtonPositions();
            UpdateInlineNavigation(hasValidSave, hasAnySave);

            if (m_panelMode == PanelMode.Archive)
            {
                UpdateArchiveHeader();
            }

            m_lastSaveFileState = saveFileState;
            m_lastSlotState = slotState;
        }
    }

    private void OpenRenamePanel()
    {
        if (m_panelMode != PanelMode.None)
        {
            return;
        }

        if (SaveFileManagerPlugin.s_instance.TryGetCustomName(m_saveSlotButton.SaveSlotIndex, out string customName))
        {
            m_renameInput.Value = customName;
        }
        else
        {
            m_renameInput.Value = string.Empty;
        }

        SetPanelVisible(m_renamePanel, m_renamePanelGroup, visible: true);
        SetPanelVisible(m_archivePanel, m_archivePanelGroup, visible: false);
        m_panelMode = PanelMode.Rename;

        SetRenamePanelNavigation();
        FocusGameObject(m_renameInput.SelectableComponent.gameObject);
        m_renameInput.InputField.ActivateInputField();
    }

    private void OpenArchivePanel()
    {
        if (m_panelMode != PanelMode.None)
        {
            return;
        }

        SetPanelVisible(m_archivePanel, m_archivePanelGroup, visible: true);
        SetPanelVisible(m_renamePanel, m_renamePanelGroup, visible: false);
        m_panelMode = PanelMode.Archive;

        UpdateArchiveHeader();
        SetArchivePanelNavigation();

        Selectable firstSelection = m_archiveItemButtons.Count > 0
            ? m_archiveItemButtons[0].SelectableComponent
            : m_archiveBackButton.SelectableComponent;
        FocusGameObject(firstSelection.gameObject);
    }

    private void SaveRename()
    {
        SaveFileManagerPlugin.s_instance.SetCustomName(m_saveSlotButton.SaveSlotIndex, m_renameInput.InputField.text);
        SyncFromSlotState();
        ClosePanel(restoreSelection: true);
    }

    private void ClosePanel(bool restoreSelection)
    {
        SetPanelVisible(m_renamePanel, m_renamePanelGroup, visible: false);
        SetPanelVisible(m_archivePanel, m_archivePanelGroup, visible: false);
        m_panelMode = PanelMode.None;

        if (restoreSelection && m_lastInvoker != null)
        {
            FocusGameObject(m_lastInvoker.gameObject);
        }
    }

    private void ArchiveCurrentPlaceholder()
    {
        if (m_saveSlotButton.saveFileState == SaveSlotButton.SaveFileStates.Empty)
        {
            m_archiveInfo.text = "Placeholder: would import selected archive entry into this empty slot.";
        }
        else
        {
            m_archiveInfo.text = "Placeholder: would move current save into archive storage.";
        }
    }

    private void ReplaceWithArchivePlaceholder(SaveFileManagerPlugin.MockArchiveEntry entry)
    {
        if (m_saveSlotButton.saveFileState == SaveSlotButton.SaveFileStates.Empty)
        {
            m_archiveInfo.text = $"Placeholder: would import '{entry.Label}' into this empty slot.";
        }
        else
        {
            m_archiveInfo.text = $"Placeholder: would replace current slot with '{entry.Label}'.";
        }
    }

    private void UpdateArchiveHeader()
    {
        bool empty = m_saveSlotButton.saveFileState == SaveSlotButton.SaveFileStates.Empty;
        m_archiveHeader.text = empty ? "Archive (Import)" : "Archive";
        m_archiveCurrentButton.ButtonText.text = empty ? "Import Selected Archive" : "Archive Current Save";
        m_archiveCurrentButton.DescriptionText.text = "Placeholder action";
    }

    private void UpdateInlineButtonPositions()
    {
        if (m_saveSlotButton.clearSaveButton.transform is not RectTransform clearRect)
        {
            return;
        }

        Vector2 clearPos = clearRect.anchoredPosition;
        m_renameButton.RectTransform.anchoredPosition = clearPos + new Vector2(0f, -92f);
        m_archiveButton.RectTransform.anchoredPosition = clearPos + new Vector2(0f, -184f);
    }

    private void UpdateInlineNavigation(bool hasValidSave, bool hasAnySave)
    {
        if (m_panelMode != PanelMode.None)
        {
            return;
        }

        Selectable? clear = m_saveSlotButton.clearSaveButton.GetComponent<Selectable>();
        Selectable? restore = m_restoreSaveButton;
        Selectable? rename = hasValidSave && m_renameButton.Container.activeInHierarchy
            ? m_renameButton.SelectableComponent
            : null;
        bool hasArchiveAction = hasAnySave || m_saveSlotButton.saveFileState == SaveSlotButton.SaveFileStates.Empty;
        Selectable? archive = hasArchiveAction && m_archiveButton.Container.activeInHierarchy
            ? m_archiveButton.SelectableComponent
            : null;

        List<Selectable> chain = new();
        if (restore != null && restore.gameObject.activeInHierarchy)
        {
            chain.Add(restore);
        }
        if (rename != null)
        {
            chain.Add(rename);
        }
        if (archive != null)
        {
            chain.Add(archive);
        }
        if (clear != null && clear.gameObject.activeInHierarchy)
        {
            chain.Add(clear);
        }

        if (chain.Count < 2)
        {
            return;
        }

        for (int i = 0; i < chain.Count; i++)
        {
            Selectable current = chain[i];
            Selectable up = chain[(i - 1 + chain.Count) % chain.Count];
            Selectable down = chain[(i + 1) % chain.Count];

            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = up;
            nav.selectOnDown = down;
            current.navigation = nav;
        }
    }

    private void SetRenamePanelNavigation()
    {
        SetLoopNav(
            m_renameInput.SelectableComponent,
            up: m_renameCancelButton.SelectableComponent,
            down: m_renameSaveButton.SelectableComponent
        );
        SetLoopNav(
            m_renameSaveButton.SelectableComponent,
            up: m_renameInput.SelectableComponent,
            down: m_renameCancelButton.SelectableComponent
        );
        SetLoopNav(
            m_renameCancelButton.SelectableComponent,
            up: m_renameSaveButton.SelectableComponent,
            down: m_renameInput.SelectableComponent
        );
    }

    private void SetArchivePanelNavigation()
    {
        List<Selectable> chain = new();
        foreach (TextButton button in m_archiveItemButtons)
        {
            if (button.Container.activeInHierarchy)
            {
                chain.Add(button.SelectableComponent);
            }
        }
        chain.Add(m_archiveCurrentButton.SelectableComponent);
        chain.Add(m_archiveBackButton.SelectableComponent);

        for (int i = 0; i < chain.Count; i++)
        {
            Selectable up = chain[(i - 1 + chain.Count) % chain.Count];
            Selectable down = chain[(i + 1) % chain.Count];
            SetLoopNav(chain[i], up, down);
        }
    }

    private Text CreateCustomNameLabel()
    {
        Text label = UnityEngine.Object.Instantiate(m_saveSlotButton.locationText, m_saveSlotButton.locationText.transform.parent);
        label.name = "CustomSaveNameLabel";
        label.fontSize = Mathf.Max(16, m_saveSlotButton.locationText.fontSize - 6);
        label.color = new Color(1f, 0.86f, 0.58f, 1f);
        label.raycastTarget = false;
        label.rectTransform.anchoredPosition = m_saveSlotButton.locationText.rectTransform.anchoredPosition + new Vector2(0f, 58f);
        label.gameObject.SetActive(value: false);
        return label;
    }

    private void AttachInlineElement(MenuElement element, Vector2 anchoredPosition)
    {
        element.RectTransform.SetParent(m_saveSlotButton.transform, worldPositionStays: false);
        element.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        element.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        element.RectTransform.pivot = new Vector2(0.5f, 0.5f);
        element.RectTransform.anchoredPosition = anchoredPosition;
    }

    private (
        GameObject root,
        CanvasGroup group,
        TextInput<string> input,
        TextButton saveButton,
        TextButton cancelButton
    ) BuildRenamePanel()
    {
        GameObject root = CreatePanelRoot("SFM-RenamePanel", out CanvasGroup group, new Vector2(900f, 360f), new Vector2(0f, -120f));
        root.transform.SetParent(m_saveSlotButton.transform, worldPositionStays: false);
        _ = CreatePanelLabel(root.transform, "Rename Save", 24, new Vector2(0f, 132f));

        TextInput<string> input = new("Name", TextModels.ForStrings(), "Temporary session label");
        input.InputField.characterLimit = 48;
        AttachPanelElement(input, root.transform, new Vector2(0f, 40f));

        TextButton saveButton = new("Save", "Apply custom name")
        {
            OnSubmit = SaveRename
        };
        AttachPanelElement(saveButton, root.transform, new Vector2(0f, -54f));

        TextButton cancelButton = new("Back", "Close rename panel")
        {
            OnSubmit = () => ClosePanel(restoreSelection: true)
        };
        AttachPanelElement(cancelButton, root.transform, new Vector2(0f, -132f));

        return (root, group, input, saveButton, cancelButton);
    }

    private (
        GameObject root,
        CanvasGroup group,
        Text header,
        Text info,
        TextButton currentButton,
        TextButton backButton
    ) BuildArchivePanel()
    {
        GameObject root = CreatePanelRoot("SFM-ArchivePanel", out CanvasGroup group, new Vector2(980f, 520f), new Vector2(0f, -96f));
        root.transform.SetParent(m_saveSlotButton.transform, worldPositionStays: false);

        Text header = CreatePanelLabel(root.transform, "Archive", 24, new Vector2(0f, 210f));
        Text info = CreatePanelLabel(root.transform, "Frontend-only placeholders. No save data is modified.", 16, new Vector2(0f, 166f));

        float y = 98f;
        foreach (SaveFileManagerPlugin.MockArchiveEntry entry in SaveFileManagerPlugin.s_instance.MockArchiveEntries)
        {
            SaveFileManagerPlugin.MockArchiveEntry currentEntry = entry;
            TextButton button = new(entry.Label, entry.Details)
            {
                OnSubmit = () => ReplaceWithArchivePlaceholder(currentEntry)
            };
            AttachPanelElement(button, root.transform, new Vector2(0f, y));
            m_archiveItemButtons.Add(button);
            y -= 74f;
        }

        TextButton currentButton = new("Archive Current Save", "Placeholder action")
        {
            OnSubmit = ArchiveCurrentPlaceholder
        };
        AttachPanelElement(currentButton, root.transform, new Vector2(0f, y - 4f));

        TextButton backButton = new("Back", "Close archive panel")
        {
            OnSubmit = () => ClosePanel(restoreSelection: true)
        };
        AttachPanelElement(backButton, root.transform, new Vector2(0f, y - 82f));

        return (root, group, header, info, currentButton, backButton);
    }

    private static GameObject CreatePanelRoot(string name, out CanvasGroup group, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject root = new(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.84f);
        background.raycastTarget = true;

        return root;
    }

    private Text CreatePanelLabel(Transform parent, string content, int fontSize, Vector2 anchoredPosition)
    {
        Text label = UnityEngine.Object.Instantiate(m_saveSlotButton.locationText, parent);
        label.name = "PanelLabel";
        label.text = content;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        return label;
    }

    private static void AttachPanelElement(MenuElement element, Transform parent, Vector2 anchoredPosition)
    {
        element.RectTransform.SetParent(parent, worldPositionStays: false);
        element.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        element.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        element.RectTransform.pivot = new Vector2(0.5f, 0.5f);
        element.RectTransform.anchoredPosition = anchoredPosition;
    }

    private static void SetPanelVisible(GameObject panel, CanvasGroup group, bool visible)
    {
        panel.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    private static void SetLoopNav(Selectable selectable, Selectable up, Selectable down)
    {
        Navigation nav = selectable.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = up;
        nav.selectOnDown = down;
        selectable.navigation = nav;
    }

    private static void FocusGameObject(GameObject obj)
    {
        EventSystem? eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(obj);
        }
    }

    private sealed class SaveOptionsUpdater : MonoBehaviour
    {
        private SaveOptions? m_owner;

        public void Initialize(SaveOptions owner)
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
