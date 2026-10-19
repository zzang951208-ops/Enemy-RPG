using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>타이틀 / 일시정지 / 조작법 / 사망 / 엔딩 화면</summary>
    public class MenuScreens : MonoBehaviour
    {
        RectTransform title, pause, controls, death, ending, confirm;
        Button continueBtn;
        Text endingStats, endingLabels, endingTitle, endingStory, endingHint, deathText, confirmText, titleSave;
        Action confirmYes;
        CanvasGroup titleGroup;

        public bool PauseOpen { get { return pause != null && pause.gameObject.activeSelf; } }
        public bool ControlsOpen { get { return controls != null && controls.gameObject.activeSelf; } }
        GameManager GM { get { return GameManager.I; } }

        public const string GameTitle = "새벽빛 전설";

        public void Build(RectTransform parent)
        {
            BuildTitle(parent);
            BuildPause(parent);
            BuildDeath(parent);
            BuildEnding(parent);
            BuildControls(parent);
            BuildConfirm(parent);
        }

        // ─────────────── 공통 ───────────────
        static RectTransform FullDim(Transform parent, string name, Color c)
        {
            var img = UIFactory.Panel(parent, name, c, false);
            UIFactory.Stretch(img.rectTransform);
            img.gameObject.SetActive(false);
            return img.rectTransform;
        }

        static RectTransform MenuColumn(Transform parent, Vector2 pos, float width)
        {
            var col = UIFactory.Rect("Buttons", parent);
            UIFactory.Place(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), pos, new Vector2(width, 400));
            var v = UIFactory.VList(col.gameObject, 12, 0);
            v.childAlignment = TextAnchor.UpperCenter;
            return col;
        }

        static Button MenuButton(Transform col, string text, Action a, Color? c = null)
        {
            var b = UIFactory.Button(col, text, a, c, 26);
            UIFactory.Size(b, -1, 62);
            return b;
        }

        public void HideAll()
        {
            title.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            death.gameObject.SetActive(false);
            ending.gameObject.SetActive(false);
            controls.gameObject.SetActive(false);
            confirm.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

        /// <summary>Esc 처리. 메뉴가 입력을 소비했으면 true</summary>
        public bool HandleEsc()
        {
            if (confirm.gameObject.activeSelf) { confirm.gameObject.SetActive(false); return true; }
            if (ControlsOpen) { controls.gameObject.SetActive(false); return true; }
            if (PauseOpen) { TogglePause(); return true; }
            return false;
        }

        // ─────────────── 타이틀 ───────────────
        void BuildTitle(Transform parent)
        {
            title = FullDim(parent, "Title", new Color(0.02f, 0.03f, 0.08f, 0.55f));
            titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            // 상단/하단 그라데이션 느낌의 띠
            var band = UIFactory.Panel(title, "Band", new Color(0, 0, 0, 0.45f), false);
            UIFactory.Place(band.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(4000, 260));

            var t = UIFactory.Label(title, GameTitle, 112, TextAnchor.MiddleCenter, UIFactory.Gold);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(1400, 150));
            t.fontStyle = FontStyle.Bold;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.35f, 0.15f, 0.02f, 0.9f); o.effectDistance = new Vector2(4, -4);
            var sub = UIFactory.Label(title, "— 고블린 왕과 럭키 잭의 천막 —", 30, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f));
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1200, 44));

            var col = MenuColumn(title, new Vector2(0, 10), 380);
            MenuButton(col, "새 게임", () =>
            {
                if (GameManager.HasSave) Confirm("저장된 모험이 있습니다.\n새 게임을 시작하면 기존 기록이 사라집니다.\n계속할까요?", () => GM.NewGame());
                else GM.NewGame();
            }, new Color(0.7f, 0.45f, 0.15f));
            continueBtn = MenuButton(col, "이어하기", () => { if (GameManager.HasSave) GM.ContinueGame(); });
            MenuButton(col, "조작법", ShowControls);
            MenuButton(col, "종료", Quit, new Color(0.45f, 0.2f, 0.22f));

            titleSave = UIFactory.Label(title, "", 18, TextAnchor.MiddleCenter, UIFactory.TextDim);
            UIFactory.Place(titleSave.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1200, 30));
            var credit = UIFactory.Label(title, "Unity 코드 전용 프로젝트 · 모든 그래픽/사운드 절차적 생성", 16, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.5f));
            UIFactory.Place(credit.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1200, 26));
        }

        public void ShowTitle()
        {
            HideAll();
            title.gameObject.SetActive(true);
            continueBtn.interactable = GameManager.HasSave;
            titleSave.text = GameManager.HasSave ? "저장 데이터: " + GameManager.SaveSummary() : "저장 데이터 없음";
            StartCoroutine(FadeIn(titleGroup, 0.8f));
        }

        static IEnumerator FadeIn(CanvasGroup g, float dur)
        {
            float t = 0;
            g.alpha = 0;
            while (t < dur) { t += Time.unscaledDeltaTime; g.alpha = t / dur; yield return null; }
            g.alpha = 1;
        }

        // ─────────────── 확인 팝업 ───────────────
        void BuildConfirm(Transform parent)
        {
            confirm = FullDim(parent, "Confirm", new Color(0, 0, 0, 0.6f));
            var box = UIFactory.Panel(confirm, "Box", UIFactory.PanelDark);
            UIFactory.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 320));
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.8f, 0.68f, 0.35f, 0.7f);
            confirmText = UIFactory.Label(box.transform, "", 24, TextAnchor.MiddleCenter, UIFactory.TextMain);
            UIFactory.Stretch(confirmText.rectTransform, 30, 30, 30, 110);
            var yes = UIFactory.Button(box.transform, "예", () => { confirm.gameObject.SetActive(false); if (confirmYes != null) confirmYes(); }, new Color(0.7f, 0.3f, 0.25f), 24);
            UIFactory.Place((RectTransform)yes.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-110, 30), new Vector2(190, 60));
            var no = UIFactory.Button(box.transform, "아니오", () => confirm.gameObject.SetActive(false), null, 24);
            UIFactory.Place((RectTransform)no.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(110, 30), new Vector2(190, 60));
        }

        void Confirm(string msg, Action yes)
        {
            confirmText.text = msg;
            confirmYes = yes;
            confirm.gameObject.SetActive(true);
            confirm.SetAsLastSibling();
        }

        // ─────────────── 일시정지 ───────────────
        void BuildPause(Transform parent)
        {
            pause = FullDim(parent, "Pause", new Color(0, 0, 0, 0.6f));
            var t = UIFactory.Label(pause, "일시정지", 64, TextAnchor.MiddleCenter, UIFactory.Gold);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(800, 90));
            t.fontStyle = FontStyle.Bold;
            var col = MenuColumn(pause, new Vector2(0, 170), 380);
            MenuButton(col, "계속하기", TogglePause, new Color(0.25f, 0.5f, 0.35f));
            MenuButton(col, "저장하기", () => { GM.Save(); GM.UI.Hud.Toast("저장되었습니다"); SoundFX.Play(Sfx.Quest, 0.5f); });
            MenuButton(col, "조작법", ShowControls);
            MenuButton(col, "타이틀로", () => Confirm("진행 상황을 저장하고 타이틀로 돌아갈까요?", () => { GM.Save(); GM.GoToTitle(); }));
            MenuButton(col, "게임 종료", () => Confirm("진행 상황을 저장하고 게임을 종료할까요?", () => { GM.Save(); Quit(); }), new Color(0.45f, 0.2f, 0.22f));
        }

        public void TogglePause()
        {
            if (GM.State != GameState.Playing) return;
            bool open = !PauseOpen;
            pause.gameObject.SetActive(open);
            if (!open) { controls.gameObject.SetActive(false); confirm.gameObject.SetActive(false); }
            Time.timeScale = open ? 0f : 1f;
            if (!open) GM.UI.MarkClosed();
        }

        // ─────────────── 조작법 ───────────────
        void BuildControls(Transform parent)
        {
            controls = FullDim(parent, "Controls", new Color(0, 0, 0, 0.5f));
            var box = UIFactory.Panel(controls, "Box", UIFactory.PanelDark);
            UIFactory.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 700));
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.45f, 0.55f, 0.8f, 0.6f);
            var h = UIFactory.Label(box.transform, "조작법", 40, TextAnchor.MiddleCenter, UIFactory.Gold);
            UIFactory.Place(h.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(800, 56));
            string keys =
                "WASD / 방향키\nSpace / J / 좌클릭\nR\nShift\n1 / 2\nE / F\nI / Tab\nQ\nEsc";
            string descs =
                "이동\n공격\n회전베기 (MP 15, 주변 적 전체 공격)\n대시 (사용 중 무적)\nHP 포션 / MP 포션\nNPC와 대화\n캐릭터 & 인벤토리\n퀘스트 일지\n창 닫기 / 일시정지";
            var kl = UIFactory.Label(box.transform, keys, 24, TextAnchor.UpperLeft, UIFactory.Gold);
            UIFactory.Stretch(kl.rectTransform, 80, 560, 100, 250);
            kl.lineSpacing = 1.2f;
            var dl = UIFactory.Label(box.transform, descs, 24, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch(dl.rectTransform, 360, 40, 100, 250);
            dl.lineSpacing = 1.2f;
            var tips = UIFactory.Label(box.transform,
                "· 머리 위 <color=#FFD24D>!</color> = 새 퀘스트,  <color=#7CFF8A>?</color> = 보고 가능\n" +
                "· 마을 안에서는 HP/MP가 빠르게 회복됩니다\n" +
                "· 여관에서 저장 · 퀘스트 완료/도박 후 자동 저장", 20, TextAnchor.UpperLeft, UIFactory.TextDim);
            UIFactory.Stretch(tips.rectTransform, 80, 40, 470, 100);
            tips.lineSpacing = 1.2f;
            var close = UIFactory.Button(box.transform, "닫기", () => controls.gameObject.SetActive(false), null, 24);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(220, 60));
        }

        void ShowControls()
        {
            controls.gameObject.SetActive(true);
            controls.SetAsLastSibling();
        }

        // ─────────────── 사망 ───────────────
        void BuildDeath(Transform parent)
        {
            death = FullDim(parent, "Death", new Color(0.25f, 0f, 0f, 0.6f));
            var t = UIFactory.Label(death, "쓰러졌습니다...", 80, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.4f));
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1200, 110));
            t.fontStyle = FontStyle.Bold;
            deathText = UIFactory.Label(death, "", 26, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Place(deathText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1200, 80));
            var b = UIFactory.Button(death, "마을에서 부활하기", () => GM.Revive(), new Color(0.6f, 0.25f, 0.25f), 28);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(380, 70));
        }

        public void ShowDeath(int goldLoss)
        {
            HideAll();
            deathText.text = "새벽빛 마을의 치유사가 당신을 구해줄 것입니다.\n치료비로 " + UIFactory.Col(goldLoss + " G", UIFactory.Gold) + " (소지금의 10%)를 잃습니다.";
            death.gameObject.SetActive(true);
            var g = death.GetComponent<CanvasGroup>();
            if (g == null) g = death.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(FadeIn(g, 1.2f));
        }

        // ─────────────── 엔딩 ───────────────
        void BuildEnding(Transform parent)
        {
            ending = FullDim(parent, "Ending", new Color(0.02f, 0.02f, 0.06f, 0.82f));
            endingTitle = UIFactory.Label(ending, "", 58, TextAnchor.MiddleCenter, UIFactory.Gold);
            UIFactory.Place(endingTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1600, 80));
            endingTitle.fontStyle = FontStyle.Bold;
            endingTitle.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.15f, 0, 0.9f);
            endingStory = UIFactory.Label(ending, "",
                24, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.95f));
            UIFactory.Place(endingStory.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 245), new Vector2(1500, 80));

            var panel = UIFactory.Panel(ending, "Stats", UIFactory.PanelMid);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(900, 380));
            var eh = UIFactory.Label(panel.transform, "모험 기록", 30, TextAnchor.UpperCenter, UIFactory.Gold);
            UIFactory.Stretch(eh.rectTransform, 0, 0, 22, 0);
            eh.fontStyle = FontStyle.Bold;
            endingLabels = UIFactory.Label(panel.transform, "", 24, TextAnchor.UpperLeft, UIFactory.TextDim);
            UIFactory.Stretch(endingLabels.rectTransform, 90, 500, 80, 50);
            endingLabels.lineSpacing = 1.3f;
            endingStats = UIFactory.Label(panel.transform, "", 24, TextAnchor.UpperLeft, UIFactory.Gold);
            UIFactory.Stretch(endingStats.rectTransform, 360, 30, 80, 50);
            endingStats.lineSpacing = 1.3f;
            endingHint = UIFactory.Label(panel.transform, "", 18, TextAnchor.LowerCenter, UIFactory.TextDim);
            UIFactory.Stretch(endingHint.rectTransform, 0, 0, 0, 16);

            var b1 = UIFactory.Button(ending, "계속 플레이하기", () => GM.ContinueAfterEnding(), new Color(0.25f, 0.5f, 0.35f), 26);
            UIFactory.Place((RectTransform)b1.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-170, -250), new Vector2(300, 66));
            var b2 = UIFactory.Button(ending, "타이틀로", () => { GM.Save(); GM.GoToTitle(); }, null, 26);
            UIFactory.Place((RectTransform)b2.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170, -250), new Vector2(300, 66));
        }

        public void ShowEnding(bool trueEnding)
        {
            HideAll();
            if (trueEnding)
            {
                endingTitle.text = "진 엔딩 — 새벽빛이 모든 어둠을 걷어내다";
                endingTitle.color = new Color(0.85f, 0.7f, 1f);
                endingStory.text = "망령 군주 모르가스가 쓰러지자 폐허를 뒤덮던 저주가 새벽 안개처럼 흩어졌다.\n학자 엘라라는 두 번 세상을 구한 모험가의 이야기를 한 권의 책으로 엮었다.";
                endingHint.text = "학자 엘라라에게 보고하면 마지막 보상을 받을 수 있습니다. 모든 이야기를 완주했습니다!";
            }
            else
            {
                endingTitle.text = "새벽빛 마을에 평화가 찾아왔습니다";
                endingTitle.color = UIFactory.Gold;
                endingStory.text = "고블린 왕 그락이 쓰러지자 야영지의 고블린들은 뿔뿔이 흩어졌다.\n그러나 그락이 사라진 제단 북쪽에서, 오래된 봉인이 조용히 풀리고 있었다...";
                endingHint.text = "촌장에게 보고하고, 학자 엘라라를 찾아가 보세요. 이야기는 아직 끝나지 않았습니다.";
            }
            var s = GM.Stats; var g = GM.Gamble;
            int secs = Mathf.FloorToInt(GM.PlayTime);
            string time = (secs / 3600) > 0 ? string.Format("{0}시간 {1}분 {2}초", secs / 3600, secs / 60 % 60, secs % 60) : string.Format("{0}분 {1}초", secs / 60, secs % 60);
            int doneQuests = 0;
            foreach (var q in GM.Quests.quests) if (q.state == QuestState.Done || q.state == QuestState.ReadyToTurnIn) doneQuests++;
            string net = g.Net >= 0 ? UIFactory.Col("+" + g.Net + "G", UIFactory.Good) : UIFactory.Col(g.Net + "G", UIFactory.Bad);
            endingLabels.text = "플레이 시간\n최종 레벨\n처치한 몬스터\n달성한 퀘스트\n보유 골드\n도박 전적";
            endingStats.text =
                time + "\n" +
                "Lv." + s.level + "\n" +
                s.kills + "마리\n" +
                doneQuests + " / " + GM.Quests.quests.Count + "\n" +
                s.gold.ToString("N0") + " G\n" +
                g.gamesPlayed + "판 " + g.wins + "승  (순이익 " + net + ")";
            ending.gameObject.SetActive(true);
            var cg = ending.GetComponent<CanvasGroup>();
            if (cg == null) cg = ending.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(FadeIn(cg, 1.5f));
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
