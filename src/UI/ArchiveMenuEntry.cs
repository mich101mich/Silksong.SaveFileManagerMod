using System.Collections;
using Silksong.ModMenu.Elements;
using UnityEngine;

namespace SaveFileManagerMod.UI;

public class ArchiveMenuEntry : TextButton
{
    public int m_index;

    public ArchiveMenuEntry(int slotIndex, SaveStats? stats, string? message)
        : base(MakeTitle(slotIndex, stats, message), message ?? "")
    {
        m_index = slotIndex;
    }

    public static string MakeTitle(int slotIndex, SaveStats? stats, string? message)
    {
        string buttonText;
        if (message != null)
        {
            buttonText = "Error"; // TODO: Localize
        }
        else if (stats != null)
        {
            string name;
            if (!SaveName.GetOrLoadSlotName(slotIndex, out name))
            {
                name = "(Unnamed)";
            }

            var area = GameManager.GetFormattedMapZoneStringV2(stats.MapZone);
            var timePlayed = stats.GetPlaytimeHHMM();

            if (stats.UnlockedCompletionRate)
            {
                buttonText = $"{name}  {area}  {stats.CompletionPercentage}%  {timePlayed}";
            }
            else
            {
                buttonText = $"{name}  {area}  {timePlayed}";
            }
        }
        else
        {
            buttonText = "(Empty)";
        }

        return $"{slotIndex}. {buttonText}";
    }
}
