using System.Collections;
using Silksong.ModMenu.Elements;
using UnityEngine;

namespace SaveFileManagerMod.UI;

public class ArchiveMenuEntry : TextButton
{
    public struct SaveInfo
    {
        public string name;
        public string area;
        public int? percentComplete;
        public string timePlayed;
    }

    public int m_index;

    public bool m_disposed = false;

    SaveInfo? m_saveInfo = null;

    string? m_errorMessage = null;

    public ArchiveMenuEntry(int index)
        : base($"{index}. Loading...")
    {
        m_index = index;

        GameManager.instance.GetSaveStatsForSlot(m_index, OnSaveStatsReceived);
    }

    public void OnSaveStatsReceived(SaveStats stats, string message)
    {
        if (message != null)
        {
            SfmLogger.LogInfo($"Failed to get save stats for slot {m_index}: {message}");
            m_saveInfo = null;
            m_errorMessage = message;
            UpdateText();
            return;
        }
        if (stats == null)
        {
            m_saveInfo = null;
            UpdateText();
            return;
        }

        string name;
        if (!SaveName.GetOrLoadSlotName(m_index, out name))
        {
            name = "(Unnamed)";
        }

        m_saveInfo = new SaveInfo
        {
            name = name,
            area = GameManager.GetFormattedMapZoneStringV2(stats.MapZone),
            percentComplete = stats.UnlockedCompletionRate ? (int?)stats.CompletionPercentage : null,
            timePlayed = stats.GetPlaytimeHHMM(),
        };
        UpdateText();
    }

    public void UpdateText()
    {
        string buttonText;
        if (m_errorMessage != null)
        {
            buttonText = "Error"; // TODO: Localize
            base.DescriptionText.LocalizedText = m_errorMessage;
        }
        else if (m_saveInfo is SaveInfo info)
        {
            if (info.percentComplete.HasValue)
            {
                buttonText = $"{info.name}  {info.area}  {info.percentComplete.Value}%  {info.timePlayed}";
            }
            else
            {
                buttonText = $"{info.name}  {info.area}  {info.timePlayed}";
            }
            base.DescriptionText.LocalizedText = "";
        }
        else
        {
            buttonText = "(Empty)";
            base.DescriptionText.LocalizedText = "";
        }

        base.ButtonText.LocalizedText = $"{m_index}. {buttonText}";
    }
}
