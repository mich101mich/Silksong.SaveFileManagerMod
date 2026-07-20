using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class RenameEditor : IDisposable
{
    public SaveSlotButton m_saveSlotButton;
    public InputHandler m_inputHandler;

    public InputField m_renameInput;
    public Animator m_leftCursor;
    public Animator m_rightCursor;

    public bool IsOpen = false;

    Action m_onCloseCallback;

    public RenameEditor(SaveSlotButton button, Action onCloseCallback, Action<string> onValueChangedCallback)
    {
        m_saveSlotButton = button;
        m_inputHandler = GameManager.instance.inputHandler;

        m_onCloseCallback = onCloseCallback;

        m_renameInput = BuildTextInput(m_saveSlotButton, out m_leftCursor, out m_rightCursor);
        m_renameInput.onValueChanged.AddListener(value => onValueChangedCallback(value));
        m_renameInput.onSubmit.AddListener(_ => Close(saveChanges: true));
        m_renameInput.onEndEdit.AddListener(_ => Close(saveChanges: false));
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(m_renameInput.gameObject);
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

        // Block select-on-hover logic. If we don't do at least this much, the input would go away the moment the user moves their
        // mouse (or immediately if the rename button was clicked on with a mouse).
        // Setting this flag will allow the user to move their mouse around and click into the text box, but there won't be any automatic
        // selections. If the user clicks elsewhere, the onEndEdit event will fire and the rename editor will close without saving changes.
        // Only problem is that the user can still click on other buttons and trigger their actions, which is not ideal. I tried preventing
        // this, but I honestly can't figure out where the submit events for the other buttons are coming from.
        // It would be possible to simply set allowMouseInput to false, which would prevent this as well, but that also prevents
        // 1) clicking outside of the input field to close it, and 2) moving the cursor in the input field with the mouse.
        var ui = UIManager.instance;
        ui.inputModule.focusOnMouseHover = false;

        ui.StartCoroutine(FadeInAndSelect());

        m_leftCursor.ResetTrigger(MenuSelectable._hidePropId);
        m_leftCursor.SetTrigger(MenuSelectable._showPropId);
        m_rightCursor.ResetTrigger(MenuSelectable._hidePropId);
        m_rightCursor.SetTrigger(MenuSelectable._showPropId);
    }

    public void Close(bool saveChanges)
    {
        if (!IsOpen)
        {
            return;
        }

        var ui = UIManager.instance;
        if (saveChanges)
        {
            ui.uiAudioPlayer.PlaySubmit();

            SaveName.SetSlotName(m_saveSlotButton.SaveSlotIndex, m_renameInput.text);
        }
        else
        {
            ui.uiAudioPlayer.PlayCancel();
        }

        ui.StartCoroutine(FadeOut());
        m_leftCursor.ResetTrigger(MenuSelectable._showPropId);
        m_leftCursor.SetTrigger(MenuSelectable._hidePropId);
        m_rightCursor.ResetTrigger(MenuSelectable._showPropId);
        m_rightCursor.SetTrigger(MenuSelectable._hidePropId);

        IsOpen = false;
        ui.inputModule.focusOnMouseHover = true;
        m_onCloseCallback?.Invoke();
    }

    public IEnumerator FadeInAndSelect()
    {
        m_renameInput.gameObject.SetActive(true);

        var baseColor = m_renameInput.textComponent.color;
        m_renameInput.textComponent.color = baseColor with { a = 0f };

        var alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Time.unscaledDeltaTime * UIManager.instance.MENU_FADE_SPEED;
            m_renameInput.textComponent.color = baseColor with { a = alpha };
            yield return null;
        }
        m_renameInput.textComponent.color = baseColor with { a = 1f };

        EventSystem.current?.SetSelectedGameObject(m_renameInput.gameObject);
        m_renameInput.ActivateInputField();
    }

    public IEnumerator FadeOut()
    {
        var baseColor = m_renameInput.textComponent.color;
        var alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= Time.unscaledDeltaTime * UIManager.instance.MENU_FADE_SPEED;
            m_renameInput.textComponent.color = baseColor with { a = alpha };
            yield return null;
        }
        m_renameInput.textComponent.color = baseColor with { a = 0f };
        m_renameInput.gameObject.SetActive(false);
    }

    public static InputField BuildTextInput(SaveSlotButton saveSlotButton, out Animator leftCursor, out Animator rightCursor)
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

        SfmUtil.RemoveComponent<MenuSetting>(input);
        UnityEngine.Object.Destroy(SfmUtil.GetChild(input, "Menu Option Label"));
        UnityEngine.Object.Destroy(SfmUtil.GetChild(input, "Description"));

        var oldSelectable = input.GetComponent<MenuOptionHorizontal>();

        // reuse the left/right cursors from the old MenuSelectable
        leftCursor = oldSelectable.leftCursor;
        rightCursor = oldSelectable.rightCursor;

        UnityEngine.Object.DestroyImmediate(oldSelectable); // We must delete the Selectable immediately to add a new one.

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
