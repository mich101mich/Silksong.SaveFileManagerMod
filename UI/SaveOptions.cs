using BepInEx;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class SaveOptions : IDisposable
{
    internal static ManualLogSource s_logger => SaveFileManagerPlugin.s_logger;
    public AbstractMenuScreen m_menu = null!;
    SaveSlotButton m_saveSlotButton;
    TextButton m_optionsButton;

    RestoreSaveButton m_restoreSaveButton;

    public SaveOptions(SaveSlotButton button)
    {
        s_logger.LogInfo($"Creating SaveOptions for {button.name}");
        m_saveSlotButton = button;
        m_menu = CreateMenu();

        m_restoreSaveButton = typeof(SaveSlotButton)
            .GetField("restoreSaveButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(m_saveSlotButton) as RestoreSaveButton
            ?? throw new Exception("Could not find restoreSaveButton");

        m_saveSlotButton.clearSaveButton.transform.SetLocalPositionY(10000);
        m_restoreSaveButton.transform.SetLocalPositionY(10000);

        m_optionsButton = new TextButton("Options")
        {
            OnSubmit = () =>
            {
                MenuScreenNavigation.Show(m_menu);
            }
        };
        m_optionsButton.RectTransform.SetParent(m_saveSlotButton.transform, false);
        m_optionsButton.RectTransform.SetLocalPosition2D(0, -470);
    }

    public void Dispose()
    {
        s_logger.LogInfo($"Disposing SaveOptions for {m_saveSlotButton.name}");
        m_menu.Dispose();

        m_saveSlotButton.clearSaveButton.transform.SetLocalPositionY(-400);
        m_restoreSaveButton.transform.SetLocalPositionY(-400);

        m_optionsButton.Dispose();
    }

    public AbstractMenuScreen CreateMenu()
    {
        s_logger.LogInfo("Building custom menu!");
        PaginatedMenuScreenBuilder builder = new("Save File Manager");

        TextInput<string> nameField = new TextInput<string>("Name", TextModels.ForStrings(), "Save file name");
        builder.Add(nameField);

        return builder.Build();
    }
}
