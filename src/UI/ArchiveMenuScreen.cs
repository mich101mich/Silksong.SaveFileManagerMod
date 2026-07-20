using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

namespace SaveFileManagerMod.UI;

public sealed class ArchiveMenuScreen : IDisposable
{
    public GameObject m_container;
    public MenuScreen m_menuScreen;
    public ScrollRect m_scrollRect;
    public RectTransform m_content;
    public RectTransform m_viewport;
    public Slider m_verticalSlider;
    public MenuButton m_backButton;
    public Coroutine? m_scrollRoutine;
    public List<ArchiveMenuEntry> m_entries = new List<ArchiveMenuEntry>();
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
        scrollTransform.sizeDelta = new Vector2(1510f, 875f);

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

        m_verticalSlider = CreateVerticalSlider(scrollTransform);
        m_verticalSlider.onValueChanged.AddListener(value =>
        {
            if (!Mathf.Approximately(m_scrollRect.verticalNormalizedPosition, value))
            {
                m_scrollRect.verticalNormalizedPosition = value;
            }
        });
        m_scrollRect.onValueChanged.AddListener(position => m_verticalSlider.SetValueWithoutNotify(position.y));

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

        var onSelectListener = new EventTrigger.Entry();
        onSelectListener.eventID = EventTriggerType.Select;
        onSelectListener.callback.AddListener((data) =>
        {
            if (data is not AxisEventData)
            {
                return;
            }

            // Keyboard/controller navigation: Scroll the entry into view
            var ui = UIManager.instance;
            if (m_scrollRoutine != null)
            {
                ui.StopCoroutine(m_scrollRoutine);
                m_scrollRoutine = null;
            }
            m_scrollRoutine = ui.StartCoroutine(ScrollIntoView(entry));

            // Also prevent cursor select so that we don't get weird double selections
            UIManager.instance.inputModule.focusOnMouseHover = false;
        });

        var eventTrigger = entry.MenuButton.gameObject.GetComponent<EventTrigger>()!;
        eventTrigger.triggers.Add(onSelectListener);

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

        if (m_scrollRoutine != null)
        {
            UIManager.instance.StopCoroutine(m_scrollRoutine);
            m_scrollRoutine = null;
            UIManager.instance.inputModule.focusOnMouseHover = true;
        }

        UnityEngine.Object.Destroy(m_container);
    }

    public static readonly float SCROLL_TIME_SECONDS = 0.2f;
    public IEnumerator ScrollIntoView(ArchiveMenuEntry entry)
    {
        float startY = m_scrollRect.verticalNormalizedPosition;
        for (float time = 0f; time < SCROLL_TIME_SECONDS; time += Time.unscaledDeltaTime)
        {
            // Since more elements might be added while we scroll, we will need to recalculate the scroll position each time
            var viewportSize = m_scrollRect.viewport.rect.size;
            var contentSize = m_scrollRect.content.rect.size;

            float targetY = m_scrollRect.content.InverseTransformPoint(entry.Container.transform.position).y + contentSize.y;

            float rawScrollY = (targetY - viewportSize.y * 0.5f) / (contentSize.y - viewportSize.y);

            var scrollY = Mathf.Clamp01(rawScrollY);

            scrollY = Mathf.Lerp(startY, scrollY, time / SCROLL_TIME_SECONDS);

            m_scrollRect.verticalNormalizedPosition = scrollY;
            yield return null;
        }

        UIManager.instance.inputModule.focusOnMouseHover = true;
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
            m_backButton.navigation = m_backButton.navigation with
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = m_backButton,
                selectOnDown = m_backButton,
            };
            return;
        }

        for (int i = 0; i < m_entries.Count; i++)
        {
            var button = m_entries[i].MenuButton;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i == 0 ? m_backButton : m_entries[i - 1].MenuButton,
                selectOnDown = i == m_entries.Count - 1 ? m_backButton : m_entries[i + 1].MenuButton,
            };
        }

        m_backButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = m_entries.Last().MenuButton,
            selectOnDown = m_entries.First().MenuButton,
        };
        m_menuScreen.defaultHighlight = m_entries.First().MenuButton;
    }

    public static void FitToParent(RectTransform transform)
    {
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.pivot = new Vector2(0.5f, 0.5f);
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
    }

    public static Slider CreateVerticalSlider(RectTransform scrollTransform)
    {
        var source = SfmUtil.GetChild(UIManager.instance.achievementsMenuScreen.gameObject, "Content/Scrollbar")!;
        var sliderObject = UnityEngine.Object.Instantiate(source, scrollTransform, false);
        sliderObject.name = "Scrollbar";

        var sliderTransform = sliderObject.GetComponent<RectTransform>()!;
        sliderTransform.anchorMin = new Vector2(1f, 0f);
        sliderTransform.anchorMax = new Vector2(1f, 1f);
        sliderTransform.pivot = new Vector2(0f, 0.5f);
        sliderTransform.anchoredPosition = new Vector2(0f, 0f);
        sliderTransform.sizeDelta = new Vector2(50f, 0f);

        var background = SfmUtil.GetChildComponent<RectTransform>(sliderObject, "Background")!;
        background.anchorMin = new Vector2(background.anchorMin.x, 0f);
        background.anchorMax = new Vector2(background.anchorMax.x, 1f);
        background.anchoredPosition = new Vector2(background.anchoredPosition.x, 0f);
        background.sizeDelta = new Vector2(background.sizeDelta.x, 0f);

        var slidingArea = SfmUtil.GetChildComponent<RectTransform>(sliderObject, "Sliding Area")!;
        FitToParent(slidingArea);
        slidingArea.sizeDelta = new Vector2(0f, -150f);

        var originalHandle = SfmUtil.GetChild(sliderObject, "Sliding Area/Handle")!;
        var handle = SfmUtil.GetChild(originalHandle, "TopFleur")!;
        SfmUtil.RemoveComponentImmediate<ScrollBarHandle>(handle);

        var handleTransform = handle.GetComponent<RectTransform>()!;
        handleTransform.SetParentReset(slidingArea);
        handleTransform.anchorMin = new Vector2(0f, 0.5f);
        handleTransform.anchorMax = new Vector2(1f, 0.5f);
        handleTransform.pivot = new Vector2(0.5f, 0.5f);
        handleTransform.anchoredPosition = Vector2.zero;
        handleTransform.sizeDelta = new Vector2(0f, 140f);
        handle.name = "Handle";

        UnityEngine.Object.DestroyImmediate(originalHandle);
        SfmUtil.RemoveComponentImmediate<Scrollbar>(sliderObject);

        var slider = sliderObject.AddComponent<Slider>();
        slider.handleRect = handleTransform;
        slider.direction = Slider.Direction.BottomToTop;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(1f);
        return slider;
    }
}
