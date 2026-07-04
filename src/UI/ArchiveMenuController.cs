using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Screens;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public sealed class ArchiveMenuController : MonoBehaviour
{
    public ScrollingMenuScreen? m_screen;

    public static readonly MethodInfo? s_invokeOnShow = typeof(AbstractMenuScreen)
        .GetMethod("InvokeOnShow", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly MethodInfo? s_invokeOnHide = typeof(AbstractMenuScreen)
        .GetMethod("InvokeOnHide", BindingFlags.Instance | BindingFlags.NonPublic);

    public int m_selectedSlotIndex = -1;
    public string? m_slotSummary = null;

    public bool m_isOpen = false;
    public bool m_isTransitioning = false;
    public bool m_shouldClose = false;

    public bool IsOpen => m_isOpen || m_isTransitioning;

    public void OnDestroy()
    {
        if (m_screen != null)
        {
            m_screen.Dispose();
            m_screen = null;
        }

        m_isOpen = false;
        m_isTransitioning = false;
    }

    public void OpenForSlot(SaveSlotButton slot)
    {
        if (IsOpen)
        {
            return;
        }

        m_selectedSlotIndex = slot.SaveSlotIndex;

        m_slotSummary = slot.saveFileState == SaveSlotButton.SaveFileStates.Empty
            ? $"Empty slot {slot.SaveSlotIndex}"
            : $"Select target for Slot {slot.SaveSlotIndex}";

        StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        SfmLogger.LogInfo("Closing ArchiveMenuController. IsOpen: " + m_isOpen + ", IsTransitioning: " + m_isTransitioning + ", ShouldClose: " + m_shouldClose);
        if (m_isOpen)
        {
            if (m_isTransitioning)
            {
                // already in the close routine
            }
            else
            {
                StartCoroutine(CloseRoutine());
            }
        }
        else if (m_isTransitioning)
        {
            // in the open routine
            m_shouldClose = true;
        }
    }

    public void Update()
    {
        if (!m_isOpen && !m_isTransitioning)
        {
            return;
        }

        InputHandler inputHandler = GameManager.instance.inputHandler;
        if (inputHandler.acceptingInput && inputHandler.inputActions.MenuCancel.WasPressed)
        {
            inputHandler.inputActions.MenuCancel.ClearInputState();
            UIManager.instance.uiAudioPlayer.PlayCancel();
            Close();
        }
    }

    public IEnumerator OpenRoutine()
    {
        m_isTransitioning = true;
        m_shouldClose = false;

        m_screen?.Dispose();

        m_screen = new("Save Archive");
        m_screen.AllowGoBack = false;
        m_screen.OnGoBack += Close;

        m_screen.Add(new TextLabel(m_slotSummary!));

        // Add the first entries immediately so that there is something to select
        for (int i = 1; i <= 4; i++)
        {
            if (i == m_selectedSlotIndex)
            {
                continue;
            }

            m_screen.Add(new ArchiveMenuEntry(i));
        }

        UIManager ui = UIManager.instance;
        if (ui.menuState == GlobalEnums.MainMenuState.SAVE_PROFILES)
        {
            // ui.uiAudioPlayer.PlayOpenProfileSelect();
            yield return ui.StartCoroutine(ui.HideSaveProfileMenu(updateBlackThread: true));
        }

        InvokeScreenOnShow(m_screen);

        yield return ui.StartCoroutine(ui.ShowMenu(m_screen.MenuScreen));

        for (int i = 5; i <= 50; i++)
        {
            if (i == m_selectedSlotIndex)
            {
                continue;
            }

            m_screen.Add(new ArchiveMenuEntry(i));

            yield return null; // slight delay to avoid freezing the game
            yield return null;

            if (m_shouldClose)
            {
                yield return StartCoroutine(CloseRoutine());
                yield break;
            }
        }

        m_isOpen = true;
        m_isTransitioning = false;
    }

    public IEnumerator CloseRoutine()
    {
        m_isTransitioning = true;

        UIManager ui = UIManager.instance;

        if (m_screen != null)
        {
            yield return ui.StartCoroutine(ui.HideMenu(m_screen.MenuScreen));

            InvokeScreenOnHide(m_screen);

            m_screen.Dispose();
            m_screen = null;
        }
        m_isOpen = false;

        yield return ui.StartCoroutine(ui.GoToProfileMenu());

        m_isTransitioning = false;
    }

    public static void InvokeScreenOnShow(AbstractMenuScreen screen)
    {
        if (s_invokeOnShow == null)
        {
            return;
        }

        s_invokeOnShow.Invoke(screen, new object[] { MenuScreenNavigation.NavigationType.Forwards });
    }

    public static void InvokeScreenOnHide(AbstractMenuScreen screen)
    {
        if (s_invokeOnHide == null)
        {
            return;
        }

        s_invokeOnHide.Invoke(screen, new object[] { MenuScreenNavigation.NavigationType.Backwards });
    }
}
