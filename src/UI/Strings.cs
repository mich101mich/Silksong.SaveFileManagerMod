using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using TeamCherry.Localization;

namespace SaveFileManagerMod.UI;

public class StringHelper
{
    public static LocalizedText Get(string key) => LocalizedText.Key(new LocalisedString($"Mods.{SaveFileManagerPlugin.Id}", key));

    public static LocalizedText Get(string key, object args)
    {
        string text = Language.Get(key, $"Mods.{SaveFileManagerPlugin.Id}");
        foreach (var property in args.GetType().GetProperties())
        {
            text = text.Replace($"{{{property.Name}}}", property.GetValue(args).ToString());
        }
        return LocalizedText.Raw(text);
    }
}

public partial class ArchiveSlotSelectionMenu
{
    public class Strings : StringHelper
    {
        // "Load from archive"
        public static LocalizedText TitleLoadIntoEmpty => Get("ArchiveSlotSelectionMenu.TitleLoadIntoEmpty");

        // "Archive/Replace"
        public static LocalizedText TitleArchiveReplace => Get("ArchiveSlotSelectionMenu.TitleArchiveReplace");

        // $"Target: Slot {index}. \"{name}\""
        public static LocalizedText NamedSlotInfo(string name, int index) => Get("ArchiveSlotSelectionMenu.NamedSlotInfo", new { name, index });

        // $"Target: Slot {index}."
        public static LocalizedText UnnamedSlotInfo(int index) => Get("ArchiveSlotSelectionMenu.UnnamedSlotInfo", new { index });

        // "Loading save slots..."
        public static LocalizedText LoadingText => Get("ArchiveSlotSelectionMenu.LoadingText");

        // "No loadable saves found"
        public static LocalizedText NoLoadableSavesFound => Get("ArchiveSlotSelectionMenu.NoLoadableSavesFound");

        // "Finished loading save slots"
        public static LocalizedText FinishedLoadingSaveSlots => Get("ArchiveSlotSelectionMenu.FinishedLoadingSaveSlots");
    }
}

