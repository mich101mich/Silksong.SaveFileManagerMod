
using System;
using System.IO;
using System.Reflection;

namespace SaveFileManagerMod;

public class SaveArchive
{
    /// <summary>
    /// Archives a save slot by moving it to the specified archive slot. Archive slot is assumed to be empty.
    /// </summary>
    /// <param name="slotIndex">The index of the filled save slot to archive.</param>
    /// <param name="archiveSlotIndex">The index of the empty archive slot.</param>
    public static void ArchiveSlot(int slotIndex, int archiveSlotIndex)
    {
        MoveSlot(slotIndex, archiveSlotIndex);
    }

    /// <summary>
    /// Loads a save slot from the specified archive slot. Archive slot is assumed to be filled.
    /// </summary>
    /// <param name="targetSlotIndex">The index of the empty save slot to load into.</param>
    /// <param name="archiveSlotIndex">The index of the filled archive slot.</param>
    public static void LoadSlot(int targetSlotIndex, int archiveSlotIndex)
    {
        MoveSlot(archiveSlotIndex, targetSlotIndex);
    }

    /// <summary>
    /// Swaps the contents of two save slots. Both slots are assumed to be filled.
    /// </summary>
    /// <param name="slotIndexA">The index of the first save slot.</param>
    /// <param name="slotIndexB">The index of the second save slot.</param>
    public static void SwapSlots(int slotIndexA, int slotIndexB)
    {
        if (slotIndexA == slotIndexB)
        {
            return;
        }
        MoveSlot(slotIndexA, 99999);
        MoveSlot(slotIndexB, slotIndexA);
        MoveSlot(99999, slotIndexB);
    }

    /// <summary>
    /// Internal method to move a save slot from one index to another. Target slot is assumed to be empty.
    /// </summary>
    /// <param name="sourceSlotIndex">The index of the source save slot.</param>
    /// <param name="targetSlotIndex">The index of the target save slot.</param>
    public static void MoveSlot(int sourceSlotIndex, int targetSlotIndex)
    {
        var saveDirPath = ((DesktopPlatform)Platform.Current).saveDirPath;

        // ========== Move save files (including backups) ==========
        // example names:
        // - user1.dat
        // - user1.dat.bak1
        // - user1_1.0.30000.dat
        // => Find all files starting with "user{sourceSlotIndex}" followed by a non-digit character
        var sourcePrefix = $"user{sourceSlotIndex}";
        var targetPrefix = $"user{targetSlotIndex}";
        foreach (var file in Directory.GetFiles(saveDirPath))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith(sourcePrefix) && (fileName.Length == sourcePrefix.Length || !char.IsDigit(fileName[sourcePrefix.Length])))
            {
                var targetFile = Path.Combine(saveDirPath, targetPrefix + fileName.Substring(sourcePrefix.Length));
                MovePathInternal(file, targetFile);
            }
        }

        // ========== Move restore points ==========
        var sourceRestoreDir = Path.Combine(saveDirPath, $"Restore_Points{sourceSlotIndex}");
        var targetRestoreDir = Path.Combine(saveDirPath, $"Restore_Points{targetSlotIndex}");
        MovePathInternal(sourceRestoreDir, targetRestoreDir);

        // ========== Move modded save data ==========
        var moddedSourceDir = GetModdedSaveDataDir(sourceSlotIndex);
        var moddedTargetDir = GetModdedSaveDataDir(targetSlotIndex);
        MovePathInternal(moddedSourceDir, moddedTargetDir);
    }

    public static void MovePathInternal(string sourcePath, string targetPath)
    {
        try
        {
            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
            {
                return;
            }
            Directory.Move(sourcePath, targetPath);
            SfmLogger.LogInfo($"Moved \nfrom {sourcePath}\nto   {targetPath}");
        }
        catch (Exception ex)
        {
            SfmLogger.LogError($"Failed to move \nfrom {sourcePath}\nto   {targetPath}\nreason: {ex}");
        }
    }

    public static string GetModdedSaveDataDir(int slotIndex)
    {
        // This is the directory used by the DataManager mod to store modded save data.
        // We don't use DataManager here for two reasons:
        // 1. It does not expose this directory publicly, only subdirectories for specific types of data.
        // 2. DataManager brings in several other dependencies that are not needed for this mod.
        var saveDirPath = ((DesktopPlatform)Platform.Current).saveDirPath;
        return Path.Combine(saveDirPath, "Modded", $"user{slotIndex}");
    }
}