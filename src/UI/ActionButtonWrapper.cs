using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class ActionButtonWrapper
{
    public readonly MenuButton button;
    public readonly CanvasGroup canvasGroup;
    public readonly RectTransform rectTransform;
    public readonly bool isOriginal; // Whether this button is an original game button (Restore/Clear) or one added by the mod (Rename/Archive).
    public bool canUse;

    public ActionButtonWrapper(MenuButton button, bool isOriginal)
    {
        this.button = button;
        this.canvasGroup = button.GetComponent<CanvasGroup>()!;
        this.rectTransform = button.GetComponent<RectTransform>()!;
        this.isOriginal = isOriginal;
        this.canUse = false;
    }

    public bool IsUsable()
    {
        return button != null
            && button.IsActive()
            && button.IsInteractable();
    }

    public void SetUsable(bool usable)
    {
        if (canUse == usable)
        {
            return;
        }
        canUse = usable;

        if (isOriginal)
        {
            // Original buttons are faded in/out by the game, so we don't want to interfere with that.
            return;
        }

        button.interactable = usable;
        canvasGroup.interactable = usable;
        if (usable)
        {
            UIManager.instance.StartCoroutine(FadeInAfterDelay(0.1f));
        }
        else
        {
            UIManager.instance.StartCoroutine(FadeOutAfterDelay(0.1f));
        }
    }

    public void SetVisibility(bool visible, bool focus = false)
    {
        var ui = UIManager.instance;
        if (visible)
        {
            if (!canUse)
            {
                return;
            }
            if (focus)
            {
                ui.StartCoroutine(FadeInAndFocus());
            }
            else
            {
                ui.StartCoroutine(ui.FadeInCanvasGroup(canvasGroup));
            }
        }
        else
        {
            ui.StartCoroutine(ui.FadeOutCanvasGroup(canvasGroup));
        }
    }

    public IEnumerator FadeInAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return UIManager.instance.FadeInCanvasGroup(canvasGroup);
        canvasGroup.blocksRaycasts = true;
    }
    public IEnumerator FadeOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canvasGroup.blocksRaycasts = false;
        yield return UIManager.instance.FadeOutCanvasGroup(canvasGroup);
    }
    public IEnumerator FadeInAndFocus()
    {
        yield return UIManager.instance.FadeInCanvasGroup(canvasGroup);
        canvasGroup.blocksRaycasts = true;
        EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }
}
