using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class RenameEditor : IDisposable
{
    public SaveSlotButton m_saveSlotButton;
    public InputHandler m_inputHandler;

    public InputField m_renameInput;

    public bool IsOpen = false;

    Action m_onCloseCallback;

    public RenameEditor(SaveSlotButton button, Action onCloseCallback, Action<string> onValueChangedCallback)
    {
        m_saveSlotButton = button;
        m_inputHandler = GameManager.instance.inputHandler;

        m_onCloseCallback = onCloseCallback;

        m_renameInput = BuildTextInput(m_saveSlotButton);
        m_renameInput.onValueChanged.AddListener(value => onValueChangedCallback(value));
        m_renameInput.onSubmit.AddListener(_ => Close(saveChanges: true));
        m_renameInput.onEndEdit.AddListener(_ => Close(saveChanges: false));

        SetRenameEditorVisible(visible: false);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(m_renameInput.gameObject);
    }

    public void Refresh()
    {
        if (!IsOpen)
        {
            return;
        }
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        if (SaveName.TryGetSlotName(m_saveSlotButton.SaveSlotIndex, out string name))
        {
            m_renameInput.text = name;
        }
        else
        {
            m_renameInput.text = string.Empty;
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

        EventSystem.current?.SetSelectedGameObject(m_renameInput.gameObject);
        m_renameInput.ActivateInputField();
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

            SaveName.SetSlotName(m_saveSlotButton.SaveSlotIndex, m_renameInput.text);
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
        m_renameInput.gameObject.SetActive(visible);
        // m_renameInput.Interactable = visible;
    }

    public static InputField BuildTextInput(SaveSlotButton saveSlotButton)
    {
        RectTransform slotRect = saveSlotButton.GetComponent<RectTransform>();
        RectTransform actionRowRect = saveSlotButton.clearSaveButton.GetComponent<RectTransform>();

        var canvas = SfmUtil.GetChild(UIManager.instance.gameObject, "UICanvas")!;
        var input = UnityEngine.Object.Instantiate(
            SfmUtil.GetChild(canvas, "GameOptionsMenuScreen/Content/CamShakeSetting/CamShakePopupOption")!,
            actionRowRect.parent,
            worldPositionStays: false
        );
        input.SetActive(false);
        input.name = "SFM-RenameEditor";

        // TODO: copy arrows from MenuOptionHorizontal and get them working somehow

        SfmUtil.RemoveComponent<EventTrigger>(input);
        SfmUtil.RemoveComponent<FixVerticalAlign>(input);
        SfmUtil.RemoveComponentImmediate<MenuOptionHorizontal>(input); // We must delete the Selectable immediately to add a new one.
        SfmUtil.RemoveComponent<MenuSetting>(input);
        UnityEngine.Object.Destroy(SfmUtil.GetChild(input, "Menu Option Label"));
        UnityEngine.Object.Destroy(SfmUtil.GetChild(input, "Description"));

        var text = SfmUtil.GetChildComponent<Text>(input, "Menu Option Text")!;
        text.gameObject.name = "SFM-RenameEditorText";
        SfmUtil.RemoveComponent<ChangeTextFontScaleOnHandHeld>(text.gameObject);
        SfmUtil.RemoveComponent<FixVerticalAlign>(text.gameObject);
        text.fontSize = saveSlotButton.locationText.fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.alignment = TextAnchor.MiddleRight;
        text.lineSpacing = 1f;

        var textInputField = input.AddComponent<InputField>();
        textInputField.textComponent = text;
        textInputField.caretColor = Color.white;
        textInputField.contentType = InputField.ContentType.Standard;
        textInputField.lineType = InputField.LineType.MultiLineSubmit;
        textInputField.caretWidth = 8;
        textInputField.text = "";
        textInputField.characterLimit = 32;

        var inputTransform = input.GetComponent<RectTransform>();
        inputTransform.anchorMin = actionRowRect.anchorMin;
        inputTransform.anchorMax = actionRowRect.anchorMax;
        inputTransform.pivot = actionRowRect.pivot;
        inputTransform.anchoredPosition = new Vector2(0f, actionRowRect.anchoredPosition.y);
        inputTransform.sizeDelta = new Vector2(slotRect.rect.width, actionRowRect.rect.height);

        RectTransform textTransform = text.gameObject.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0f, 0f);
        textTransform.anchorMax = new Vector2(1f, 1f);
        textTransform.anchoredPosition = new Vector2(0f, 0f);
        textTransform.sizeDelta = new Vector2(0f, 0f);

        return textInputField;
    }

}
