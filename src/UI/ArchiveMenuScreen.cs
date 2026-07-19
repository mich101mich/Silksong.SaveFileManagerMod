using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

namespace SaveFileManagerMod.UI;

public sealed class ArchiveMenuScreen : IDisposable
{
    public GameObject m_container;
    public MenuScreen m_menuScreen;
    public ScrollRect m_scrollRect;
    public RectTransform m_content;
    public RectTransform m_viewport;
    public MenuButton m_backButton;
    public List<ArchiveMenuEntry> m_entries = new List<ArchiveMenuEntry>();
    public ArchiveMenuScreenDriver m_driver;
    public bool m_disposed;

    public ArchiveMenuScreen(string title, string statusMessage, Action onGoBack, out Text statusText)
    {
        var canvas = SfmUtil.GetChild(UIManager.instance.gameObject, "UICanvas")!;
        var optionsScreen = SfmUtil.GetChild(canvas, "OptionsMenuScreen")!;

        m_container = UnityEngine.Object.Instantiate(optionsScreen, canvas.transform, false);
        m_container.name = "SFM-ArchiveSlotSelectionMenu";
        m_container.SetActive(false);

        SfmUtil.RemoveComponentImmediate<MenuButtonList>(m_container);

        var oldContent = SfmUtil.GetChild(m_container, "Content")!;
        UnityEngine.Object.DestroyImmediate(oldContent);

        var titleText = SfmUtil.GetChildComponent<Text>(m_container, "Title")!;
        SfmUtil.RemoveComponent<AutoLocalizeTextUI>(titleText.gameObject);
        titleText.text = title;

        m_menuScreen = m_container.GetComponent<MenuScreen>()!;
        m_backButton = SfmUtil.GetChildComponent<MenuButton>(m_container, "Controls/ApplyButton")!;
        SfmUtil.RemoveComponent<EventTrigger>(m_backButton.gameObject);
        m_backButton.OnSubmitPressed = new UnityEvent();
        m_backButton.OnSubmitPressed.AddListener(() => onGoBack());
        m_menuScreen.backButton = m_backButton;

        var scrollPane = new GameObject("SFM-ArchiveScrollPane")
        {
            layer = m_container.layer
        };

        var scrollTransform = scrollPane.AddComponent<RectTransform>()!;
        scrollTransform.SetParent(m_container.transform, false);
        scrollTransform.anchorMin = new Vector2(0.5f, 1f);
        scrollTransform.anchorMax = new Vector2(0.5f, 1f);
        scrollTransform.pivot = new Vector2(0.5f, 1f);
        scrollTransform.anchoredPosition = new Vector2(0f, -330f);
        scrollTransform.sizeDelta = new Vector2(1510f, Mathf.Ceil(105f * 8.334f));

        var viewport = new GameObject("Viewport")
        {
            layer = m_container.layer
        };

        m_viewport = viewport.AddComponent<RectTransform>()!;
        m_viewport.SetParent(scrollTransform, false);
        FitToParent(m_viewport);

        var viewportImage = viewport.AddComponent<Image>()!;
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content")
        {
            layer = m_container.layer
        };

        m_content = content.AddComponent<RectTransform>()!;
        m_content.SetParent(m_viewport, false);
        m_content.anchorMin = new Vector2(0.5f, 1f);
        m_content.anchorMax = new Vector2(0.5f, 1f);
        m_content.pivot = new Vector2(0.5f, 1f);
        m_content.anchoredPosition = Vector2.zero;
        m_content.sizeDelta = new Vector2(ArchiveMenuEntry.SLOT_WIDTH, 0f);

        m_scrollRect = scrollPane.AddComponent<ScrollRect>()!;
        m_scrollRect.horizontal = false;
        m_scrollRect.vertical = true;
        m_scrollRect.movementType = ScrollRect.MovementType.Clamped;
        m_scrollRect.scrollSensitivity = 80f;
        m_scrollRect.viewport = m_viewport;
        m_scrollRect.content = m_content;
        m_scrollRect.verticalNormalizedPosition = 1f;

        m_driver = m_container.AddComponent<ArchiveMenuScreenDriver>();
        m_driver.Initialize(this);

        statusText = CreateStatusLabel(statusMessage);
    }

    public Text CreateStatusLabel(string text)
    {
        var source = SfmUtil.GetChild(UIManager.instance.gameObject, "UICanvas/OptionsMenuScreen/Content/GameOptions/GameOptionsButton/Menu Button Text")!;
        var label = UnityEngine.Object.Instantiate(source, m_content, false);
        label.name = "SFM-StatusLabel";
        label.SetActive(true);

        SfmUtil.RemoveComponent<AutoLocalizeTextUI>(label);
        SfmUtil.RemoveComponent<ChangeTextFontScaleOnHandHeld>(label);
        SfmUtil.RemoveComponent<FixVerticalAlign>(label);

        var statusText = label.GetComponent<Text>()!;
        statusText.raycastTarget = false;
        statusText.lineSpacing = 1f;
        statusText.text = text;

        var transform = label.GetComponent<RectTransform>()!;
        transform.anchorMin = new Vector2(0.5f, 1f);
        transform.anchorMax = new Vector2(0.5f, 1f);
        transform.pivot = new Vector2(0.5f, 1f);
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = new Vector2(ArchiveMenuEntry.SLOT_WIDTH, ArchiveMenuEntry.SLOT_TOTAL_HEIGHT);

        UpdateLayout();
        return statusText;
    }

