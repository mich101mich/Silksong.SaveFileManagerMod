using System;
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
    private readonly CanvasGroup m_clearButtonCanvasGroup;
    private readonly Navigation m_originalRestoreNav;
    private readonly Navigation m_originalClearNav;

    private readonly MenuButton m_renameButton;
    private readonly MenuButton m_archiveButton;

    private bool m_canRename;
    private bool m_canArchive;
    private bool m_customButtonsInteractable = true;
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

        m_clearButtonCanvasGroup = m_clearButton.GetComponent<CanvasGroup>()
            ?? throw new Exception("Could not find CanvasGroup on clear save button");

        m_originalClearNav = m_clearButton.navigation;

        m_renameButton = CloneIconButton("SFM-RenameIcon", onRename);
        m_archiveButton = CloneIconButton("SFM-ArchiveIcon", onArchive);

        m_renameButton.gameObject.SetActive(false);
        m_archiveButton.gameObject.SetActive(false);
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

    public void SetAvailability(bool canRename, bool canArchive)
    {
        m_canRename = canRename;
        m_canArchive = canArchive;

        m_renameButton.gameObject.SetActive(canRename);
        m_archiveButton.gameObject.SetActive(canArchive);

        SyncCanvasGroupState();
        ApplyInteractableState();
    }

    public void SetCustomButtonsInteractable(bool interactable)
    {
        m_customButtonsInteractable = interactable;
        ApplyInteractableState();
    }

    public void Refresh()
    {
        if (m_disposed)
        {
            return;
        }

        SyncCanvasGroupState();
        UpdateLayout();
        UpdateNavigation();
    }

    private void ApplyInteractableState()
    {
        bool interactable = m_customButtonsInteractable && m_clearButton.interactable;
        m_renameButton.interactable = m_canRename && interactable;
        m_archiveButton.interactable = m_canArchive && interactable;
    }

    private MenuButton CloneIconButton(string name, Action onSubmit)
    {
        GameObject template = m_slot.clearSaveButton.gameObject;
        GameObject root = UnityEngine.Object.Instantiate(template, template.transform.parent);
        root.name = name;

        MenuButton templateButton = root.GetComponent<MenuButton>()
            ?? throw new Exception($"Could not find MenuButton on {name}");

        MenuButton actionButton = ConvertToActionButton(root, templateButton);
        EnsureSubmitEvent(actionButton);
        actionButton.OnSubmitPressed.RemoveAllListeners();
        actionButton.OnSubmitPressed.AddListener(() => onSubmit());
        actionButton.buttonType = MenuButton.MenuButtonType.Activate;

        return actionButton;
    }

    private static MenuButton ConvertToActionButton(GameObject root, MenuButton original)
    {
        Navigation navigation = original.navigation;
        bool interactable = original.interactable;
        Selectable.Transition transition = original.transition;
        Graphic? targetGraphic = original.targetGraphic;
        ColorBlock colors = original.colors;
        SpriteState spriteState = original.spriteState;

        CancelAction cancelAction = original.cancelAction;
        bool playSubmitSound = original.playSubmitSound;
        VibrationDataAsset? menuSubmitVibration = original.menuSubmitVibration;
        VibrationDataAsset? menuCancelVibration = original.menuCancelVibration;

        UnityEngine.Object.DestroyImmediate(original);

        SfmActionIconButton replacement = root.AddComponent<SfmActionIconButton>();
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

        EnsureSubmitEvent(replacement);
        replacement.OnSubmitPressed.RemoveAllListeners();

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

    private void SyncCanvasGroupState()
    {
        ApplyCanvasGroupState(m_renameButton.gameObject);
        ApplyCanvasGroupState(m_archiveButton.gameObject);
    }

    private void ApplyCanvasGroupState(GameObject root)
    {
        if (!root.TryGetComponent(out CanvasGroup target))
        {
            return;
        }

        target.alpha = m_clearButtonCanvasGroup.alpha;
        target.interactable = m_clearButtonCanvasGroup.interactable;
        target.blocksRaycasts = m_clearButtonCanvasGroup.blocksRaycasts;
    }

    private void UpdateLayout()
    {
        List<RectTransform> rects = new();
        if (m_restoreButton?.transform is RectTransform restoreRect)
        {
            rects.Add(restoreRect);
        }
        if (m_renameButton?.transform is RectTransform renameRect)
        {
            rects.Add(renameRect);
        }
        if (m_archiveButton?.transform is RectTransform archiveRect)
        {
            rects.Add(archiveRect);
        }
        if (m_clearButton?.transform is RectTransform clearRect)
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

    private void UpdateNavigation()
    {
        List<Selectable> row = new();

        if (IsUsable(m_restoreButton))
        {
            row.Add(m_restoreButton);
        }
        if (m_canRename && IsUsable(m_renameButton))
        {
            row.Add(m_renameButton);
        }
        if (m_canArchive && IsUsable(m_archiveButton))
        {
            row.Add(m_archiveButton);
        }
        if (IsUsable(m_clearButton))
        {
            row.Add(m_clearButton);
        }

        if (row.Count == 0)
        {
            return;
        }

        for (int i = 0; i < row.Count; i++)
        {
            Navigation nav = row[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = m_slot;
            nav.selectOnDown = m_slot.backButton;
            nav.selectOnLeft = i > 0 ? row[i - 1] : m_originalRestoreNav.selectOnLeft;
            nav.selectOnRight = i + 1 < row.Count ? row[i + 1] : m_originalClearNav.selectOnRight;
            row[i].navigation = nav;
        }
    }

    private static bool IsUsable(Selectable? selectable)
    {
        return selectable != null
            && selectable.IsActive()
            && selectable.IsInteractable();
    }

    private sealed class SfmActionIconButton : MenuButton
    {
    }
}
