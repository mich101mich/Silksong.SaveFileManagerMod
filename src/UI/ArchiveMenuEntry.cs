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

    public static readonly float SLOT_WIDTH = 1510f - 164f * 2f;

    public SaveProfileHealthBar? m_healthSlots;
    public SaveProfileSilkBar? m_silkBar;

    public ArchiveMenuEntry(int slotIndex, SaveStats? stats, string? message, SaveSlotButton template)
        : base(Init(slotIndex, stats, message), message ?? "")
    {
        base.Container.name = $"SFM-ArchiveSaveSlot";

        // Layout:
        // +-------------------------------------------------------------------------------+ ---
        // | <<< <N>. <Slot Name>                                                      >>> |  | SLOT_TEXT_HEIGHT
        // |     [     ]                         R 1234    12H 15M       91%               | ---
        // |     [Crest] M M M M M  +||||---+                                              |  | SLOT_PREVIEW_HEIGHT
        // |     [     ]                         S 800     The Abyss                       |  |
        // +-------------------------------------------------------------------------------+ ---

        base.Container.GetComponent<RectTransform>()!.sizeDelta = new Vector2(SLOT_WIDTH, SLOT_TOTAL_HEIGHT);

        var textButton = GetChild(base.Container, "TextButton")!;
        textButton.name = "SFM-ArchiveSaveSlot-Inner";
        textButton.GetComponent<RectTransform>()!.sizeDelta = new Vector2(SLOT_WIDTH, SLOT_TOTAL_HEIGHT);

        var nameText = base.ButtonText.gameObject;
        nameText.name = "SFM-SaveSlotName";

        var oldFitter = nameText.GetComponent<ContentSizeFitter>()!;
        UnityEngine.Object.Destroy(oldFitter);

        // Name: Top-Aligned against the parent, full width.
        var nameTransform = nameText.GetComponent<RectTransform>()!;
        nameTransform.pivot = new Vector2(0.5f, 1f);
        nameTransform.anchorMin = new Vector2(0.5f, 1f);
        nameTransform.anchorMax = new Vector2(0.5f, 1f);
        nameTransform.anchoredPosition = new Vector2(0f, 0f);
        nameTransform.sizeDelta = new Vector2(SLOT_WIDTH, SLOT_TEXT_HEIGHT);

        base.ButtonText.alignment = TextAnchor.MiddleLeft; // We made the slot wider, so the text should no longer be centered.

        var preview = base.DescriptionText.gameObject; // Put the decorations in the place where the description text would normally go
        preview.name = "SFM-SaveSlotPreview";

        if (message != null || stats == null)
        {
            return;
        }

        var oldMove = preview.GetComponent<ChangePositionByLanguage>()!;
        UnityEngine.Object.Destroy(oldMove);

        // Preview: Top-Aligned against the parent but below the name text, full width.
        var previewTransform = preview.GetComponent<RectTransform>()!;
        previewTransform.pivot = new Vector2(0.5f, 1f);
        previewTransform.anchorMin = new Vector2(0.5f, 1f);
        previewTransform.anchorMax = new Vector2(0.5f, 1f);
        previewTransform.anchoredPosition = new Vector2(0f, -SLOT_TEXT_HEIGHT); // Below the text
        previewTransform.sizeDelta = new Vector2(SLOT_WIDTH, SLOT_PREVIEW_HEIGHT);

        CloneSaveSlotElements(preview, slotIndex, stats, template);
    }

    public static string Init(int slotIndex, SaveStats? stats, string? message)
    {
        if (message != null)
        {
            return $"{slotIndex}. <{Strings.Error}>";
        }
        else if (stats != null)
        {
            if (SaveName.GetOrLoadSlotName(slotIndex, out string name))
            {
                return $"{slotIndex}. {name}";
            }
            else
            {
                return $"{slotIndex}. <{Strings.Unnamed}>";
            }
        }
        else
        {
            return $"{slotIndex}. <{Strings.Empty}>";
        }
    }

    public void CloneSaveSlotElements(GameObject preview, int slotIndex, SaveStats stats, SaveSlotButton template)
    {
        var (spoolIcon, spoolIconTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Health Bar/Spool Icon");
        var (healthSlots, healthSlotsTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Health Bar/HealthSlots");
        var (defeated, defeatedTransform) = CloneChild(preview, template, "DefeatedText");
        var (threadSpool, threadSpoolTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Thread Spool");
        var (rosaries, rosariesTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Rosaries");
        var (shellShards, shellShardsTransform) = CloneChild(preview, template, "ActiveSaveSlot/HUDLayout/Shell Shards");
        var (playTime, playTimeTransform) = CloneChild(preview, template, "ActiveSaveSlot/Bottom Section/PlayTimeText");
        var (completion, completionTransform) = CloneChild(preview, template, "ActiveSaveSlot/Bottom Section/CompletionText");
        var (locationObj, locationTransform) = CloneChild(preview, template, "ActiveSaveSlot/Bottom Section/LocationText");


        // health slots have a padding on the left to account for the spool icon, but we want to position them manually
        var healthSlotsLayout = healthSlots.GetComponent<GridLayoutGroup>()!;
        healthSlotsLayout.padding = new RectOffset(0, 0, 0, 0);

        var healthHeight = stats.MaxHealth > 5 ? 100f : 50f;
        healthSlotsTransform.sizeDelta = new Vector2(220f, healthHeight);

        defeatedTransform.sizeDelta = new Vector2(480f, 70f); // defeated text for some reason starts with a negative width

        spoolIconTransform.anchoredPosition = new Vector2(-45f, 0f);

        healthSlotsTransform.anchoredPosition = new Vector2(155f, 0f);
        defeatedTransform.anchoredPosition = new Vector2(155f, 0f); // will be displayed instead of health slots if the save is defeated

        threadSpoolTransform.anchoredPosition = new Vector2(370f, 0f);

        rosariesTransform.anchoredPosition = new Vector2(630f, 35f);
        shellShardsTransform.anchoredPosition = new Vector2(630f, -35f);

        playTimeTransform.anchoredPosition = new Vector2(870f, 35f);
        locationTransform.anchoredPosition = new Vector2(870f, -35f);

        completionTransform.anchoredPosition = new Vector2(890f, 35f);

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

        // Thread spool template starts as a child of "Rod Sizer/Inside", but is moved to "Rod Sizer" if ShowSilk was called.
        // So, filled slots have the template in the parent, while empty slots have the template still in the child.
        var silkChunkTemplateTransform = threadSpoolTransform.Find("Rod Sizer/Inside/Silk Chunk") ?? threadSpoolTransform.Find("Rod Sizer/Silk Chunk");

        m_silkBar = new()
        {
            sizer = GetChildComponent<LayoutElement>(threadSpool, "Rod Sizer")!,
            widthPerSilk = template.silkBar.widthPerSilk,
            baseWidth = template.silkBar.baseWidth,
            silkChunkTemplate = silkChunkTemplateTransform!.gameObject.GetComponent<Image>()!,
            silkChunkVariants = template.silkBar.silkChunkVariants,
            silkChunkParent = threadSpoolTransform.Find("Rod Sizer/Inside")!,
            notBroken = GetChild(threadSpool, "Rod Sizer/NotBroken")!,
            brokenAlt = GetChild(threadSpool, "Rod Sizer/Broken")!,
            cursedAlt = GetChild(threadSpool, "Rod Sizer/Broken Cursed")!,
        };

        if (stats.PermadeathMode == GlobalEnums.PermadeathModes.Dead)
        {
            m_healthSlots.ShowHealth(0, true, stats.CrestId);
            threadSpool.SetActive(false);

            defeated.SetActive(true);
            defeated.GetComponent<Text>()!.alignment = TextAnchor.UpperLeft;
            defeated.GetComponent<CanvasGroup>()!.alpha = 1f;
        }
        else
        {
            bool isStealsoul = stats.PermadeathMode == GlobalEnums.PermadeathModes.On;
            m_healthSlots.ShowHealth(stats.MaxHealth, isStealsoul, stats.CrestId);
            m_silkBar.ShowSilk(stats.IsSpoolBroken, stats.MaxSilk, stats.CrestId == "Cursed");
            defeated.SetActive(false);
        }

        var rosaryText = rosariesTransform.Find("Text")?.GetComponent<Text>()!;
        rosaryText.text = stats.Geo.ToString();
        rosaryText.fontSize = 40;

        var shardText = shellShardsTransform.Find("Text")?.GetComponent<Text>()!;
        shardText.text = stats.Shards.ToString();
        shardText.fontSize = 40;

        var playTimeText = playTime.GetComponent<Text>()!;
        playTimeText.text = stats.GetPlaytimeHHMM();
        playTimeText.alignment = TextAnchor.MiddleLeft;

        if (stats.UnlockedCompletionRate)
        {
            var completionText = completion.GetComponent<Text>()!;
            completionText.text = stats.CompletionPercentage.ToString(CultureInfo.InvariantCulture) + "%";
            completionText.alignment = TextAnchor.MiddleRight;
            completionText.fontSize = 40;
        }
        else
        {
            completion.SetActive(false);
        }

        string locationStr;
        if (stats.IsBlackThreadInfected)
        {
            locationStr = "???";
        }
        else if (template.saveSlots.GetBackground(stats)?.NameOverride is LocalisedString nameOverride && !nameOverride.IsEmpty)
        {
            locationStr = nameOverride;
        }
        else
        {
            locationStr = GameManager.GetFormattedMapZoneStringV2(stats.MapZone);
        }
        var locationText = locationObj.GetComponent<Text>()!;
        locationText.text = locationStr;
        locationText.alignment = TextAnchor.MiddleLeft;
        locationText.fontSize = 40;
    }

    public GameObject? GetChild(GameObject parent, string childPath)
    {
        var child = parent.transform.Find(childPath);
        if (child == null)
        {
            SfmLogger.LogError($"Could not find child '{childPath}' in '{parent.name}'");
            return null;
        }
        return child.gameObject;
    }

    public T? GetChildComponent<T>(GameObject parent, string childPath) where T : class
    {
        var child = GetChild(parent, childPath);
        if (child == null)
        {
            return null;
        }
        var component = child.GetComponent<T>();
        if (component == null)
        {
            SfmLogger.LogError($"Could not find component '{typeof(T).Name}' in '{parent.name}/{childPath}'");
            return null;
        }
        return component;
    }

    public (GameObject obj, RectTransform transform) CloneChild(GameObject target, SaveSlotButton slot, string childPath)
    {
        var parent = slot.gameObject;

        var lastSlashIndex = childPath.LastIndexOf('/');
        var childName = lastSlashIndex >= 0 ? childPath.Substring(lastSlashIndex + 1) : childPath;

        var child = GetChild(parent, childPath);
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
