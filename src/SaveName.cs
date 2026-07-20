using System;
using System.Collections.Generic;
using System.IO;

namespace SaveFileManagerMod;

public class SaveName
{
    public static Dictionary<int, string> Names = new Dictionary<int, string>();

    /// <summary>
    /// Tries to get the name for a save slot. Returns false if no name is set.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    /// <param name="name">The name of the save slot, if set.</param>
    /// <returns>True if a name is set for the slot; otherwise, false.</returns>
    public static bool TryGetSlotName(int slotIndex, out string name)
    {
        return Names.TryGetValue(slotIndex, out name!)
            && !string.IsNullOrWhiteSpace(name);
    }

    /// <summary>
    /// Gets the name for a save slot, loading it from disk if necessary. Returns false if no name is set.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    /// <param name="name">The name of the save slot, if set.</param>
    /// <returns>True if a name is set for the slot; otherwise, false.</returns>
    public static bool GetOrLoadSlotName(int slotIndex, out string name)
    {
        if (TryGetSlotName(slotIndex, out name!))
        {
            return true;
        }

        ReloadSlot(slotIndex);
        return TryGetSlotName(slotIndex, out name!);
    }

    /// <summary>
    /// Sets the name for a save slot and saves it to disk.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    /// <param name="name">The name to set for the save slot.</param>
    public static void SetSlotName(int slotIndex, string name)
    {
        string normalized = name.Trim();

        Names[slotIndex] = normalized;

        var nameFilePath = GetNameFilePath(slotIndex);
        try
        {
            var baseDir = Path.GetDirectoryName(nameFilePath);
            Directory.CreateDirectory(baseDir);
            File.WriteAllText(nameFilePath, normalized);
            SfmLogger.LogInfo($"Saved name \"{normalized}\" for slot {slotIndex} to {nameFilePath}");
        }
        catch (Exception ex)
        {
            SfmLogger.LogError($"Error saving name for slot {slotIndex} to {nameFilePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Reloads the name for a save slot from disk.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    public static void ReloadSlot(int slotIndex)
    {
        var nameFilePath = GetNameFilePath(slotIndex);
        try
        {
            string name = System.IO.File.ReadAllText(nameFilePath).Trim();
            Names[slotIndex] = name;
            SfmLogger.LogInfo($"Loaded name \"{name}\" for slot {slotIndex}");
            return;
        }
        catch (DirectoryNotFoundException)
        {
            // ok, no name saved yet
        }
        catch (FileNotFoundException)
        {
            // ok, no name saved yet
        }
        catch (Exception ex)
        {
            SfmLogger.LogError($"Error loading name for slot {slotIndex} from {nameFilePath}: {ex.Message}");
        }

        var legacyNameFilePath = GetLegacyNameFilePath(slotIndex);
        try
        {
            string name = System.IO.File.ReadAllText(legacyNameFilePath).Trim();
            Names[slotIndex] = name;
            SfmLogger.LogInfo($"Loaded legacy name \"{name}\" for slot {slotIndex}");
            SetSlotName(slotIndex, name); // Save to new location
            System.IO.File.Delete(legacyNameFilePath); // Delete old location
            return;
        }
        catch (DirectoryNotFoundException)
        {
            // ok, no name saved yet
        }
        catch (FileNotFoundException)
        {
            // ok, no name saved yet
        }
        catch (Exception ex)
        {
            SfmLogger.LogError($"Error loading legacy name for slot {slotIndex} from {legacyNameFilePath}: {ex.Message}");
        }

        Names[slotIndex] = string.Empty;
    }

    public static void ClearSlot(int slotIndex)
    {
        Names[slotIndex] = string.Empty;

        var nameFilePath = GetNameFilePath(slotIndex);
        try
        {
            File.Delete(nameFilePath);
        }
        catch (Exception)
        {
            // Ignore and let DataManager handle the deletion
        }

        var legacyNameFilePath = GetLegacyNameFilePath(slotIndex);
        try
        {
            File.Delete(legacyNameFilePath);
        }
        catch (Exception)
        {
            // Ignore and let DataManager handle the deletion
        }
    }

    /// <summary>
    /// Gets the file path for the name of a save slot.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    /// <returns>The file path for the name of the save slot.</returns>
    public static string GetNameFilePath(int slotIndex)
    {
        // Note that we don't use the DataManager API for storing this save-file-specific data, because that only loads
        // data when you load into a save, not when you open the save select menu.

        var rootDir = SaveArchive.GetModdedSaveDataDir(slotIndex);
        return Path.Combine(rootDir, "sfm_save_name.txt.dat"); // .dat extension to get cloud sync, which globs for all .dat files.
    }

    /// <summary>
    /// Gets the old file path for the name of a save slot.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    /// <returns>The file path for the name of the save slot.</returns>
    public static string GetLegacyNameFilePath(int slotIndex)
    {
        // This was the path used in the first version of this mod, but it was changed so that the file would not be confused with
        // the DataManager mod's actual save data files.
        var rootDir = SaveArchive.GetModdedSaveDataDir(slotIndex);
        return Path.Combine(rootDir, "SaveData", "sfm_save_name.txt.dat");
    }
}
