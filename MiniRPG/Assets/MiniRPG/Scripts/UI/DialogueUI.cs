using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    public class DialogueChoice
    {
        public string label;
        public Action action;
        public DialogueChoice(string label, Action action) { this.label = label; this.action = action; }
    }

    public class DialogueUI : UIWindow
    {
        RectTransform frame, choiceRoot;
        Text nameText, bodyText, hintText;
        string fullText = "";
        float shown;
        bool typing;
        int openedFrame;
        List<DialogueChoice> current = new List<DialogueChoice>();
        const float CharsPerSec = 55f;

        public void Build(RectTransform parent)
        {
            root = UIFactory.Rect("DialogueRoot", parent);
            UIFactory.Stretch(root);
            var bg = UIFactory.Panel(root, "Frame", UIFactory.PanelDark);
            frame = bg.rectTransform;
            UIFactory.Place(frame, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(1400, 280));
            var ol = bg.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.8f, 0.68f, 0.35f, 0.6f);
            ol.effectDistance = new Vector2(2, -2);
            // 클릭하면 타이핑 스킵
            var skip = bg.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(OnConfirm);

            var plate = UIFactory.Panel(frame, "NamePlate", new Color(0.55f, 0.42f, 0.18f));
            UIFactory.Place(plate.rectTransform, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(300, 48));
            nameText = UIFactory.Label(plate.transform, "", 24, TextAnchor.MiddleCenter, Color.white);
            nameText.fontStyle = FontStyle.Bold;
            UIFactory.Stretch(nameText.rectTransform);

            bodyText = UIFactory.Label(frame, "", 24, TextAnchor.UpperLeft, UIFactory.TextMain);
            bodyText.lineSpacing = 1.15f;
            var br = bodyText.rectTransform;
            br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 1);
            br.offsetMin = new Vector2(40, 40); br.offsetMax = new Vector2(-470, -40);

            hintText = UIFactory.Label(frame, "Space / 클릭: 넘기기", 14, TextAnchor.LowerLeft, UIFactory.TextDim);
            UIFactory.Place(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 10), new Vector2(400, 24));

            choiceRoot = UIFactory.Rect("Choices", frame);
            choiceRoot.anchorMin = new Vector2(1, 0); choiceRoot.anchorMax = new Vector2(1, 1);
            choiceRoot.pivot = new Vector2(1, 0.5f);
            choiceRoot.offsetMin = new Vector2(-450, 20); choiceRoot.offsetMax = new Vector2(-20, -30);
            var vl = UIFactory.VList(choiceRoot.gameObject, 8, 0);
            vl.childAlignment = TextAnchor.MiddleCenter;

            root.gameObject.SetActive(false);
        }

        public void Show(NPC npc, string text, List<DialogueChoice> choices)
        {
            string n = npc != null ? npc.displayName : "";
            Show(n, text, choices);
        }

        public void Show(string speaker, string text, List<DialogueChoice> choices)
        {
            var gm = GameManager.I;
            if (gm.UI.Shop.IsOpen) gm.UI.Shop.Close();
            ShowRoot();
            openedFrame = Time.frameCount;
            nameText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            nameText.text = speaker;
            fullText = text ?? "";
            shown = 0; typing = true;
            bodyText.text = "";

            current = choices ?? new List<DialogueChoice>();
            if (current.Count == 0) current.Add(new DialogueChoice("닫기", Close));
            UIFactory.Clear(choiceRoot);
            int idx = 1;
            foreach (var c in current)
            {
                var ch = c;
                var b = UIFactory.Button(choiceRoot, idx + ". " + c.label, () => Pick(ch), UIFactory.BtnNormal, 20);
                UIFactory.Size(b, -1, 42);
                UIFactory.ButtonLabel(b).alignment = TextAnchor.MiddleLeft;
                idx++;
            }
            float h = Mathf.Max(260f, current.Count * 50f + 70f);
            frame.sizeDelta = new Vector2(1400, h);
        }

        /// <summary>여러 줄을 순서대로 보여주고 끝나면 onEnd 호출</summary>
        public void ShowSequence(NPC npc, string[] lines, Action onEnd)
        {
            ShowSequence(npc != null ? npc.displayName : "", lines, 0, onEnd);
        }

        public void ShowSequence(string speaker, string[] lines, Action onEnd)
        {
            ShowSequence(speaker, lines, 0, onEnd);
        }

        void ShowSequence(string speaker, string[] lines, int i, Action onEnd)
        {
            if (i >= lines.Length) { if (onEnd != null) onEnd(); else Close(); return; }
            bool last = i == lines.Length - 1;
            Show(speaker, lines[i], new List<DialogueChoice>
            {
                new DialogueChoice(last ? "확인" : "다음 ▶", () => ShowSequence(speaker, lines, i + 1, onEnd))
            });
        }

        void Pick(DialogueChoice c)
        {
            if (typing) { typing = false; bodyText.text = fullText; }
            if (c.action != null) c.action(); else Close();
        }

        void OnConfirm()
        {
            if (Time.frameCount == openedFrame) return;
            if (typing) { typing = false; bodyText.text = fullText; return; }
            if (current.Count == 1) Pick(current[0]);
        }

        void Update()
        {
            if (!IsOpen) return;
            if (typing)
            {
                shown += Time.unscaledDeltaTime * CharsPerSec;
                int n = (int)shown;
                int visible = VisibleLength(fullText);
                if (n >= visible) { typing = false; bodyText.text = fullText; }
                else bodyText.text = RichPrefix(fullText, n);
            }
            if (Time.frameCount != openedFrame && GameInput.Confirm) OnConfirm();
        }

        // ── 리치 텍스트 태그를 깨뜨리지 않는 타자기 효과 ──
        static int VisibleLength(string s)
        {
            int n = 0; bool tag = false;
            foreach (char c in s)
            {
                if (c == '<') tag = true;
                else if (c == '>' && tag) tag = false;
                else if (!tag) n++;
            }
            return n;
        }

        static string RichPrefix(string s, int count)
        {
            var sb = new StringBuilder();
            var stack = new List<string>();
            int vis = 0, i = 0;
            while (i < s.Length && vis < count)
            {
                char c = s[i];
                if (c == '<')
                {
                    int end = s.IndexOf('>', i);
                    if (end < 0) break;
                    string tag = s.Substring(i + 1, end - i - 1);
                    if (tag.StartsWith("/")) { if (stack.Count > 0) stack.RemoveAt(stack.Count - 1); }
                    else
                    {
                        int eq = tag.IndexOf('=');
                        stack.Add(eq >= 0 ? tag.Substring(0, eq) : tag);
                    }
                    sb.Append(s, i, end - i + 1);
                    i = end + 1;
                    continue;
                }
                sb.Append(c); vis++; i++;
            }
            for (int k = stack.Count - 1; k >= 0; k--) sb.Append("</").Append(stack[k]).Append('>');
            return sb.ToString();
        }
    }
}
