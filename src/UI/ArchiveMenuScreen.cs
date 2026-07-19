using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public sealed class ArchiveMenuScreen : IDisposable
{
    public readonly GameObject Container;
    public readonly MenuScreen MenuScreen;
    public readonly ScrollRect ScrollRect;

    private readonly RectTransform m_content;
    private readonly RectTransform m_viewport;
    private readonly MenuButton m_backButton;
    private readonly List<ArchiveMenuEntry> m_entries = new List<ArchiveMenuEntry>();
    private readonly ArchiveMenuScreenDriver m_driver;
    private Text? m_statusText;
    private bool m_disposed;

    public ArchiveMenuScreen(string title, Action onGoBack)
    {
        var canvas = SfmUtil.GetChild(UIManager.instance.gameObject, "UICanvas")!;
        var optionsScreen = SfmUtil.GetChild(canvas, "OptionsMenuScreen")!;

        Container = UnityEngine.Object.Instantiate(optionsScreen, canvas.transform, false);
        Container.name = "SFM-ArchiveSlotSelectionMenu";
        Container.SetActive(false);

        SfmUtil.RemoveComponentImmediate<MenuButtonList>(Container);

        var oldContent = SfmUtil.GetChild(Container, "Content")!;
        UnityEngine.Object.DestroyImmediate(oldContent);

        var titleText = SfmUtil.GetChildComponent<Text>(Container, "Title")!;
        SfmUtil.RemoveComponent<AutoLocalizeTextUI>(titleText.gameObject);
        titleText.text = title;

        MenuScreen = Container.GetComponent<MenuScreen>()!;
        m_backButton = SfmUtil.GetChildComponent<MenuButton>(Container, "Controls/ApplyButton")!;
        SfmUtil.RemoveComponent<EventTrigger>(m_backButton.gameObject);
        m_backButton.OnSubmitPressed = new UnityEvent();
        m_backButton.OnSubmitPressed.AddListener(() => onGoBack());
        MenuScreen.backButton = m_backButton;

        var scrollPane = new GameObject("SFM-ArchiveScrollPane", typeof(RectTransform), typeof(ScrollRect));
        scrollPane.layer = Container.layer;
        var scrollTransform = scrollPane.GetComponent<RectTransform>()!;
        scrollTransform.SetParent(Container.transform, false);
        scrollTransform.anchorMin = new Vector2(0.5f, 1f);
        scrollTransform.anchorMax = new Vector2(0.5f, 1f);
        scrollTransform.pivot = new Vector2(0.5f, 1f);
        scrollTransform.anchoredPosition = new Vector2(0f, -330f);
        scrollTransform.sizeDelta = new Vector2(1510f, Mathf.Ceil(105f * 8.334f));

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.layer = Container.layer;
        m_viewport = viewport.GetComponent<RectTransform>()!;
        m_viewport.SetParent(scrollTransform, false);
        FitToParent(m_viewport);
        var viewportImage = viewport.GetComponent<Image>()!;
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        var content = new GameObject("Content", typeof(RectTransform));
        content.layer = Container.layer;
        m_content = content.GetComponent<RectTransform>()!;
        m_content.SetParent(m_viewport, false);
        m_content.anchorMin = new Vector2(0.5f, 1f);
        m_content.anchorMax = new Vector2(0.5f, 1f);
        m_content.pivot = new Vector2(0.5f, 1f);
        m_content.anchoredPosition = Vector2.zero;
        m_content.sizeDelta = new Vector2(ArchiveMenuEntry.SLOT_WIDTH, 0f);

        ScrollRect = scrollPane.GetComponent<ScrollRect>()!;
        ScrollRect.horizontal = false;
        ScrollRect.vertical = true;
        ScrollRect.movementType = ScrollRect.MovementType.Clamped;
        ScrollRect.scrollSensitivity = 80f;
        ScrollRect.viewport = m_viewport;
        ScrollRect.content = m_content;
        ScrollRect.verticalNormalizedPosition = 1f;

        m_driver = Container.AddComponent<ArchiveMenuScreenDriver>();
        m_driver.Initialize(this);
    }

    public Text AddStatusLabel(string text)
    {
        var canvas = SfmUtil.GetChild(UIManager.instance.gameObject, "UICanvas")!;
        var source = SfmUtil.GetChild(canvas, "OptionsMenuScreen/Content/GameOptions/GameOptionsButton/Menu Button Text")!;
        var label = UnityEngine.Object.Instantiate(source, m_content, false);
        label.name = "SFM-StatusLabel";
        label.SetActive(true);

        SfmUtil.RemoveComponent<AutoLocalizeTextUI>(label);
        SfmUtil.RemoveComponent<ChangeTextFontScaleOnHandHeld>(label);
        SfmUtil.RemoveComponent<FixVerticalAlign>(label);

        m_statusText = label.GetComponent<Text>()!;
        m_statusText.raycastTarget = false;
        m_statusText.lineSpacing = 1f;
        m_statusText.text = text;

        var transform = label.GetComponent<RectTransform>()!;
        transform.anchorMin = new Vector2(0.5f, 1f);
        transform.anchorMax = new Vector2(0.5f, 1f);
        transform.pivot = new Vector2(0.5f, 1f);
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = new Vector2(ArchiveMenuEntry.SLOT_WIDTH, ArchiveMenuEntry.SLOT_TOTAL_HEIGHT);

        UpdateLayout();
        return m_statusText;
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

    public void Show()
    {
        UpdateLayout();
        EventSystem.current?.SetSelectedGameObject(null);
        Container.SetActive(true);
    }

    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }

        m_disposed = true;
        m_backButton.OnSubmitPressed.RemoveAllListeners();
        UnityEngine.Object.Destroy(Container);
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

    private void UpdateLayout()
    {
        for (int i = 0; i < m_entries.Count; i++)
        {
            var transform = m_entries[i].Container.GetComponent<RectTransform>()!;
            transform.anchorMin = new Vector2(0.5f, 1f);
            transform.anchorMax = new Vector2(0.5f, 1f);
            transform.pivot = new Vector2(0.5f, 1f);
            transform.anchoredPosition = new Vector2(0f, -(i + 1) * ArchiveMenuEntry.SLOT_TOTAL_HEIGHT);
        }

        float rowCount = m_entries.Count + (m_statusText != null ? 1 : 0);
        m_content.sizeDelta = new Vector2(ArchiveMenuEntry.SLOT_WIDTH, rowCount * ArchiveMenuEntry.SLOT_TOTAL_HEIGHT);

        RebuildNavigation();
        Canvas.ForceUpdateCanvases();
    }

    private void RebuildNavigation()
    {
        if (m_entries.Count == 0)
        {
            MenuScreen.defaultHighlight = m_backButton;
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
        MenuScreen.defaultHighlight = m_entries[0].MenuButton;
    }

    private static void FitToParent(RectTransform transform)
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
    private ArchiveMenuScreen? m_screen;

    public void Initialize(ArchiveMenuScreen screen)
    {
        m_screen = screen;
    }

    public void LateUpdate()
    {
        m_screen?.KeepSelectionVisible();
    }
}