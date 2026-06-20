using BepInEx.Logging;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public sealed class SaveOptions : IDisposable
{
    private readonly SaveSlotButton m_saveSlotButton;
    private readonly InputHandler m_inputHandler;

    private readonly SaveSlotActionRow m_actionRow;
    private readonly ArchiveMenuController m_archiveMenu;

    private readonly Text m_customNameLabel;

    private readonly GameObject m_renamePanel;
    private readonly CanvasGroup m_renamePanelGroup;
    private readonly TextInput<string> m_renameInput;
    private readonly TextButton m_renameSaveButton;
    private readonly TextButton m_renameCancelButton;

    private readonly SaveOptionsUpdater m_updater;

    private bool m_renamePanelOpen;
    private bool m_isDisposed;

    public SaveOptions(SaveSlotButton button)
    {
        m_saveSlotButton = button;
        m_inputHandler = GameManager.instance.inputHandler;
        m_archiveMenu = SaveFileManagerPlugin.s_instance.m_archiveMenu;

        SfmLogger.LogInfo($"Creating SaveOptions for {button.name}");

        m_actionRow = new SaveSlotActionRow(m_saveSlotButton, OpenRenamePanel, OpenArchiveMenu);

        m_customNameLabel = CreateCustomNameLabel();

        (
            m_renamePanel,
            m_renamePanelGroup,
            m_renameInput,
            m_renameSaveButton,
            m_renameCancelButton
        ) = BuildRenamePanel();

        SetRenamePanelVisible(visible: false);
        m_renamePanelOpen = false;

        m_updater = m_saveSlotButton.gameObject.AddComponent<SaveOptionsUpdater>();
        m_updater.Initialize(this);
    }

    public void Dispose()
    {
        if (m_isDisposed)
        {
            return;
        }

        m_isDisposed = true;
        SfmLogger.LogInfo($"Disposing SaveOptions for {m_saveSlotButton.name}");

        m_updater.ClearOwner();

        m_actionRow.Dispose();

        m_renameInput.Dispose();
        m_renameSaveButton.Dispose();
        m_renameCancelButton.Dispose();

        if (m_customNameLabel != null)
        {
            UnityEngine.Object.Destroy(m_customNameLabel.gameObject);
        }

        if (m_renamePanel != null)
        {
            UnityEngine.Object.Destroy(m_renamePanel);
        }

        if (m_updater != null)
        {
            UnityEngine.Object.Destroy(m_updater);
        }
    }

    public void SetupNavigation(SaveOptions? previousOptions, SaveOptions? nextOptions)
    {
        m_actionRow.SetupNavigation(previousOptions?.m_actionRow, nextOptions?.m_actionRow);
    }

    public void Tick()
    {
        if (m_isDisposed)
        {
            return;
        }

        if (m_renamePanelOpen
            && m_inputHandler.acceptingInput
            && m_inputHandler.inputActions.MenuCancel.WasPressed)
        {
            m_inputHandler.inputActions.MenuCancel.ClearInputState();
            CloseRenamePanel(restoreSelection: true);
        }

        SyncFromSlotState();
    }

    public void SyncFromSlotState()
    {
        if (m_isDisposed)
        {
            return;
        }

        m_actionRow.Refresh();

        if (SaveFileManagerPlugin.s_instance.TryGetCustomName(m_saveSlotButton.SaveSlotIndex, out string customName))
        {
            m_customNameLabel.text = customName;
            m_customNameLabel.gameObject.SetActive(value: true);
        }
        else
        {
            m_customNameLabel.text = string.Empty;
            m_customNameLabel.gameObject.SetActive(value: false);
        }
    }

    private void OpenRenamePanel()
    {
        if (m_renamePanelOpen || m_archiveMenu.IsOpen)
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

        m_renamePanelOpen = true;
        SetRenamePanelVisible(visible: true);
        SetRenamePanelNavigation();
        FocusGameObject(m_renameInput.SelectableComponent.gameObject);
        m_renameInput.InputField.ActivateInputField();
    }

    private void OpenArchiveMenu()
    {
        if (m_renamePanelOpen)
        {
            CloseRenamePanel(restoreSelection: false);
        }

        m_archiveMenu.OpenForSlot(m_saveSlotButton);
    }

    private void SaveRename()
    {
        SaveFileManagerPlugin.s_instance.SetCustomName(m_saveSlotButton.SaveSlotIndex, m_renameInput.InputField.text);
        CloseRenamePanel(restoreSelection: true);
    }

    private void CloseRenamePanel(bool restoreSelection)
    {
        if (!m_renamePanelOpen)
        {
            return;
        }

        m_renamePanelOpen = false;
        SetRenamePanelVisible(visible: false);

        if (restoreSelection)
        {
            FocusGameObject(m_saveSlotButton.gameObject);
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
            OnSubmit = () => CloseRenamePanel(restoreSelection: true)
        };
        AttachPanelElement(cancelButton, root.transform, new Vector2(0f, -132f));

        return (root, group, input, saveButton, cancelButton);
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

    private void SetRenamePanelVisible(bool visible)
    {
        m_renamePanel.SetActive(visible);
        m_renamePanelGroup.alpha = visible ? 1f : 0f;
        m_renamePanelGroup.blocksRaycasts = visible;
        m_renamePanelGroup.interactable = visible;
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
}
