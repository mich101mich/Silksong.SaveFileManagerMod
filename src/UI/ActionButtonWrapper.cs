using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class ActionButtonWrapper(MenuButton button, bool isOriginal)
{
    public readonly MenuButton m_button = button;
    public readonly CanvasGroup m_canvasGroup = button.GetComponent<CanvasGroup>();
    public readonly RectTransform m_rectTransform = button.GetComponent<RectTransform>();
    public readonly bool m_isOriginal = isOriginal; // Whether this button is an original game button (Restore/Clear) or one added by the mod (Rename/Archive).
    public bool m_canUse = false;
    public Coroutine? m_currentCoroutine = null;

    public bool IsUsable()
    {
        return m_button != null
            && m_button.IsActive()
            && m_button.IsInteractable();
    }

    public void SetUsable(bool usable)
    {
        if (m_canUse == usable)
        {
            return;
        }
        m_canUse = usable;

        if (m_isOriginal)
        {
            // Original buttons are faded in/out by the game, so we don't want to interfere with that.
            return;
        }

        var ui = UIManager.instance;
        if (m_currentCoroutine != null)
        {
            ui.StopCoroutine(m_currentCoroutine);
            m_currentCoroutine = null;
        }

        m_button.interactable = usable;
        m_canvasGroup.interactable = usable;
        if (usable)
        {
            m_currentCoroutine = ui.StartCoroutine(FadeInAfterDelay(0.1f));
        }
        else
        {
            m_currentCoroutine = ui.StartCoroutine(FadeOutAfterDelay(0.1f));
        }
    }

    public void SetVisibility(bool visible, bool focus = false)
    {
        var ui = UIManager.instance;

        if (m_currentCoroutine != null)
        {
            ui.StopCoroutine(m_currentCoroutine);
            m_currentCoroutine = null;
        }

        if (visible)
        {
            if (!m_canUse)
            {
                return;
            }
            if (focus)
            {
                m_currentCoroutine = ui.StartCoroutine(FadeInAndFocus());
            }
            else
            {
                m_currentCoroutine = ui.StartCoroutine(ui.FadeInCanvasGroup(m_canvasGroup));
            }
        }
        else
        {
            m_currentCoroutine = ui.StartCoroutine(ui.FadeOutCanvasGroup(m_canvasGroup));
        }
    }

    public IEnumerator FadeInAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return UIManager.instance.FadeInCanvasGroup(m_canvasGroup);
        m_canvasGroup.blocksRaycasts = true;
    }
    public IEnumerator FadeOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_canvasGroup.blocksRaycasts = false;
        yield return UIManager.instance.FadeOutCanvasGroup(m_canvasGroup);
    }
    public IEnumerator FadeInAndFocus()
    {
        yield return UIManager.instance.FadeInCanvasGroup(m_canvasGroup);
        m_canvasGroup.blocksRaycasts = true;
        EventSystem.current?.SetSelectedGameObject(m_button.gameObject);
    }
}
