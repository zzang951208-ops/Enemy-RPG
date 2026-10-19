using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniRPG
{
    // ═══════════════════════════════ HUD ═══════════════════════════════
    public class HUD : MonoBehaviour
    {
        RectTransform root;
        UIBar hpBar, mpBar, expBar, bossBar;
        Text levelText, goldText, zoneText, zoneSubText, questTracker, promptText, bossName;
        RectTransform promptRoot, bossRoot, toastStack;
        Text bannerTitle, bannerSub;
        CanvasGroup bannerGroup;
        readonly Queue<KeyValuePair<string, string>> bannerQueue = new Queue<KeyValuePair<string, string>>();
        bool bannerBusy;

        // 액션바
        RectTransform skillCdMask, dashCdMask;
        Text potion1Count, potion2Count;

        public void Build(RectTransform r, RenderTexture minimapRT)
        {
            root = r;
            group = r.gameObject.AddComponent<CanvasGroup>();
            var gold = UIFactory.Gold;

            // ── 좌상단: 캐릭터 상태 ──
            var status = UIFactory.Panel(r, "Status", UIFactory.PanelDark);
            UIFactory.Place(status.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(430, 158));
            var portrait = UIFactory.Panel(status.transform, "Portrait", new Color(0.25f, 0.35f, 0.6f));
            portrait.sprite = UIFactory.Circle; portrait.type = Image.Type.Simple;
            UIFactory.Place(portrait.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(92, 92));
            var ring = UIFactory.Panel(portrait.transform, "Ring", gold);
            ring.sprite = UIFactory.Ring; ring.type = Image.Type.Simple;
            UIFactory.Stretch(ring.rectTransform, -3, -3, -3, -3);
            levelText = UIFactory.Label(portrait.transform, "Lv.1", 26, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Stretch(levelText.rectTransform);
            levelText.fontStyle = FontStyle.Bold;
            var nameT = UIFactory.Label(status.transform, "모험가", 20, TextAnchor.MiddleLeft, gold);
            UIFactory.Place(nameT.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -10), new Vector2(200, 28));
            goldText = UIFactory.Label(status.transform, "0 G", 20, TextAnchor.MiddleRight, gold);
            UIFactory.Place(goldText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -10), new Vector2(180, 28));
            hpBar = new UIBar(status.transform, new Color(0.85f, 0.22f, 0.25f), new Vector2(294, 26));
            UIFactory.Place(hpBar.root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -42), new Vector2(294, 26));
            mpBar = new UIBar(status.transform, new Color(0.25f, 0.45f, 0.95f), new Vector2(294, 20));
            UIFactory.Place(mpBar.root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -74), new Vector2(294, 20));
            expBar = new UIBar(status.transform, new Color(0.95f, 0.78f, 0.25f), new Vector2(400, 14), true, 12);
            UIFactory.Place(expBar.root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -122), new Vector2(400, 16));

            // ── 우상단: 미니맵 ──
            var mapFrame = UIFactory.Panel(r, "MinimapFrame", UIFactory.PanelDark);
            mapFrame.sprite = UIFactory.Circle; mapFrame.type = Image.Type.Simple;
            UIFactory.Place(mapFrame.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(236, 236));
            var mask = UIFactory.Panel(mapFrame.transform, "Mask", Color.white);
            mask.sprite = UIFactory.Circle; mask.type = Image.Type.Simple;
            UIFactory.Stretch(mask.rectTransform, 8, 8, 8, 8);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var raw = UIFactory.Rect("Map", mask.transform).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(raw.rectTransform);
            raw.texture = minimapRT;
            raw.raycastTarget = false;
            var mapRing = UIFactory.Panel(mapFrame.transform, "Ring", new Color(0.8f, 0.7f, 0.4f));
            mapRing.sprite = UIFactory.Ring; mapRing.type = Image.Type.Simple;
            UIFactory.Stretch(mapRing.rectTransform, 4, 4, 4, 4);
            var north = UIFactory.Label(mapFrame.transform, "N", 18, TextAnchor.MiddleCenter, gold);
            UIFactory.Place(north.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(30, 24));
            zoneText = UIFactory.Label(r, "", 22, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Place(zoneText.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-142, -262), new Vector2(300, 28));
            zoneSubText = UIFactory.Label(r, "", 15, TextAnchor.MiddleCenter, UIFactory.TextDim);
            UIFactory.Place(zoneSubText.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-142, -288), new Vector2(300, 22));

            // ── 퀘스트 추적 ──
            var qt = UIFactory.Panel(r, "QuestTracker", new Color(0, 0, 0, 0.35f));
            UIFactory.Place(qt.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -320), new Vector2(360, 230));
            questTracker = UIFactory.Label(qt.transform, "", 17, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(questTracker.rectTransform, 14, 10, 10, 10);

            // ── 하단 액션바 ──
            var bar = UIFactory.Panel(r, "ActionBar", UIFactory.PanelDark);
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(470, 96));
            UIFactory.HList(bar.gameObject, 10, 9);
            Slot(bar.transform, "Space", "공격", new Color(0.55f, 0.3f, 0.25f));
            skillCdMask = Slot(bar.transform, "R", "회전베기", new Color(0.25f, 0.4f, 0.65f));
            dashCdMask = Slot(bar.transform, "Shift", "대시", new Color(0.3f, 0.5f, 0.35f));
            potion1Count = SlotCount(Slot(bar.transform, "1", "HP 포션", new Color(0.65f, 0.2f, 0.25f)));
            potion2Count = SlotCount(Slot(bar.transform, "2", "MP 포션", new Color(0.2f, 0.3f, 0.7f)));

            var help = UIFactory.Label(r, "<color=#FFD24D>I</color> 인벤토리   <color=#FFD24D>Q</color> 퀘스트   <color=#FFD24D>E</color> 대화   <color=#FFD24D>Esc</color> 메뉴",
                                       17, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Place(help.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(22, 18), new Vector2(560, 30));

            // ── 상호작용 안내 ──
            var pr = UIFactory.Panel(r, "Prompt", new Color(0.05f, 0.05f, 0.1f, 0.85f));
            promptRoot = pr.rectTransform;
            UIFactory.Place(promptRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(380, 48));
            promptText = UIFactory.Label(pr.transform, "", 20, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Stretch(promptText.rectTransform);
            promptRoot.gameObject.SetActive(false);

            // ── 보스 체력바 ──
            bossRoot = UIFactory.Rect("BossBar", r);
            UIFactory.Place(bossRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(720, 70));
            bossName = UIFactory.Label(bossRoot, "", 24, TextAnchor.MiddleCenter, new Color(1f, 0.75f, 1f));
            UIFactory.Place(bossName.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(720, 30));
            bossName.fontStyle = FontStyle.Bold;
            bossBar = new UIBar(bossRoot, new Color(0.75f, 0.2f, 0.85f), new Vector2(720, 26));
            UIFactory.Place(bossBar.root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(720, 26));
            bossRoot.gameObject.SetActive(false);

            // ── 토스트 ──
            toastStack = UIFactory.Rect("Toasts", r);
            UIFactory.Place(toastStack, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(760, 300));
            var vl = UIFactory.VList(toastStack.gameObject, 6, 0);
            vl.childForceExpandWidth = false;

            // ── 배너 ──
            var banner = UIFactory.Rect("Banner", r);
            UIFactory.Place(banner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(1000, 140));
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0; bannerGroup.blocksRaycasts = false; bannerGroup.interactable = false;
            var bbg = UIFactory.Panel(banner, "BG", new Color(0, 0, 0, 0.45f), false);
            UIFactory.Stretch(bbg.rectTransform, 0, 0, 16, 16);
            bannerTitle = UIFactory.Label(banner, "", 46, TextAnchor.MiddleCenter, gold);
            bannerTitle.fontStyle = FontStyle.Bold;
            UIFactory.Place(bannerTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 18), new Vector2(1000, 60));
            bannerTitle.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            bannerSub = UIFactory.Label(banner, "", 22, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Place(bannerSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1000, 34));
        }

        RectTransform Slot(Transform parent, string key, string label, Color c)
        {
            var s = UIFactory.Panel(parent, "Slot_" + key, c);
            UIFactory.Size(s, 80, 78);
            var name = UIFactory.Label(s.transform, label, 15, TextAnchor.LowerCenter, Color.white);
            UIFactory.Stretch(name.rectTransform, 2, 2, 2, 6);
            var k = UIFactory.Label(s.transform, key, 14, TextAnchor.UpperLeft, UIFactory.Gold);
            UIFactory.Stretch(k.rectTransform, 6, 4, 4, 4);
            var cd = UIFactory.Panel(s.transform, "Cooldown", new Color(0, 0, 0, 0.65f));
            var cr = cd.rectTransform;
            cr.anchorMin = Vector2.zero; cr.anchorMax = new Vector2(1, 0); cr.offsetMin = Vector2.zero; cr.offsetMax = Vector2.zero;
            return cr;
        }

        Text SlotCount(RectTransform cdMask)
        {
            cdMask.gameObject.SetActive(false);
            var t = UIFactory.Label(cdMask.parent, "0", 22, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Stretch(t.rectTransform, 2, 2, 2, 14);
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Stats == null) return;
            var s = gm.Stats;
            float dt = Time.unscaledDeltaTime;

            levelText.text = "Lv." + s.level;
            goldText.text = s.gold.ToString("N0") + " G";
            hpBar.Set(s.hp / (float)s.MaxHP, s.hp + " / " + s.MaxHP);
            mpBar.Set(s.mp / (float)s.MaxMP, s.mp + " / " + s.MaxMP);
            expBar.Set(s.exp / (float)s.ExpToNext, "EXP " + s.exp + " / " + s.ExpToNext);
            hpBar.Tick(dt); mpBar.Tick(dt); expBar.Tick(dt);

            var p = gm.Player;
            if (p != null)
            {
                skillCdMask.anchorMax = new Vector2(1, p.SkillCdRatio);
                dashCdMask.anchorMax = new Vector2(1, p.DashCdRatio);
                int zone = WorldBuilder.ZoneAt(p.transform.position);
                zoneText.text = WorldBuilder.ZoneName[zone];
                zoneSubText.text = WorldBuilder.ZoneSub[zone];

                bool showPrompt = p.NearbyNPC != null && !gm.InputBlocked;
                if (promptRoot.gameObject.activeSelf != showPrompt) promptRoot.gameObject.SetActive(showPrompt);
                if (showPrompt) promptText.text = "<color=#FFD24D>[E]</color> " + p.NearbyNPC.displayName + " 대화하기";
            }
            potion1Count.text = (gm.Inv.Count("potion_s") + gm.Inv.Count("potion_l")).ToString();
            potion2Count.text = gm.Inv.Count("mana_potion").ToString();

            // 보스 바
            var boss = gm.ActiveBoss;
            bool showBoss = boss != null && !boss.IsDead && (boss.Aggro || boss.HpRatio < 1f) && gm.State == GameState.Playing;
            if (bossRoot.gameObject.activeSelf != showBoss) bossRoot.gameObject.SetActive(showBoss);
            if (showBoss)
            {
                bossName.text = boss.Def.name;
                bossBar.Set(boss.HpRatio, boss.HP + " / " + boss.Def.hp);
                bossBar.Tick(dt);
            }

            // 퀘스트 추적
            var sb = new System.Text.StringBuilder();
            sb.Append("<color=#FFD24D><b>퀘스트</b></color>  <size=13><color=#9FB2CC>(Q)</color></size>\n");
            int shown = 0;
            foreach (var q in gm.Quests.quests)
            {
                if (q.state != QuestState.Active && q.state != QuestState.ReadyToTurnIn) continue;
                if (shown >= 4) break;
                shown++;
                string tag = q.main ? "<color=#FFB84D>[메인]</color> " : "<color=#8CC8FF>[서브]</color> ";
                sb.Append(tag).Append(q.title).Append('\n');
                if (q.state == QuestState.ReadyToTurnIn) sb.Append("  <color=#7CFF8A>완료! 의뢰인에게 보고하세요</color>\n");
                else sb.Append("  <color=#C8D0DC>").Append(q.ProgressText).Append("</color>\n");
            }
            if (shown == 0)
            {
                var avail = gm.Quests.quests.Find(x => x.main && x.state == QuestState.Available);
                string giverName = "촌장 에드릭";
                if (avail != null) foreach (var n in NPC.All) if (n != null && n.id == avail.giver) giverName = n.displayName;
                sb.Append(avail != null ? "<color=#C8D0DC>" + giverName + "에게 말을 걸어보세요 <color=#FFD24D>(!)</color></color>"
                                        : "<color=#C8D0DC>진행 중인 퀘스트 없음</color>");
            }
            questTracker.text = sb.ToString();
        }

        // ── 알림 ──
        public void Toast(string msg)
        {
            if (toastStack == null) return;
            if (toastStack.childCount >= 5) Destroy(toastStack.GetChild(0).gameObject);
            var p = UIFactory.Panel(toastStack, "Toast", new Color(0.05f, 0.06f, 0.1f, 0.85f));
            var t = UIFactory.Label(p.transform, msg, 19, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Stretch(t.rectTransform, 18, 18, 6, 6);
            var fit = p.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(20, 20, 8, 8);
            fit.childControlWidth = true; fit.childControlHeight = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var cg = p.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            StartCoroutine(ToastRoutine(cg));
        }

        IEnumerator ToastRoutine(CanvasGroup cg)
        {
            float t = 0;
            while (t < 3.4f && cg != null)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = t < 0.2f ? t / 0.2f : t > 2.8f ? 1f - (t - 2.8f) / 0.6f : 1f;
                yield return null;
            }
            if (cg != null) Destroy(cg.gameObject);
        }

        public void Banner(string title, string sub)
        {
            bannerQueue.Enqueue(new KeyValuePair<string, string>(title, sub));
            if (!bannerBusy) StartCoroutine(BannerRoutine());
        }

        IEnumerator BannerRoutine()
        {
            bannerBusy = true;
            while (bannerQueue.Count > 0)
            {
                var b = bannerQueue.Dequeue();
                bannerTitle.text = b.Key;
                bannerSub.text = b.Value;
                float t = 0;
                while (t < 2.4f)
                {
                    t += Time.unscaledDeltaTime;
                    bannerGroup.alpha = t < 0.25f ? t / 0.25f : t > 1.9f ? 1f - (t - 1.9f) / 0.5f : 1f;
                    float sc = t < 0.25f ? Mathf.Lerp(1.3f, 1f, t / 0.25f) : 1f;
                    bannerTitle.rectTransform.localScale = Vector3.one * sc;
                    yield return null;
                }
                bannerGroup.alpha = 0;
            }
            bannerBusy = false;
        }

        CanvasGroup group;
        public void SetVisible(bool v) { group.alpha = v ? 1f : 0f; group.blocksRaycasts = v; }
    }
}
