using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>퀘스트 일지</summary>
    public class QuestUI : UIWindow
    {
        RectTransform list;
        Text detailText;
        ScrollRect scroll;
        string selected;

        GameManager GM { get { return GameManager.I; } }

        public void Build(RectTransform parent)
        {
            RectTransform body; Text title;
            root = UIFactory.Window(parent, "퀘스트 일지  (Q)", new Vector2(1100, 680), Close, out body, out title);

            var left = UIFactory.Rect("List", body);
            left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0.42f, 1);
            left.offsetMin = Vector2.zero; left.offsetMax = new Vector2(-8, 0);
            list = UIFactory.ScrollList(left, out scroll, 5);
            UIFactory.Stretch((RectTransform)scroll.transform);

            var right = UIFactory.Panel(body, "Detail", UIFactory.PanelMid);
            var rr = right.rectTransform;
            rr.anchorMin = new Vector2(0.42f, 0); rr.anchorMax = new Vector2(1, 1);
            rr.offsetMin = new Vector2(8, 0); rr.offsetMax = Vector2.zero;
            detailText = UIFactory.Label(right.transform, "", 21, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch(detailText.rectTransform, 24, 24, 22, 22);
            detailText.lineSpacing = 1.25f;
            root.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (GM.UI.Dialogue.IsOpen || GM.UI.Shop.IsOpen || GM.UI.Gamble.IsOpen) return;
            if (GM.UI.Inventory.IsOpen) GM.UI.Inventory.Close();
            ShowRoot();
            Refresh();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        static int Order(Quest q)
        {
            switch (q.state)
            {
                case QuestState.ReadyToTurnIn: return 0;
                case QuestState.Active: return 1;
                case QuestState.Available: return 2;
                case QuestState.Done: return 3;
                default: return 4;
            }
        }

        void Refresh()
        {
            UIFactory.Clear(list);
            var qs = new System.Collections.Generic.List<Quest>();
            foreach (var q in GM.Quests.quests) if (q.state != QuestState.Locked) qs.Add(q);
            qs.Sort((a, b) =>
            {
                int c = Order(a).CompareTo(Order(b));
                if (c != 0) return c;
                return b.main.CompareTo(a.main);
            });
            if (selected == null || !qs.Exists(x => x.id == selected)) selected = qs.Count > 0 ? qs[0].id : null;

            string lastHeader = null;
            foreach (var q in qs)
            {
                string header = q.state == QuestState.Done ? "완료한 퀘스트" : q.state == QuestState.Available ? "수락 가능" : "진행 중";
                if (header != lastHeader)
                {
                    var h = UIFactory.Label(list, header, 18, TextAnchor.LowerLeft, UIFactory.TextDim);
                    UIFactory.Size(h, -1, 32);
                    lastHeader = header;
                }
                string id = q.id;
                var btn = UIFactory.Button(list, "", () => { selected = id; Refresh(); }, id == selected ? new Color(0.3f, 0.4f, 0.62f) : UIFactory.PanelLight);
                UIFactory.Size(btn, -1, 54);
                var lbl = UIFactory.ButtonLabel(btn);
                lbl.alignment = TextAnchor.MiddleLeft;
                lbl.fontSize = 20;
                UIFactory.Stretch(lbl.rectTransform, 14, 10, 0, 0);
                string tag = q.main ? UIFactory.Col("[메인] ", new Color(1f, 0.72f, 0.3f)) : UIFactory.Col("[서브] ", new Color(0.55f, 0.78f, 1f));
                string st = q.state == QuestState.ReadyToTurnIn ? UIFactory.Col("  (보고 가능)", UIFactory.Good)
                          : q.state == QuestState.Done ? UIFactory.Col("  (완료)", UIFactory.TextDim) : "";
                lbl.text = tag + q.title + st;
            }
            if (qs.Count == 0)
            {
                var e = UIFactory.Label(list, "퀘스트가 없습니다.", 20, TextAnchor.MiddleCenter, UIFactory.TextDim);
                UIFactory.Size(e, -1, 80);
            }
            ShowDetail();
        }

        void ShowDetail()
        {
            var q = GM.Quests.Get(selected);
            if (q == null) { detailText.text = UIFactory.Col("마을 사람들과 대화하여 퀘스트를 받아보세요.\n머리 위의 ! 표시를 찾아보세요.", UIFactory.TextDim); return; }
            string state;
            switch (q.state)
            {
                case QuestState.Available: state = UIFactory.Col("수락 가능 — " + GiverName(q.giver) + "에게 말을 거세요", UIFactory.Gold); break;
                case QuestState.Active: state = UIFactory.Col("진행 중", UIFactory.Accent); break;
                case QuestState.ReadyToTurnIn: state = UIFactory.Col("목표 달성! " + GiverName(q.giver) + "에게 보고하세요", UIFactory.Good); break;
                default: state = UIFactory.Col("완료", UIFactory.TextDim); break;
            }
            detailText.text =
                "<size=30><b>" + UIFactory.Col(q.title, UIFactory.Gold) + "</b></size>\n" +
                UIFactory.Col((q.main ? "메인 퀘스트" : "서브 퀘스트") + " · 의뢰인: " + GiverName(q.giver), UIFactory.TextDim) + "\n\n" +
                state + "\n\n" +
                q.desc + "\n\n" +
                UIFactory.Col("목표", UIFactory.Gold) + "\n  " + q.GoalText +
                (q.state == QuestState.Active || q.state == QuestState.ReadyToTurnIn ? "\n  진행: " + q.ProgressText : "") + "\n\n" +
                UIFactory.Col("보상", UIFactory.Gold) + "\n  " + q.RewardText +
                (q.state == QuestState.Active ? "\n\n" + UIFactory.Col("\"" + q.activeText + "\"", UIFactory.TextDim) : "");
        }

        static string GiverName(string id)
        {
            foreach (var n in NPC.All) if (n != null && n.id == id) return n.displayName;
            return id;
        }
    }
}
