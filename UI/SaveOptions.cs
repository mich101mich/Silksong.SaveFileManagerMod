using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public sealed class SaveOptions : MonoBehaviour
{
    public SaveSlotButton m_saveSlotButton = null!;
    public InputHandler m_inputHandler = null!;

    public SaveSlotActionRow m_actionRow = null!;
    public ArchiveMenuController m_archiveMenu = null!;

    public Text m_customNameLabel = null!;
    public TextInput<string> m_renameInput = null!;

    public bool m_isRenaming = false;

    public void Initialize(SaveSlotButton button)
    {
        m_saveSlotButton = button;
        m_inputHandler = GameManager.instance.inputHandler;
        m_archiveMenu = SaveFileManagerPlugin.s_instance.m_archiveMenu;

        SfmLogger.LogInfo($"Creating SaveOptions for {button.name}");

        m_actionRow = new SaveSlotActionRow(m_saveSlotButton, OpenRenameEditor, OpenArchiveMenu);

        m_customNameLabel = CreateCustomNameLabel();
        m_renameInput = BuildInlineRenameEditor();
        SetRenameEditorVisible(visible: false);
    }

    public void OnDestroy()
    {
        SfmLogger.LogInfo($"Destroying SaveOptions for {m_saveSlotButton.name}");

        m_actionRow.Dispose();
        m_renameInput.Dispose();

        if (m_customNameLabel != null)
        {
            UnityEngine.Object.Destroy(m_customNameLabel.gameObject);
        }
    }

    public void SetupNavigation(SaveOptions? previousOptions, SaveOptions? nextOptions)
    {
        m_actionRow.SetupNavigation(previousOptions?.m_actionRow, nextOptions?.m_actionRow);
    }

    public void Update()
    {
        if (m_isRenaming && m_inputHandler.acceptingInput)
        {
            bool submitPressed = m_inputHandler.inputActions.MenuSubmit.WasPressed
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter);

            bool cancelPressed = m_inputHandler.inputActions.MenuCancel.WasPressed
                || Input.GetKeyDown(KeyCode.Escape);

            if (cancelPressed)
            {
                m_inputHandler.inputActions.MenuCancel.ClearInputState();
                CloseRenameEditor(saveChanges: false);
            }
            else if (submitPressed)
            {
                m_inputHandler.inputActions.MenuSubmit.ClearInputState();
                CloseRenameEditor(saveChanges: true);
            }
        }

        SyncFromSlotState();
    }

    public void SyncFromSlotState()
    {
        if (!m_isRenaming)
        {
            m_actionRow.Refresh();
        }

        if (SaveFileManagerPlugin.s_instance.TryGetCustomName(m_saveSlotButton.SaveSlotIndex, out string customName))
        {
            m_customNameLabel.text = customName;
            m_customNameLabel.gameObject.SetActive(!m_isRenaming);
        }
        else
        {
            m_customNameLabel.text = string.Empty;
            m_customNameLabel.gameObject.SetActive(false);
        }
    }

    private void OpenRenameEditor()
    {
        if (m_isRenaming || m_archiveMenu.IsOpen)
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

        m_isRenaming = true;
        m_customNameLabel.gameObject.SetActive(false);
        m_actionRow.SetButtonVisibility(visible: false);
        SetRenameEditorVisible(visible: true);
        EventSystem.current?.SetSelectedGameObject(m_renameInput.InputField.gameObject);
        m_renameInput.InputField.ActivateInputField();
    }

    private void OpenArchiveMenu()
    {
        if (m_isRenaming)
        {
            return;
        }

        m_archiveMenu.OpenForSlot(m_saveSlotButton);
    }

    private void CloseRenameEditor(bool saveChanges)
    {
        if (!m_isRenaming)
        {
            return;
        }

        if (saveChanges)
        {
            SaveFileManagerPlugin.s_instance.SetCustomName(m_saveSlotButton.SaveSlotIndex, m_renameInput.InputField.text);
        }

        m_isRenaming = false;
        SetRenameEditorVisible(visible: false);
        m_actionRow.SetButtonVisibility(visible: true, focusedElement: m_actionRow.m_rename);
        SyncFromSlotState();
    }

    private Text CreateCustomNameLabel()
    {
        Text label = UnityEngine.Object.Instantiate(m_saveSlotButton.locationText, m_saveSlotButton.locationText.transform.parent);
        label.name = "SFM-CustomSaveNameLabel";
        label.fontSize = Mathf.Max(16, m_saveSlotButton.locationText.fontSize - 6);
        label.color = new Color(1f, 0.86f, 0.58f, 1f);
        label.raycastTarget = false;
        label.rectTransform.anchoredPosition = m_saveSlotButton.locationText.rectTransform.anchoredPosition + new Vector2(0f, 58f);
        label.gameObject.SetActive(value: false);
        return label;
    }

    private TextInput<string> BuildInlineRenameEditor()
    {
        TextInput<string> input = new("", TextModels.ForStrings(), "");
        input.Container.name = "SFM-InlineRenameEditor";

        RectTransform slotRect = m_saveSlotButton.GetComponent<RectTransform>();
        RectTransform actionRowRect = m_saveSlotButton.clearSaveButton.GetComponent<RectTransform>();

        input.RectTransform.SetParent(actionRowRect.parent, worldPositionStays: false);
        input.RectTransform.anchorMin = actionRowRect.anchorMin;
        input.RectTransform.anchorMax = actionRowRect.anchorMax;
        input.RectTransform.pivot = actionRowRect.pivot;
        input.RectTransform.anchoredPosition = new Vector2(0f, actionRowRect.anchoredPosition.y);
        input.RectTransform.sizeDelta = new Vector2(slotRect.rect.width, actionRowRect.rect.height);

        RectTransform inputRect = input.InputField.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(slotRect.rect.width, actionRowRect.rect.height);

        input.LabelText.gameObject.SetActive(value: false);
        input.DescriptionText.gameObject.SetActive(value: false);

        input.InputField.characterLimit = 48;
        input.InputField.lineType = InputField.LineType.SingleLine;
        input.InputField.textComponent.fontSize = m_customNameLabel.fontSize;
        input.InputField.textComponent.color = new Color(1f, 0.86f, 0.58f, 1f);

        return input;
    }

    private void SetRenameEditorVisible(bool visible)
    {
        m_renameInput.Container.SetActive(visible);
        m_renameInput.Interactable = visible;
    }
}
