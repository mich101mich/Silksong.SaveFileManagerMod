using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
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

    public Text m_nameLabel = null!;
    public TextInput<string> m_renameInput = null!;

    public bool m_isRenaming = false;

    public void Initialize(SaveSlotButton button)
    {
        m_saveSlotButton = button;
        m_inputHandler = GameManager.instance.inputHandler;
        m_archiveMenu = SaveFileManagerPlugin.s_instance.m_archiveMenu;

        SfmLogger.LogInfo($"Creating SaveOptions for {button.name}");

        m_actionRow = new SaveSlotActionRow(m_saveSlotButton, OpenRenameEditor, OpenArchiveMenu);

        m_nameLabel = CreateNameLabel();
        m_renameInput = BuildRenameEditor();
        SetRenameEditorVisible(visible: false);
    }

    public void OnDestroy()
    {
        SfmLogger.LogInfo($"Destroying SaveOptions for {m_saveSlotButton.name}");

        m_actionRow.Dispose();
        m_renameInput.Dispose();

        if (m_nameLabel != null)
        {
            UnityEngine.Object.Destroy(m_nameLabel.gameObject);
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

        if (SaveName.TryGetSlotName(m_saveSlotButton.SaveSlotIndex, out string name))
        {
            m_nameLabel.text = name;
            m_nameLabel.gameObject.SetActive(!m_isRenaming);
        }
        else
        {
            m_nameLabel.text = string.Empty;
            m_nameLabel.gameObject.SetActive(false);
        }
    }

    public void OpenRenameEditor()
    {
        if (m_isRenaming || m_archiveMenu.IsOpen)
        {
            return;
        }

        if (SaveName.TryGetSlotName(m_saveSlotButton.SaveSlotIndex, out string name))
        {
            m_renameInput.Value = name;
        }
        else
        {
            m_renameInput.Value = string.Empty;
        }

        m_isRenaming = true;
        m_nameLabel.gameObject.SetActive(false);
        m_actionRow.SetButtonVisibility(visible: false);
        SetRenameEditorVisible(visible: true);
        EventSystem.current?.SetSelectedGameObject(m_renameInput.InputField.gameObject);
        m_renameInput.InputField.ActivateInputField();
    }

    public void OpenArchiveMenu()
    {
        if (m_isRenaming)
        {
            return;
        }

        m_archiveMenu.OpenForSlot(m_saveSlotButton);
    }

    public void CloseRenameEditor(bool saveChanges)
    {
        if (!m_isRenaming)
        {
            return;
        }

        if (saveChanges)
        {
            SaveName.SetSlotName(m_saveSlotButton.SaveSlotIndex, m_renameInput.InputField.text);
        }

        m_isRenaming = false;
        SetRenameEditorVisible(visible: false);
        m_actionRow.SetButtonVisibility(visible: true, focusedElement: m_actionRow.m_rename);
        SyncFromSlotState();
    }

    public Text CreateNameLabel()
    {
        Text label = UnityEngine.Object.Instantiate(m_saveSlotButton.locationText, m_saveSlotButton.locationText.transform.parent);
        label.name = "SFM-SaveNameLabel";
        label.fontSize = Mathf.Max(16, m_saveSlotButton.locationText.fontSize - 6);
        label.color = new Color(1f, 0.86f, 0.58f, 1f);
        label.raycastTarget = false;
        label.rectTransform.anchoredPosition = m_saveSlotButton.locationText.rectTransform.anchoredPosition + new Vector2(0f, 58f);
        label.gameObject.SetActive(value: false);
        return label;
    }

    public TextInput<string> BuildRenameEditor()
    {
        TextInput<string> input = new("", TextModels.ForStrings(), "");
        input.Container.name = "SFM-RenameEditor";

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
        input.InputField.textComponent.fontSize = m_nameLabel.fontSize;
        input.InputField.textComponent.color = new Color(1f, 0.86f, 0.58f, 1f);

        return input;
    }

    public void SetRenameEditorVisible(bool visible)
    {
        m_renameInput.Container.SetActive(visible);
        m_renameInput.Interactable = visible;
    }
}
