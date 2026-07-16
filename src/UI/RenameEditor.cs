using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class RenameEditor : IDisposable
{
    public SaveSlotButton m_saveSlotButton;
    public InputHandler m_inputHandler;

    public TextInput<string> m_renameInput;
    public bool m_inputHasEnded = false;

    public bool IsOpen = false;

    Action m_onCloseCallback;

    public RenameEditor(SaveSlotButton button, Action onCloseCallback, Action<string> onValueChangedCallback)
    {
        m_saveSlotButton = button;
        m_inputHandler = GameManager.instance.inputHandler;

        m_onCloseCallback = onCloseCallback;

        m_renameInput = BuildTextInput(m_saveSlotButton);

        m_renameInput.OnTextValueChanged += onValueChangedCallback;

        // Ideally I would listen to m_renameInput.InputField.onSubmit to see if the user pressed enter, but that doesn't seem to work.
        // There is also a wasCancelled property on the InputField, but that isn't always set when e.g. switching focus.
        // Also, this function is called before our Refresh() and before m_inputHandler.inputActions is updated, so we can't do anything in this callback.
        // So instead, we just set a flag and check in the Refresh() method what kind of exit the user did (cancel or submit).
        m_renameInput.InputField.onEndEdit.AddListener(_ => m_inputHasEnded = true);

        SetRenameEditorVisible(visible: false);
    }

    public void Dispose()
    {
        m_renameInput.Dispose();
    }

    public void Refresh()
    {
        if (!IsOpen)
        {
            return;
        }

        if (m_inputHandler.acceptingInput)
        {
            if (m_inputHandler.inputActions.MenuCancel.WasPressed)
            {
                SfmLogger.LogInfo("MenuCancel pressed, closing rename editor without saving changes.");
                m_inputHandler.inputActions.MenuCancel.ClearInputState();
                Close(saveChanges: false);
                return;
            }
            else if (m_inputHandler.inputActions.MenuSubmit.WasPressed)
            {
                SfmLogger.LogInfo("MenuSubmit pressed, closing rename editor and saving changes.");
                m_inputHandler.inputActions.MenuSubmit.ClearInputState();
                Close(saveChanges: true);
                return;
            }
        }

        if (m_inputHasEnded)
        {
            SfmLogger.LogInfo("Input field lost focus, closing rename editor without saving changes.");
            Close(saveChanges: false);
        }
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        m_inputHasEnded = false;

        if (SaveName.TryGetSlotName(m_saveSlotButton.SaveSlotIndex, out string name))
        {
            m_renameInput.Value = name;
        }
        else
        {
            m_renameInput.Value = string.Empty;
        }

        IsOpen = true;
        SetRenameEditorVisible(visible: true);

        // Block select-on-hover logic. If we don't do at least this much, the input would go away the moment the user moves their
        // mouse (or immediately if the rename button was clicked on with a mouse).
        // Setting this flag will allow the user to move their mouse around and click into the text box, but there won't be any automatic
        // selections. If the user clicks elsewhere, the onEndEdit event will fire and the rename editor will close without saving changes.
        // Only problem is that the user can still click on other buttons and trigger their actions, which is not ideal. I tried preventing
        // this, but I honestly can't figure out where the submit events for the other buttons are coming from.
        // It would be possible to simply set allowMouseInput to false, which would prevent this as well, but that also prevents
        // 1) clicking outside of the input field to close it, and 2) moving the cursor in the input field with the mouse.
        UIManager.instance.inputModule.focusOnMouseHover = false;

        EventSystem.current?.SetSelectedGameObject(m_renameInput.InputField.gameObject);
        m_renameInput.InputField.ActivateInputField();
    }

    public void Close(bool saveChanges)
    {
        if (!IsOpen)
        {
            return;
        }

        if (saveChanges)
        {
            UIManager.instance.uiAudioPlayer.PlaySubmit();

            SaveName.SetSlotName(m_saveSlotButton.SaveSlotIndex, m_renameInput.InputField.text);
        }
        else
        {
            UIManager.instance.uiAudioPlayer.PlayCancel();
        }

        IsOpen = false;
        SetRenameEditorVisible(visible: false);
        UIManager.instance.inputModule.focusOnMouseHover = true;
        m_onCloseCallback?.Invoke();
    }

    public void SetRenameEditorVisible(bool visible)
    {
        m_renameInput.Container.SetActive(visible);
        m_renameInput.Interactable = visible;
    }

    public static TextInput<string> BuildTextInput(SaveSlotButton saveSlotButton)
    {
        var input = new TextInput<string>("", TextModels.ForStrings(), "");
        input.Container.name = "SFM-RenameEditor";
        input.InputField.gameObject.name = "SFM-RenameEditorTextInput";
        input.InputField.textComponent.gameObject.name = "SFM-RenameEditorText";

        RectTransform slotRect = saveSlotButton.GetComponent<RectTransform>();
        RectTransform actionRowRect = saveSlotButton.clearSaveButton.GetComponent<RectTransform>();

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
        input.InputField.textComponent.fontSize = saveSlotButton.locationText.fontSize;

        return input;
    }

}
