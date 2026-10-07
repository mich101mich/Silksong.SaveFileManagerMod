using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

/// <summary>
/// An InputField that allows controllers to at least submit and cancel input, so that they are not stuck in the field.
/// </summary>
public class ControllerFriendlyInputField : InputField, ICancelHandler
{
    public override void OnUpdateSelected(BaseEventData eventData)
    {
        base.OnUpdateSelected(eventData);

        if (!isFocused)
        {
            return;
        }

        var ih = GameManager.instance?.inputHandler;
        if (ih == null || !ih.acceptingInput)
        {
            return;
        }

        if (ih.inputActions.MenuSubmit.WasPressed)
        {
            ih.inputActions.MenuSubmit.ClearInputState();
            SendOnSubmit();
            DeactivateInputField();
        }
        else if (ih.inputActions.MenuCancel.WasPressed)
        {
            ih.inputActions.MenuCancel.ClearInputState();
            DeactivateInputField();
        }
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        if (isFocused)
        {
            SendOnSubmit();
            DeactivateInputField();
            return;
        }

        base.OnSubmit(eventData);
    }

    public virtual void OnCancel(BaseEventData eventData)
    {
        if (isFocused)
        {
            DeactivateInputField();
        }
    }
}
