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
    private readonly CanvasGroup m_clearCanvasGroup;
    private readonly Selectable m_clearSelectable;
    private readonly Selectable? m_restoreSelectable;

    private readonly MenuButton m_renameButton;
    private readonly MenuButton m_archiveButton;

    private readonly GameObject m_renameRoot;
    private readonly GameObject m_archiveRoot;

    private bool m_canRename;
    private bool m_canArchive;
    private bool m_customButtonsInteractable = true;
    private bool m_disposed;

    public SaveSlotActionRow(SaveSlotButton slot, Action onRename, Action onArchive)
    {
        m_slot = slot;
        m_clearCanvasGroup = m_slot.clearSaveButton;

        m_clearSelectable = m_slot.clearSaveButton.GetComponent<Selectable>()
            ?? throw new Exception("Could not find clear save selectable");

        m_restoreSelectable = typeof(SaveSlotButton)
            .GetField("restoreSaveButton", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(m_slot) as Selectable;

        (m_renameRoot, m_renameButton) = CloneIconButton("SFM-RenameIcon", onRename);
        (m_archiveRoot, m_archiveButton) = CloneIconButton("SFM-ArchiveIcon", onArchive);

        m_renameRoot.SetActive(value: false);
        m_archiveRoot.SetActive(value: false);
    }

    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }

        m_disposed = true;

        if (m_renameRoot != null)
        {
            UnityEngine.Object.Destroy(m_renameRoot);
        }

        if (m_archiveRoot != null)
        {
            UnityEngine.Object.Destroy(m_archiveRoot);
        }
    }

    public void SetAvailability(bool canRename, bool canArchive)
    {
        m_canRename = canRename;
        m_canArchive = canArchive;

        m_renameRoot.SetActive(canRename);
        m_archiveRoot.SetActive(canArchive);

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
        bool interactable = m_customButtonsInteractable && m_clearCanvasGroup.interactable;
        m_renameButton.interactable = m_canRename && interactable;
        m_archiveButton.interactable = m_canArchive && interactable;
    }

    private (GameObject root, MenuButton button) CloneIconButton(string name, Action onSubmit)
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

        return (root, actionButton);
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
        ApplyCanvasGroupState(m_renameRoot, m_clearCanvasGroup);
        ApplyCanvasGroupState(m_archiveRoot, m_clearCanvasGroup);
    }

    private static void ApplyCanvasGroupState(GameObject root, CanvasGroup source)
    {
        if (!root.TryGetComponent(out CanvasGroup target))
        {
            return;
        }

        target.alpha = source.alpha;
        target.interactable = source.interactable;
        target.blocksRaycasts = source.blocksRaycasts;
    }

    private void UpdateLayout()
    {
        if (m_slot.clearSaveButton.transform is not RectTransform clearRect)
        {
            return;
        }

        float spacing = 120f;
        if (m_restoreSelectable?.transform is RectTransform restoreRect)
        {
            spacing = Mathf.Abs(clearRect.anchoredPosition.x - restoreRect.anchoredPosition.x);
            if (spacing < 32f)
            {
                spacing = 120f;
            }
        }

        Vector2 basePos = clearRect.anchoredPosition;

        if (m_renameRoot.transform is RectTransform renameRect)
        {
            renameRect.anchorMin = clearRect.anchorMin;
            renameRect.anchorMax = clearRect.anchorMax;
            renameRect.pivot = clearRect.pivot;
            renameRect.anchoredPosition = new Vector2(basePos.x + spacing, basePos.y);
        }

        if (m_archiveRoot.transform is RectTransform archiveRect)
        {
            archiveRect.anchorMin = clearRect.anchorMin;
            archiveRect.anchorMax = clearRect.anchorMax;
            archiveRect.pivot = clearRect.pivot;
            archiveRect.anchoredPosition = new Vector2(basePos.x + (spacing * 2f), basePos.y);
        }
    }

    private void UpdateNavigation()
    {
        List<Selectable> row = new();

        if (IsUsable(m_restoreSelectable))
        {
            row.Add(m_restoreSelectable!);
        }

        if (IsUsable(m_clearSelectable))
        {
            row.Add(m_clearSelectable);
        }

        if (m_canRename && IsUsable(m_renameButton))
        {
            row.Add(m_renameButton);
        }

        if (m_canArchive && IsUsable(m_archiveButton))
        {
            row.Add(m_archiveButton);
        }

        if (row.Count == 0)
        {
            return;
        }

        for (int i = 0; i < row.Count; i++)
        {
            Selectable current = row[i];
            Selectable left = i > 0 ? row[i - 1] : current;
            Selectable right = i < row.Count - 1 ? row[i + 1] : current;

            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = m_slot;
            nav.selectOnDown = m_slot.backButton;
            nav.selectOnLeft = left;
            nav.selectOnRight = right;
            current.navigation = nav;
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
