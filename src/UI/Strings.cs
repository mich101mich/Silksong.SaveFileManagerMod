using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using TeamCherry.Localization;

namespace SaveFileManagerMod.UI;

public class StringHelper
{
    public static string ModSheet => $"Mods.{SaveFileManagerPlugin.Id}";

    public static string Get(string key, string fallback)
    {
        // Note: We could return a LocalisedString or LocalizedText here, but that would make handling more complicated.
        //       The only benefit that those types provide is that they can be auto refreshed when the language changes,
        //       but that is not possible from within our menus, and they are all reconstructed on load anyway, so the
        //       live translation would never take effect.
        return Language.Has(key, ModSheet) ? Language.Get(key, ModSheet) : fallback;
    }

    public static string Get(string key, string fallback, object args)
    {
        string text = Get(key, fallback);
        foreach (var property in args.GetType().GetProperties())
        {
            text = text.Replace($"{{{property.Name}}}", property.GetValue(args).ToString());
        }
        return text;
    }
}

public partial class ArchiveSlotSelectionMenu
{
    public class Strings : StringHelper
    {
        public static string TitleLoadIntoEmpty => Get("ArchiveSlotSelectionMenu.TitleLoadIntoEmpty", "Load from archive");
        public static string TitleArchiveSwap => Get("ArchiveSlotSelectionMenu.TitleArchiveSwap", "Archive / Swap");
        public static string NamedSlotInfo(string name, int index) => Get("ArchiveSlotSelectionMenu.NamedSlotInfo", "Target: Slot {index}. \"{name}\"", new { name, index });
        public static string UnnamedSlotInfo(int index) => Get("ArchiveSlotSelectionMenu.UnnamedSlotInfo", "Target: Slot {index}.", new { index });
        public static string LoadingText => Get("ArchiveSlotSelectionMenu.LoadingText", "Loading save slots...");
        public static string NoLoadableSavesFound => Get("ArchiveSlotSelectionMenu.NoLoadableSavesFound", "No loadable saves found");
    }
}

public partial class ArchiveMenuEntry
{
    public class Strings : StringHelper
    {
        public static string Error => Get("ArchiveMenuEntry.Error", "Error");
        public static string Unnamed => Get("ArchiveMenuEntry.Unnamed", "Unnamed");
        public static string Empty => Get("ArchiveMenuEntry.Empty", "Empty");
    }
}
