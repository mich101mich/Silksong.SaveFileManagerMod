using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using Silksong.ModMenu.Elements;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public partial class ArchiveMenuEntry : TextButton
{
    public static readonly float SLOT_TEXT_HEIGHT = SpacingConstants.VSPACE_SMALL;
    public static readonly float SLOT_PREVIEW_HEIGHT = 140f;
    public static readonly float SLOT_TOTAL_HEIGHT = SLOT_TEXT_HEIGHT + SLOT_PREVIEW_HEIGHT;

    public SaveProfileHealthBar? m_healthSlots;
    public SaveProfileSilkBar? m_silkBar;

    public ArchiveMenuEntry(int slotIndex, SaveStats? stats, string? message, SaveSlotButton template)
        : base(Init(slotIndex, stats, message), message ?? "")
    {
        base.Container.name = $"SFM-ArchiveSaveSlotButton";

        // Width calculation: Scroll area size is 1510 and we need to fit the slot + selection arrow on both sides.
        const float totalWidth = 1510f;
        const float arrowWidth = 164f;
        const float slotWidth = totalWidth - (arrowWidth * 2f);

        // Layout:
        // +-------------------------------------------------------------------------------+ ---
        // | <<< <N>. <Slot Name>                                                      >>> |  | SLOT_TEXT_HEIGHT
        // |     [     ]                         R 1234                                    | ---
        // |     [Crest] M M M M M  +||||---+             91%    12H 15M   The Abyss       |  | SLOT_PREVIEW_HEIGHT
        // |     [     ]                         S 800                                     |  |
        // +-------------------------------------------------------------------------------+ ---

        base.Container.GetComponent<RectTransform>()!.sizeDelta = new Vector2(slotWidth, SLOT_TOTAL_HEIGHT);

        var textButton = FindChildObject(base.Container, "TextButton")!;
        textButton.GetComponent<RectTransform>()!.sizeDelta = new Vector2(slotWidth, SLOT_TOTAL_HEIGHT);

        var nameText = base.ButtonText.gameObject;

        var oldFitter = nameText.GetComponent<ContentSizeFitter>()!;
        UnityEngine.Object.Destroy(oldFitter);

        // Name: Top-Aligned against the parent, full width.
        var nameTransform = nameText.GetComponent<RectTransform>()!;
        nameTransform.pivot = new Vector2(0.5f, 1f);
        nameTransform.anchorMin = new Vector2(0.5f, 1f);
        nameTransform.anchorMax = new Vector2(0.5f, 1f);
        nameTransform.anchoredPosition = new Vector2(0f, 0f);
        nameTransform.sizeDelta = new Vector2(slotWidth, SLOT_TEXT_HEIGHT);

        base.ButtonText.alignment = TextAnchor.MiddleLeft; // We made the slot wider, so the text should no longer be centered.

        if (message != null || stats == null)
        {
            return;
        }

        var preview = base.DescriptionText.gameObject; // Put the decorations in the place where the description text would normally go

        var oldMove = preview.GetComponent<ChangePositionByLanguage>()!;
        UnityEngine.Object.Destroy(oldMove);

        // Preview: Top-Aligned against the parent but below the name text, full width.
        var previewTransform = preview.GetComponent<RectTransform>()!;
        previewTransform.pivot = new Vector2(0.5f, 1f);
        previewTransform.anchorMin = new Vector2(0.5f, 1f);
        previewTransform.anchorMax = new Vector2(0.5f, 1f);
        previewTransform.anchoredPosition = new Vector2(0f, -SLOT_TEXT_HEIGHT); // Below the text
        previewTransform.sizeDelta = new Vector2(slotWidth, SLOT_PREVIEW_HEIGHT);

        CloneSaveSlotElements(preview, slotIndex, stats, template);
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

    public void CloneSaveSlotElements(GameObject preview, int slotIndex, SaveStats stats, SaveSlotButton template)
    {
        var (spoolIcon, spoolIconTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Health Bar/Spool Icon");
        var (healthSlots, healthSlotsTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Health Bar/HealthSlots");
        var (threadSpool, threadSpoolTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Thread Spool");
        var (rosaries, rosariesTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Rosaries");
        var (shellShards, shellShardsTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Shell Shards");

        // health slots have a padding on the left to account for the spool icon, but we want to position them manually
        var healthSlotsLayout = healthSlots.GetComponent<GridLayoutGroup>()!;
        healthSlotsLayout.padding = new RectOffset(0, 0, 0, 0);

        var healthHeight = stats.MaxHealth > 5 ? 100f : 50f;
        healthSlotsTransform.sizeDelta = new Vector2(220f, healthHeight);

        spoolIconTransform.anchoredPosition = new Vector2(-45f, 0f);
        healthSlotsTransform.anchoredPosition = new Vector2(145f, 0f);
        threadSpoolTransform.anchoredPosition = new Vector2(145f + 220f, 0f);

        rosariesTransform.anchoredPosition = new Vector2(610f, 35f);
        shellShardsTransform.anchoredPosition = new Vector2(610f, -35f);

        // TODO: Percentage
        // TODO: Playtime
        // TODO: Location

        var healthImages = healthSlotsTransform.GetComponentsInChildren<Image>(includeInactive: true);
        m_healthSlots = new()
        {
            crests = template.healthSlots.crests,
            spoolImage = spoolIcon.GetComponent<Image>()!,
            healthTemplate = healthImages[0],
        };
        for (int i = 1; i < healthImages.Length; i++)
        {
            m_healthSlots.healthImages.Add(healthImages[i]);
        }
        m_healthSlots.ShowHealth(stats.MaxHealth, stats.PermadeathMode == GlobalEnums.PermadeathModes.On && !stats.BossRushMode, stats.CrestId);

        m_silkBar = new()
        {
            sizer = template.silkBar.sizer,
            widthPerSilk = template.silkBar.widthPerSilk,
            baseWidth = template.silkBar.baseWidth,
            silkChunkTemplate = template.silkBar.silkChunkTemplate,
            silkChunkVariants = template.silkBar.silkChunkVariants,
            silkChunkParent = threadSpoolTransform.Find("Rod Sizer")!,
            notBroken = FindChildObject(threadSpool, "Rod Sizer/NotBroken")!,
            brokenAlt = FindChildObject(threadSpool, "Rod Sizer/Broken")!,
            cursedAlt = FindChildObject(threadSpool, "Rod Sizer/Broken Cursed")!,
        };
        // TODO: fix null reference exception here
        // m_silkBar.ShowSilk(stats.IsSpoolBroken, stats.MaxSilk, stats.CrestId == "Cursed");

        var rosaryText = rosariesTransform.Find("Text")?.GetComponent<Text>()!;
        rosaryText.text = stats.Geo.ToString();

        var shardText = shellShardsTransform.Find("Text")?.GetComponent<Text>()!;
        shardText.text = stats.Shards.ToString();
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

    public GameObject? FindChildObject(GameObject parent, string childPath)
    {
        var child = parent.transform.Find(childPath);
        if (child == null)
        {
            SfmLogger.LogError($"Could not find child '{childPath}' in '{parent.name}'");
            return null;
        }
        return child.gameObject;
    }

    public (GameObject obj, RectTransform transform) CloneChild(GameObject target, SaveSlotButton slot, string childPath)
    {
        var parent = slot.gameObject;

        var lastSlashIndex = childPath.LastIndexOf('/');
        var childName = lastSlashIndex >= 0 ? childPath.Substring(lastSlashIndex + 1) : childPath;

        var child = FindChildObject(parent, childPath);
        if (child == null)
        {
            var obj = new GameObject($"MissingChild-{childName}");
            var t = obj.AddComponent<RectTransform>();
            return (obj, t);
        }

        var clone = UnityEngine.Object.Instantiate(child, target.transform);
        clone.name = childName;
        clone.SetActive(true);

        var rectTransform = clone.GetComponent<RectTransform>()!;

        // We want all positions to be based on the left side of the slot
        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0.5f);

        return (clone, rectTransform);
    }
}
