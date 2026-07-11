using System;
using System.Collections;
using System.Globalization;
using Silksong.ModMenu.Elements;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public partial class ArchiveMenuEntry : SelectableElement
{
    TextButton m_textButton;

    public Action? OnSubmit;

    public ArchiveMenuEntry(int slotIndex, SaveStats? stats, string? message, SaveSlotButton template)
        : base(Init(slotIndex, stats, message, template, out var textButton, out var selectable), selectable)
    {
        Container.name = $"SFM-ArchiveSaveSlotButton";
        m_textButton = textButton;
        m_textButton.OnSubmit = () => OnSubmit?.Invoke();

        // Width calculation: Scroll area size is 1510 and, we need to fit the slot + selection arrow on both sides.
        const float totalWidth = 1510f;
        const float arrowWidth = 164f;
        const float slotWidth = totalWidth - (arrowWidth * 2f);

        m_textButton.Container.GetComponent<RectTransform>()!.sizeDelta = new Vector2(slotWidth, 105f);

        var menuButton = m_textButton.ButtonText.gameObject;

        var oldFitter = menuButton.GetComponent<ContentSizeFitter>();
        if (oldFitter != null)
        {
            UnityEngine.Object.Destroy(oldFitter);
        }

        menuButton.GetComponent<RectTransform>()!.sizeDelta = new Vector2(slotWidth, 105f);

        m_textButton.ButtonText.alignment = TextAnchor.MiddleLeft; // We made the slot wider, so the text should no longer be centered.
    }

    public static GameObject Init(int slotIndex, SaveStats? stats, string? message, SaveSlotButton template, out TextButton textButton, out Selectable selectable)
    {
        if (message != null)
        {
            textButton = new TextButton($"{slotIndex}. ({Strings.Error.Text})", message);
        }
        else if (stats != null)
        {
            if (SaveName.GetOrLoadSlotName(slotIndex, out string name))
            {
                textButton = new TextButton($"{slotIndex}. {name}");
            }
            else
            {
                textButton = new TextButton($"{slotIndex}. ");

            }

            var entry = textButton.Container.AddComponent<ArchiveSaveSlotButton>();
            entry.Initialize(slotIndex, stats, template);
        }
        else
        {
            textButton = new TextButton($"{slotIndex}. ({Strings.Empty.Text})");
        }
        selectable = textButton.SelectableComponent;
        return textButton.Container;
    }

    /// <inheritdoc/>
    public override void SetMainColor(Color color)
    {
        m_textButton.SetMainColor(color);
    }

    /// <inheritdoc/>
    public override void SetFontSizes(FontSizes fontSizes)
    {
        m_textButton.SetFontSizes(fontSizes);
    }
}

public class ArchiveSaveSlotButton : Component
{
    public void Initialize(int slotIndex, SaveStats stats, SaveSlotButton template)
    {
        var spoolIcon = CloneChild(template, "ActiveSaveSlot/HUDLayout/Health Bar/Spool Icon");
        var healthSlots = CloneChild(template, "ActiveSaveSlot/HUDLayout/Health Bar/HealthSlots");
        var threadSpool = CloneChild(template, "ActiveSaveSlot/HUDLayout/Thread Spool");
        var rosaries = CloneChild(template, "ActiveSaveSlot/HUDLayout/Rosaries");
        var shellShards = CloneChild(template, "ActiveSaveSlot/HUDLayout/Shell Shards");
    }

    public static void ShowSaveSlot(SaveSlotButton slot, SaveStats currentSaveStats)
    {
        var saveSlotCompletionIcons = typeof(SaveSlotButton)
            .GetField("saveSlotCompletionIcons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(slot) as SaveSlotCompletionIcons;

        if (currentSaveStats.IsBlackThreadInfected)
        {
            slot.healthSlots.gameObject.SetActive(false);

            slot.silkBar.gameObject.SetActive(false);

            saveSlotCompletionIcons?.gameObject.SetActive(false);
        }
        else
        {
            bool isSteelsoul = currentSaveStats.PermadeathMode == GlobalEnums.PermadeathModes.On && !currentSaveStats.BossRushMode;

            slot.healthSlots.gameObject.SetActive(true);
            slot.healthSlots.ShowHealth(currentSaveStats.MaxHealth, isSteelsoul, currentSaveStats.CrestId);

            slot.silkBar.ShowSilk(currentSaveStats.IsSpoolBroken, currentSaveStats.MaxSilk, currentSaveStats.CrestId == "Cursed");

            saveSlotCompletionIcons?.gameObject.SetActive(true);
            saveSlotCompletionIcons?.SetCompletionIconState(currentSaveStats);
        }

        bool showRosariesAndShards = !currentSaveStats.IsBlackThreadInfected && !currentSaveStats.BossRushMode;
        slot.rosaryGroup.SetActive(showRosariesAndShards);
        slot.shardGroup.SetActive(showRosariesAndShards);
        if (showRosariesAndShards)
        {
            slot.rosaryText.text = currentSaveStats.Geo.ToString();
            slot.shardText.text = currentSaveStats.Shards.ToString();
        }

        if (currentSaveStats.UnlockedCompletionRate && !currentSaveStats.IsBlackThreadInfected && !currentSaveStats.BossRushMode)
        {
            slot.completionText.gameObject.SetActive(true);
            slot.completionText.text = currentSaveStats.CompletionPercentage.ToString(CultureInfo.InvariantCulture) + "%";
        }
        else
        {
            slot.completionText.gameObject.SetActive(false);
        }

        slot.playTimeText.gameObject.SetActive(true);
        slot.playTimeText.text = currentSaveStats.GetPlaytimeHHMM();

        string location;
        if (currentSaveStats.IsBlackThreadInfected)
        {
            location = "???";
        }
        else if (slot.saveSlots.GetBackground(currentSaveStats)?.NameOverride is LocalisedString nameOverride && !nameOverride.IsEmpty)
        {
            location = nameOverride;
        }
        else
        {
            location = GameManager.GetFormattedMapZoneStringV2(currentSaveStats.MapZone);
        }
        slot.locationText.gameObject.SetActive(true);
        slot.locationText.text = location.Replace("<br>", Environment.NewLine);
    }

    public GameObject CloneChild(SaveSlotButton slot, string childPath)
    {
        var parent = slot.gameObject;

        var lastSlashIndex = childPath.LastIndexOf('/');
        var childName = lastSlashIndex >= 0 ? childPath.Substring(lastSlashIndex + 1) : childPath;

        var child = parent.transform.Find(childPath);
        if (child == null)
        {
            SfmLogger.LogError($"Could not find child '{childPath}' in '{parent.name}'");
            return new GameObject($"MissingChild-{childName}");
        }

        var clone = UnityEngine.Object.Instantiate(child.gameObject, gameObject.transform);
        clone.name = childName;
        clone.SetActive(true);

        var rectTransform = clone.GetComponent<RectTransform>()!;
        rectTransform.SetLocalPositionY(0f);

        return clone;
    }
}
