using System.Collections;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SaveFileManagerMod
{
    [BepInAutoPlugin(id: "io.github.mich101mich.savefilemanagermod")]
    public partial class SaveFileManagerPlugin : BaseUnityPlugin
    {
        public static SaveFileManagerPlugin s_instance { get; private set; } = null!;
        internal static ManualLogSource s_logger { get; private set; } = null!;
        private Harmony m_harmony = null!;
        private GameObject m_canvas = null!;

        private void Awake()
        {
            s_instance = this;
            s_logger = base.Logger;
            s_logger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");


            m_harmony = new Harmony($"harmony-{Id}");
            m_harmony.PatchAll(typeof(SaveFileManagerPlugin));

            m_canvas = new GameObject("SaveFileManagerModCanvas");
            m_canvas.SetActive(false);

            m_canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            // m_canvas.AddComponent<GraphicRaycaster>();

            RectTransform rt = m_canvas.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(Screen.width, Screen.height);

            DontDestroyOnLoad(m_canvas);
        }

        private void OnDestroy()
        {
            s_logger.LogInfo($"Plugin {Name} ({Id}) is unloading...");
            m_harmony.UnpatchSelf();
            m_canvas.SetActive(false);
            s_instance = null!;
        }

        public void Update()
        {
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CurrencyManager), "AddShards")]
        public static void AddShards_Postfix(int amount)
        {
            s_logger.LogInfo($"Added {amount} shards");
            // CurrencyManager.ChangeCurrency(amount, CurrencyType.Shard);
            // CurrencyManager.ChangeCurrency(amount * 2, CurrencyType.Money);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIManager), "UIGoToProfileMenu")]
        public static void UIGoToProfileMenu_Postfix()
        {
            s_logger.LogInfo("Navigated to profile menu!");

            // Add a white rectangle in the center of the screen
            var rect = new GameObject("TestRect");
            rect.AddComponent<CanvasRenderer>();
            var rectTransform = rect.AddComponent<RectTransform>();
            rectTransform.SetParent(s_instance.m_canvas.transform, false);

            rectTransform.sizeDelta = new UnityEngine.Vector2(200, 100);
            var image = rect.AddComponent<UnityEngine.UI.Image>();
            image.color = UnityEngine.Color.white;

            s_instance.m_canvas.SetActive(true);
        }
    }
}
