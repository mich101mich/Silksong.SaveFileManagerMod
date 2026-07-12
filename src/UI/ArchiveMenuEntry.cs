using System;
using System.Collections;
using System.Globalization;
using Silksong.ModMenu.Elements;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public partial class ArchiveMenuEntry : TextButton
{
    public ArchiveMenuEntry(int slotIndex, SaveStats? stats, string? message, SaveSlotButton template)
        : base(Init(slotIndex, stats, message), message ?? "")
    {
        base.Container.name = $"SFM-ArchiveSaveSlotButton";

        // Width calculation: Scroll area size is 1510 and we need to fit the slot + selection arrow on both sides.
        const float totalWidth = 1510f;
        const float arrowWidth = 164f;
        const float slotWidth = totalWidth - (arrowWidth * 2f);

        base.Container.GetComponent<RectTransform>()!.sizeDelta = new Vector2(slotWidth, 125f);

        var menuButton = base.ButtonText.gameObject;

        var oldFitter = menuButton.GetComponent<ContentSizeFitter>()!;
        UnityEngine.Object.Destroy(oldFitter);

        menuButton.GetComponent<RectTransform>()!.sizeDelta = new Vector2(slotWidth, 125f);

        base.ButtonText.alignment = TextAnchor.MiddleLeft; // We made the slot wider, so the text should no longer be centered.

        if (message == null && stats != null)
        {
            var target = base.DescriptionText.gameObject; // Put the decorations in the place where the description text would normally go

            var entry = target.AddComponent<ArchiveSaveSlotButton>();
            entry.Initialize(slotIndex, stats, template);
        }
    }

    public static string Init(int slotIndex, SaveStats? stats, string? message)
    {
        if (message != null)
        {
            return $"{slotIndex}. <{Strings.Error.Text}>";
        }
        else if (stats != null)
        {
            if (SaveName.GetOrLoadSlotName(slotIndex, out string name))
            {
                return $"{slotIndex}. {name}";
            }
            else
            {
                return $"{slotIndex}. <{Strings.Unnamed.Text}>";
            }
        }
        else
        {
            return $"{slotIndex}. <{Strings.Empty.Text}>";
        }
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
        if (currentSaveStats.IsBlackThreadInfected)
        {
            slot.healthSlots.gameObject.SetActive(false);

            slot.silkBar.gameObject.SetActive(false);

            slot.saveSlotCompletionIcons?.gameObject.SetActive(false);
        }
        else
        {
            bool isSteelsoul = currentSaveStats.PermadeathMode == GlobalEnums.PermadeathModes.On && !currentSaveStats.BossRushMode;

            slot.healthSlots.gameObject.SetActive(true);
            slot.healthSlots.ShowHealth(currentSaveStats.MaxHealth, isSteelsoul, currentSaveStats.CrestId);

            slot.silkBar.ShowSilk(currentSaveStats.IsSpoolBroken, currentSaveStats.MaxSilk, currentSaveStats.CrestId == "Cursed");

            slot.saveSlotCompletionIcons?.gameObject.SetActive(true);
            slot.saveSlotCompletionIcons?.SetCompletionIconState(currentSaveStats);
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
