using System;
using System.Collections.Generic;
using System.IO;

namespace SaveFileManagerMod;

public class SaveName
{
    public static Dictionary<int, string> Names = new();

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
            var baseDir = Silksong.DataManager.DataPaths.SaveDataDir(slotIndex);
            Directory.CreateDirectory(baseDir);
            File.WriteAllText(nameFilePath, normalized);
            SfmLogger.LogInfo($"Saved name \"{normalized}\" for slot {slotIndex} to {nameFilePath}");
        }
        catch (Exception ex)
        {
            SfmLogger.LogInfo($"Error saving name for slot {slotIndex} to {nameFilePath}: {ex.Message}");
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
            SfmLogger.LogInfo($"Error loading name for slot {slotIndex} from {nameFilePath}: {ex.Message}");
        }

        Names[slotIndex] = string.Empty;
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

        var baseDir = Silksong.DataManager.DataPaths.SaveDataDir(slotIndex);
        return System.IO.Path.Combine(baseDir, "sfm_save_name.txt.dat"); // .dat extension to get cloud sync, which globs for all .dat files.
    }
}