    public void Add(ArchiveMenuEntry entry)
    {
        if (m_disposed)
        {
            entry.Dispose();
            return;
        }

        m_entries.Add(entry);
        entry.Container.transform.SetParent(m_content, false);
        entry.Container.SetActive(true);
        UpdateLayout();
    }

    public IEnumerator Show()
    {
        UpdateLayout();
        EventSystem.current?.SetSelectedGameObject(null);
        m_container.SetActive(true);

        yield return UIManager.instance.ShowMenu(m_menuScreen);
    }

    public IEnumerator Hide()
    {
        yield return UIManager.instance.HideMenu(m_menuScreen);
        m_container.SetActive(false);
    }

    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }

        m_disposed = true;
        m_backButton.OnSubmitPressed.RemoveAllListeners();
        UnityEngine.Object.Destroy(m_container);
    }

    internal void KeepSelectionVisible()
    {
        var selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null)
        {
            return;
        }

        int selectedIndex = m_entries.FindIndex(entry => selected.transform.IsChildOf(entry.Container.transform));
        if (selectedIndex < 0)
        {
            return;
        }

        float rowTop = -(selectedIndex + 1) * ArchiveMenuEntry.SLOT_TOTAL_HEIGHT;
        float rowBottom = rowTop - ArchiveMenuEntry.SLOT_TOTAL_HEIGHT;
        float visibleTop = -m_content.anchoredPosition.y;
        float visibleBottom = visibleTop - m_viewport.rect.height;
        float offset = m_content.anchoredPosition.y;

        if (rowTop > visibleTop)
        {
            offset = -rowTop;
        }
        else if (rowBottom < visibleBottom)
        {
            offset = -(rowBottom + m_viewport.rect.height);
        }

        float maxOffset = Mathf.Max(0f, m_content.rect.height - m_viewport.rect.height);
        offset = Mathf.Clamp(offset, 0f, maxOffset);
        if (!Mathf.Approximately(offset, m_content.anchoredPosition.y))
        {
            m_content.anchoredPosition = new Vector2(m_content.anchoredPosition.x, offset);
        }
    }

    public void UpdateLayout()
    {
        for (int i = 0; i < m_entries.Count; i++)
        {
            var transform = m_entries[i].Container.GetComponent<RectTransform>()!;
            transform.anchorMin = new Vector2(0.5f, 1f);
            transform.anchorMax = new Vector2(0.5f, 1f);
            transform.pivot = new Vector2(0.5f, 1f);
            transform.anchoredPosition = new Vector2(0f, -(i + 1) * ArchiveMenuEntry.SLOT_TOTAL_HEIGHT);
        }

        float rowCount = m_entries.Count + 1; // +1 for the status label
        m_content.sizeDelta = new Vector2(ArchiveMenuEntry.SLOT_WIDTH, rowCount * ArchiveMenuEntry.SLOT_TOTAL_HEIGHT);

        RebuildNavigation();
        Canvas.ForceUpdateCanvases();
    }

    public void RebuildNavigation()
    {
        if (m_entries.Count == 0)
        {
            m_menuScreen.defaultHighlight = m_backButton;
            var onlyBack = m_backButton.navigation;
            onlyBack.mode = Navigation.Mode.Explicit;
            onlyBack.selectOnUp = m_backButton;
            onlyBack.selectOnDown = m_backButton;
            m_backButton.navigation = onlyBack;
            return;
        }

        for (int i = 0; i < m_entries.Count; i++)
        {
            var button = m_entries[i].MenuButton;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = i == 0 ? m_backButton : m_entries[i - 1].MenuButton;
            navigation.selectOnDown = i == m_entries.Count - 1 ? m_backButton : m_entries[i + 1].MenuButton;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            button.navigation = navigation;
        }

        var backNavigation = m_backButton.navigation;
        backNavigation.mode = Navigation.Mode.Explicit;
        backNavigation.selectOnUp = m_entries[m_entries.Count - 1].MenuButton;
        backNavigation.selectOnDown = m_entries[0].MenuButton;
        m_backButton.navigation = backNavigation;
        m_menuScreen.defaultHighlight = m_entries[0].MenuButton;
    }

    public static void FitToParent(RectTransform transform)
    {
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.pivot = new Vector2(0.5f, 0.5f);
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
    }
}

public sealed class ArchiveMenuScreenDriver : MonoBehaviour
{
    public ArchiveMenuScreen? m_screen;

    public void Initialize(ArchiveMenuScreen screen)
    {
        m_screen = screen;
    }

    public void LateUpdate()
    {
        m_screen?.KeepSelectionVisible();
    }
}