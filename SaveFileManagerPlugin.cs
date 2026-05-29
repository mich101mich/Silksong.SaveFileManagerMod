using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SaveFileManager
{
    [BepInPlugin("io.github.mich101mich.savefilemanager", "Save File Manager", "0.1.0")]
    public partial class SaveFileManagerPlugin : BaseUnityPlugin
    {
        public static SaveFileManagerPlugin Instance { get; private set; } = null!;
        internal static new ManualLogSource Logger { get; private set; } = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo("Save File Manager Mod loaded!");

            Harmony.CreateAndPatchAll(typeof(SaveFileManagerPlugin));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CurrencyManager), "AddShards")]
        public static void AddShards_Postfix(int amount)
        {
            CurrencyManager.ChangeCurrency(amount, CurrencyType.Shard);
            CurrencyManager.ChangeCurrency(amount * 2, CurrencyType.Money);
        }
    }
}
