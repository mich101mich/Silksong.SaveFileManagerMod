using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GlobalEnums;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public sealed class SaveSlotActionRow : IDisposable
{
    private readonly SaveSlotButton m_slot;
    private readonly RestoreSaveButton m_restoreButton;
    private readonly ClearSaveButton m_clearButton;
    private readonly Navigation m_originalRestoreNav;
    private readonly Navigation m_originalClearNav;

    private readonly SaveSlotActionButton m_renameButton;
    private readonly SaveSlotActionButton m_archiveButton;
    private readonly CanvasGroup m_renameButtonCanvasGroup;
    private readonly CanvasGroup m_archiveButtonCanvasGroup;

    private bool m_canRestore;
    private bool m_canRename;
    private bool m_canArchive;
    private bool m_canClear;

    private bool m_disposed;

    public SaveSlotActionRow(SaveSlotButton slot, Action onRename, Action onArchive)
    {
        m_slot = slot;

        m_restoreButton = typeof(SaveSlotButton)
            .GetField("restoreSaveButton", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(m_slot) as RestoreSaveButton
            ?? throw new Exception("Could not find restore save button component");

        m_originalRestoreNav = m_restoreButton.navigation;

        m_clearButton = m_slot.clearSaveButton.GetComponent<ClearSaveButton>()
            ?? throw new Exception("Could not find clear save button component");

        m_originalClearNav = m_clearButton.navigation;

        m_renameButton = CloneIconButton("SFM-RenameIcon", onRename);
        m_archiveButton = CloneIconButton("SFM-ArchiveIcon", onArchive);

        m_renameButtonCanvasGroup = m_renameButton.gameObject.GetComponent<CanvasGroup>()!;
        m_archiveButtonCanvasGroup = m_archiveButton.gameObject.GetComponent<CanvasGroup>()!;

        m_renameButton.gameObject.SetActive(true);
        m_archiveButton.gameObject.SetActive(true);
    }

    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }

        m_disposed = true;

        if (m_renameButton != null)
        {
            UnityEngine.Object.Destroy(m_renameButton.gameObject);
        }

        if (m_archiveButton != null)
        {
            UnityEngine.Object.Destroy(m_archiveButton.gameObject);
        }

        if (m_restoreButton != null)
        {
            m_restoreButton.navigation = m_originalRestoreNav;
            m_restoreButton.transform.SetLocalPositionX(-67f);
        }
        if (m_clearButton != null)
        {
            m_clearButton.navigation = m_originalClearNav;
            m_clearButton.transform.SetLocalPositionX(67f);
        }
    }

    public void Refresh()
    {
        if (m_disposed)
        {
            return;
        }

        // SfmLogger.LogInfo($"Updating button states for {m_slot.name}. Current slot state: {m_slot.State}");

        // See SaveSlotButton.AnimateToSlotState() for reference
        bool isBlocked = false;
        bool isEmpty = false;
        bool isDefeated = false;
        switch (m_slot.State)
        {
            case SaveSlotButton.SlotState.Hidden:
            case SaveSlotButton.SlotState.OperationInProgress:
            case SaveSlotButton.SlotState.RestoreSave: // difference: buttons were hidden elsewhere in RestoreSave state
                isBlocked = true;
                break;
            case SaveSlotButton.SlotState.EmptySlot:
            case SaveSlotButton.SlotState.BlackThreadInfected:
                isEmpty = true;
                break;
            case SaveSlotButton.SlotState.Defeated:
                isDefeated = true;
                break;
            case SaveSlotButton.SlotState.SavePresent:
            case SaveSlotButton.SlotState.Corrupted:
            case SaveSlotButton.SlotState.Incompatible:
                // fully available
                break;
        }

        m_canRestore = !isBlocked && !isEmpty && !isDefeated;
        m_canRename = !isBlocked && !isEmpty;
        m_canArchive = !isBlocked;
        m_canClear = !isBlocked && !isEmpty;

        UIManager ui = UIManager.instance;
        if (m_canRename && m_renameButton.interactable == false)
        {
            ui.StartCoroutine(FadeInCanvasGroupAfterDelay(0.1f, m_renameButtonCanvasGroup));
        }
        else if (!m_canRename && m_renameButton.interactable)
        {
            ui.StartCoroutine(FadeOutCanvasGroupAfterDelay(0.1f, m_renameButtonCanvasGroup));
        }

        if (m_canArchive && m_archiveButton.interactable == false)
        {
            ui.StartCoroutine(FadeInCanvasGroupAfterDelay(0.1f, m_archiveButtonCanvasGroup));
        }
        else if (!m_canArchive && m_archiveButton.interactable)
        {
            ui.StartCoroutine(FadeOutCanvasGroupAfterDelay(0.1f, m_archiveButtonCanvasGroup));
        }

        m_renameButton.interactable = m_canRename;
        m_archiveButton.interactable = m_canArchive;

        m_renameButtonCanvasGroup.interactable = m_canRename;
        m_archiveButtonCanvasGroup.interactable = m_canArchive;

        // m_renameButtonCanvasGroup.alpha = m_canRename ? 1f : 0f;
        // m_archiveButtonCanvasGroup.alpha = m_canArchive ? 1f : 0f;

        UpdateLayout();
    }

    private SaveSlotActionButton CloneIconButton(string name, Action onSubmit)
    {
        GameObject template = m_slot.clearSaveButton.gameObject;
        GameObject root = UnityEngine.Object.Instantiate(template, template.transform.parent);
        root.name = name;

        // Note that a GameObject can only have one Selectable component, so we need to delete the ClearSaveButton
        // before adding the SaveSlotActionButton. This means we have to manually store and copy all relevant properties.
        ClearSaveButton original = root.GetComponent<ClearSaveButton>()
            ?? throw new Exception($"Could not find MenuButton on {root.name}");

        var navigation = original.navigation;
        var interactable = original.interactable;
        var transition = original.transition;
        var targetGraphic = original.targetGraphic;
        var colors = original.colors;
        var spriteState = original.spriteState;

        var cancelAction = original.cancelAction;
        var playSubmitSound = original.playSubmitSound;
        var menuSubmitVibration = original.menuSubmitVibration;
        var menuCancelVibration = original.menuCancelVibration;

        var leftCursor = original.leftCursor;
        var rightCursor = original.rightCursor;

        var selectIcon = typeof(ClearSaveButton)
            .GetField("selectIcon", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(original) as Animator
            ?? throw new Exception($"Could not find select icon on {original.name}");

        UnityEngine.Object.DestroyImmediate(original);

        SaveSlotActionButton replacement = root.AddComponent<SaveSlotActionButton>();
        replacement.navigation = navigation;
        replacement.interactable = interactable;
        replacement.transition = transition;
        replacement.targetGraphic = targetGraphic;
        replacement.colors = colors;
        replacement.spriteState = spriteState;

        replacement.cancelAction = cancelAction;
        replacement.playSubmitSound = playSubmitSound;
        replacement.menuSubmitVibration = menuSubmitVibration;
        replacement.menuCancelVibration = menuCancelVibration;

        replacement.leftCursor = leftCursor;
        replacement.rightCursor = rightCursor;
        replacement.selectIcon = selectIcon;

        // unique properties
        replacement.onSubmit = onSubmit;
        replacement.buttonType = MenuButton.MenuButtonType.Activate;

        var toRemove = replacement.GetComponent<ZeroAlphaOnStart>();
        if (toRemove != null)
        {
            UnityEngine.Object.Destroy(toRemove);
        }

        return replacement;
    }

    private static void EnsureSubmitEvent(MenuButton button)
    {
        if (button.OnSubmitPressed != null)
        {
            return;
        }

        button.OnSubmitPressed = new UnityEvent();
    }

    private void UpdateLayout()
    {
        List<RectTransform> rects = new();
        if (m_canRestore && m_restoreButton.transform is RectTransform restoreRect)
        {
            rects.Add(restoreRect);
        }
        if (m_canRename && m_renameButton.transform is RectTransform renameRect)
        {
            rects.Add(renameRect);
        }
        if (m_canArchive && m_archiveButton.transform is RectTransform archiveRect)
        {
            rects.Add(archiveRect);
        }
        if (m_canClear && m_clearButton.transform is RectTransform clearRect)
        {
            rects.Add(clearRect);
        }

        float spacing = 134f;
        float totalWidth = (rects.Count - 1) * spacing;
        float x = -totalWidth / 2f;

        foreach (RectTransform rect in rects)
        {
            rect.SetLocalPositionX(x);
            x += spacing;
        }
    }

    public void SetupNavigation(SaveSlotActionRow? previousRow, SaveSlotActionRow? nextRow)
    {
        List<Selectable> row = new()
        {
            m_restoreButton,
            m_renameButton,
            m_archiveButton,
            m_clearButton,
        };
        List<Selectable?> neighbors = new()
        {
            previousRow?.m_clearButton,
            m_restoreButton,
            m_renameButton,
            m_archiveButton,
            m_clearButton,
            nextRow?.m_restoreButton
        };

        for (int i = 0; i < 4; i++)
        {
            Navigation nav = row[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = m_slot;
            nav.selectOnDown = m_slot.backButton;
            nav.selectOnLeft = neighbors[1 + (i - 1)];
            nav.selectOnRight = neighbors[1 + (i + 1)];
            row[i].navigation = nav;
        }
        foreach (var field in new[] { "noNav", "fullSlotNav", "emptySlotNav", "defeatedSlotNav" })
        {
            var fieldInfo = typeof(SaveSlotButton)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!;

            var nav = (Navigation)fieldInfo.GetValue(m_slot)!;
        }
    }

    private static bool IsUsable(Selectable? selectable)
    {
        return selectable != null
            && selectable.IsActive()
            && selectable.IsInteractable();
    }

    private IEnumerator FadeInCanvasGroupAfterDelay(float delay, CanvasGroup cg)
    {
        yield return new WaitForSeconds(delay);
        yield return UIManager.instance.FadeInCanvasGroup(cg);
    }
    private IEnumerator FadeOutCanvasGroupAfterDelay(float delay, CanvasGroup cg)
    {
        yield return new WaitForSeconds(delay);
        yield return UIManager.instance.FadeOutCanvasGroup(cg);
    }

}

public static class MyUnityExtensions
{
    public static GameObject? FindChild(this GameObject obj, string path)
    {
        Transform transform = obj.transform.Find(path);
        return transform != null ? transform.gameObject : null;
    }
}