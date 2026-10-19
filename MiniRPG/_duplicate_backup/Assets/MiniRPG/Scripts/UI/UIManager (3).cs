using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniRPG
{
    public class UIManager : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform canvasRect, worldLayer, windowLayer, overlayLayer;
        public HUD Hud;
        public DialogueUI Dialogue;
        public ShopUI Shop;
        public InventoryUI Inventory;
        public QuestUI QuestLog;
        public GambleUI Gamble;
        public MenuScreens Menus;
        Image fade;
        int lastCloseFrame = -10;

        public static UIManager Build(GameManager gm, RenderTexture minimapRT)
        {
            EnsureEventSystem();
            var go = new GameObject("UI");
            var ui = go.AddComponent<UIManager>();
            ui.canvas = go.AddComponent<Canvas>();
            ui.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ui.canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            ui.canvasRect = (RectTransform)go.transform;

            ui.worldLayer = UIFactory.Stretch(UIFactory.Rect("WorldLayer", go.transform));
            gm.Fx.worldLayer = ui.worldLayer;
            var hudRoot = UIFactory.Stretch(UIFactory.Rect("HUD", go.transform));
            ui.windowLayer = UIFactory.Stretch(UIFactory.Rect("Windows", go.transform));
            ui.overlayLayer = UIFactory.Stretch(UIFactory.Rect("Overlays", go.transform));

            ui.Hud = hudRoot.gameObject.AddComponent<HUD>();
            ui.Hud.Build(hudRoot, minimapRT);

            ui.Dialogue = Make<DialogueUI>(ui.windowLayer, "Dialogue");
            ui.Dialogue.Build(ui.windowLayer);
            ui.Shop = Make<ShopUI>(ui.windowLayer, "Shop");
            ui.Shop.Build(ui.windowLayer);
            ui.Inventory = Make<InventoryUI>(ui.windowLayer, "Inventory");
            ui.Inventory.Build(ui.windowLayer);
            ui.QuestLog = Make<QuestUI>(ui.windowLayer, "QuestLog");
            ui.QuestLog.Build(ui.windowLayer);
            ui.Gamble = Make<GambleUI>(ui.windowLayer, "Gamble");
            ui.Gamble.Build(ui.windowLayer);
            ui.Menus = Make<MenuScreens>(ui.overlayLayer, "Menus");
            ui.Menus.Build(ui.overlayLayer);

            ui.fade = UIFactory.Panel(go.transform, "Fade", new Color(0, 0, 0, 0), false);
            UIFactory.Stretch(ui.fade.rectTransform);
            ui.fade.raycastTarget = false;
            return ui;
        }

        static T Make<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name + "Controller");
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        /// <summary>
        /// 클릭 입력을 받기 위한 EventSystem 준비.
        /// 씬에 이미 있는 EventSystem이 현재 입력 설정과 맞지 않는 모듈을 쓰고 있으면 교체한다.
        /// (Unity 6 기본값인 'Input System Package (New)' 설정에서 StandaloneInputModule을 쓰면 매 프레임 예외가 발생함)
        /// </summary>
        static void EnsureEventSystem()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                foreach (var found in Resources.FindObjectsOfTypeAll<EventSystem>())
                    if (found != null && found.gameObject.scene.IsValid()) { es = found; break; }
            }
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }
            if (!es.gameObject.activeSelf) es.gameObject.SetActive(true);
            if (!es.enabled) es.enabled = true;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (es.GetComponent<BaseInputModule>() == null) es.gameObject.AddComponent<StandaloneInputModule>();
#elif ENABLE_INPUT_SYSTEM
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null) Object.DestroyImmediate(legacy);
            var module = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module == null) module = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            // 코드로 추가한 모듈은 기본 UI 액션(클릭·포인트)이 비어 있으므로 반드시 지정해야 버튼이 눌린다
            if (module.actionsAsset == null) module.AssignDefaultActions();
#endif
        }

        public bool AnyModalOpen
        {
            get
            {
                return Dialogue.IsOpen || Shop.IsOpen || Inventory.IsOpen || QuestLog.IsOpen || Gamble.IsOpen || Menus.PauseOpen
                       || Time.frameCount - lastCloseFrame < 2;
            }
        }

        public void MarkClosed() { lastCloseFrame = Time.frameCount; }

        /// <summary>Esc: 가장 위의 창을 닫음. 닫을 창이 없으면 false</summary>
        public bool CloseTop()
        {
            UIWindow[] order = { Shop, Gamble, Inventory, QuestLog, Dialogue };
            foreach (var w in order)
            {
                if (!w.IsOpen) continue;
                if (w.CanClose) w.Close();
                return true;
            }
            return false;
        }

        public void CloseAllWindows()
        {
            UIWindow[] all = { Shop, Gamble, Inventory, QuestLog, Dialogue };
            foreach (var w in all) if (w.IsOpen) w.Close();
        }

        public void FadeFlash() { StartCoroutine(FadeRoutine()); }

        IEnumerator FadeRoutine()
        {
            float t = 0;
            while (t < 1.2f)
            {
                t += Time.unscaledDeltaTime;
                float a = t < 0.5f ? t / 0.5f : t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.5f;
                fade.color = new Color(0, 0, 0, Mathf.Clamp01(a) * 0.9f);
                yield return null;
            }
            fade.color = new Color(0, 0, 0, 0);
        }
    }

}
