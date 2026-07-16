using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using TeamCherry.Localization;

namespace SaveFileManagerMod.UI;

using LT = LocalizedText; // to shorten declarations

public class StringHelper
{
    public static string ModSheet => $"Mods.{SaveFileManagerPlugin.Id}";

    public static LocalizedText Get(string key, string fallback)
    {
        return Language.Has(key, ModSheet)
            ? LocalizedText.Key(new LocalisedString(ModSheet, key))
            : LocalizedText.Raw(fallback);
    }

    public static LocalizedText Get(string key, string fallback, object args)
    {
        string text = Language.Has(key, ModSheet) ? Language.Get(key, ModSheet) : fallback;
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
        public static LT TitleLoadIntoEmpty => Get("ArchiveSlotSelectionMenu.TitleLoadIntoEmpty", "Load from archive");
        public static LT TitleArchiveReplace => Get("ArchiveSlotSelectionMenu.TitleArchiveReplace", "Archive / Replace");
        public static LT NamedSlotInfo(string name, int index) => Get("ArchiveSlotSelectionMenu.NamedSlotInfo", "Target: Slot {index}. \"{name}\"", new { name, index });
        public static LT UnnamedSlotInfo(int index) => Get("ArchiveSlotSelectionMenu.UnnamedSlotInfo", "Target: Slot {index}.", new { index });
        public static LT LoadingText => Get("ArchiveSlotSelectionMenu.LoadingText", "Loading save slots...");
        public static LT NoLoadableSavesFound => Get("ArchiveSlotSelectionMenu.NoLoadableSavesFound", "No loadable saves found");
    }
}

public partial class ArchiveMenuEntry
{
    public class Strings : StringHelper
    {
        public static LT Error => Get("ArchiveMenuEntry.Error", "Error");
        public static LT Unnamed => Get("ArchiveMenuEntry.Unnamed", "Unnamed");
        public static LT Empty => Get("ArchiveMenuEntry.Empty", "Empty");
    }
}
