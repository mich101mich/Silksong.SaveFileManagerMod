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

public sealed class SaveSlotActionRow : IDisposable
{
    public readonly SaveSlotButton m_slot;
    public readonly Navigation m_originalRestoreNav;
    public readonly Navigation m_originalClearNav;

    public readonly ActionButtonWrapper m_restore;
    public readonly ActionButtonWrapper m_rename;
    public readonly ActionButtonWrapper m_archive;
    public readonly ActionButtonWrapper m_clear;
    public readonly List<ActionButtonWrapper> m_buttons;

    public bool m_disposed;

    public SaveSlotActionRow(SaveSlotButton slot, Action onRename, Action onArchive)
    {
        m_slot = slot;

        var restoreButton = typeof(SaveSlotButton)
            .GetField("restoreSaveButton", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(m_slot) as RestoreSaveButton
            ?? throw new Exception("Could not find restore save button component");

        m_originalRestoreNav = restoreButton.navigation;

        var clearButton = m_slot.clearSaveButton.GetComponent<ClearSaveButton>()
            ?? throw new Exception("Could not find clear save button component");

        m_originalClearNav = clearButton.navigation;

        m_restore = new ActionButtonWrapper(restoreButton, isOriginal: true);
        m_rename = new ActionButtonWrapper(CloneIconButton("SFM-RenameButton", onRename), isOriginal: false);
        m_archive = new ActionButtonWrapper(CloneIconButton("SFM-ArchiveButton", onArchive), isOriginal: false);
        m_clear = new ActionButtonWrapper(clearButton, isOriginal: true);

        m_buttons = new List<ActionButtonWrapper> { m_restore, m_rename, m_archive, m_clear };
    }

    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }

        m_disposed = true;

        UnityEngine.Object.Destroy(m_rename.button.gameObject);
        UnityEngine.Object.Destroy(m_archive.button.gameObject);

        m_restore.button.navigation = m_originalRestoreNav;
        m_restore.button.transform.SetLocalPositionX(-67f);
        m_clear.button.navigation = m_originalClearNav;
        m_clear.button.transform.SetLocalPositionX(67f);
    }

    public void Refresh()
    {
        if (m_disposed)
        {
            return;
        }

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

        m_restore.SetUsable(!isBlocked && !isEmpty && !isDefeated);
        m_rename.SetUsable(!isBlocked && !isEmpty);
        m_archive.SetUsable(!isBlocked);
        m_clear.SetUsable(!isBlocked && !isEmpty);

        UpdateLayout();
    }

    public void SetButtonVisibility(bool visible, ActionButtonWrapper? focusedElement = null)
    {
        foreach (ActionButtonWrapper button in m_buttons)
        {
            button.SetVisibility(visible, focus: button == focusedElement);
        }
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
        List<RectTransform> rects = m_buttons.Where(b => b.canUse).Select(b => b.rectTransform).ToList();

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
        for (int i = 0; i < m_buttons.Count; i++)
        {
            Navigation nav = m_buttons[i].button.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = m_slot;
            nav.selectOnDown = m_slot.backButton;
            nav.selectOnLeft = i > 0 ? m_buttons[i - 1].button : previousRow?.m_clear.button;
            nav.selectOnRight = i < m_buttons.Count - 1 ? m_buttons[i + 1].button : nextRow?.m_restore.button;
            m_buttons[i].button.navigation = nav;
        }

        // Update the various navigation fields on SaveSlotButton to point to the archive button.
        // By default, empty goes to the back button, and defeated to the clear button.
        // We point all to the archive button since that is the most available action.
        foreach (var field in new[] { "fullSlotNav", "emptySlotNav", "defeatedSlotNav" })
        {
            var fieldInfo = typeof(SaveSlotButton)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!;

            var nav = (Navigation)fieldInfo.GetValue(m_slot)!;
            nav.selectOnDown = m_archive.button;
            fieldInfo.SetValue(m_slot, nav);
        }
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