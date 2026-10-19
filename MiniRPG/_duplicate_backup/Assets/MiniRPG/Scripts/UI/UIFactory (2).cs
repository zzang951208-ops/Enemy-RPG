using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>코드만으로 UI를 만들기 위한 헬퍼 모음 (프리팹/에셋 불필요)</summary>
    public static class UIFactory
    {
        // ─────────────── 팔레트 ───────────────
        public static readonly Color PanelDark = new Color(0.08f, 0.09f, 0.13f, 0.94f);
        public static readonly Color PanelMid = new Color(0.14f, 0.16f, 0.22f, 0.96f);
        public static readonly Color PanelLight = new Color(0.22f, 0.25f, 0.33f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        public static readonly Color Accent = new Color(0.36f, 0.62f, 1f);
        public static readonly Color Good = new Color(0.35f, 0.85f, 0.45f);
        public static readonly Color Bad = new Color(0.95f, 0.35f, 0.35f);
        public static readonly Color TextMain = new Color(0.95f, 0.95f, 0.97f);
        public static readonly Color TextDim = new Color(0.68f, 0.71f, 0.78f);
        public static readonly Color BtnNormal = new Color(0.24f, 0.3f, 0.44f, 1f);

        // ─────────────── 폰트 ───────────────
        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = LoadFont();
                return _font;
            }
        }

        static Font LoadFont()
        {
            Font f = null;
            // 한글이 나오는 OS 폰트를 우선 사용 (Windows: 맑은 고딕, macOS: Apple SD Gothic Neo)
            string[] names = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "AppleSDGothicNeo-Regular", "AppleGothic",
                               "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic", "Arial" };
            try { f = Font.CreateDynamicFontFromOSFont(names, 32); } catch { f = null; }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { f = null; } }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { f = null; } }
            return f;
        }

        // ─────────────── 절차적 스프라이트 ───────────────
        static Sprite _rounded, _circle, _white, _ring;
        public static Sprite Rounded { get { if (_rounded == null) _rounded = MakeRounded(64, 14); return _rounded; } }
        public static Sprite Circle { get { if (_circle == null) _circle = MakeCircle(128, false); return _circle; } }
        public static Sprite Ring { get { if (_ring == null) _ring = MakeCircle(128, true); return _ring; } }
        public static Sprite White { get { if (_white == null) _white = MakeRounded(8, 0); return _white; } }

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    if (radius > 0)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                        float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        a = Mathf.Clamp01(radius - d + 0.5f);
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            float b = radius;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite MakeCircle(int size, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01(r - d);
                    if (ring) a *= Mathf.Clamp01(d - (r - 6f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ─────────────── 레이아웃 ───────────────
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>앵커/피벗/위치/크기를 한 번에 지정</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>부모를 가득 채우되 여백(inset) 적용</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded ? Rounded : White;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor anchor, Color color, bool shadow = true)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.75f);
                s.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color? bg = null, int fontSize = 22)
        {
            var img = Panel(parent, "Button", bg ?? BtnNormal);
            var btn = img.gameObject.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            cb.colorMultiplier = 1.2f;
            btn.colors = cb;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            var label = Label(img.transform, text, fontSize, TextAnchor.MiddleCenter, TextMain);
            Stretch(label.rectTransform, 6, 6, 2, 2);
            if (onClick != null)
                btn.onClick.AddListener(() => { SoundFX.Play(Sfx.Click, 0.5f); onClick(); });
            return btn;
        }

        public static Text ButtonLabel(Button b) { return b.GetComponentInChildren<Text>(); }

        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static VerticalLayoutGroup VList(GameObject go, float spacing, int pad, bool expandW = true)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = expandW;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HList(GameObject go, float spacing, int pad)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return h;
        }

        /// <summary>세로 스크롤 목록. 반환값은 항목을 붙일 content</summary>
        public static RectTransform ScrollList(Transform parent, out ScrollRect scroll, float spacing = 6)
        {
            var root = Panel(parent, "Scroll", new Color(0, 0, 0, 0.25f));
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", root.transform);
            Stretch(viewport, 6, 6, 6, 6);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VList(content.gameObject, spacing, 2);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }

        /// <summary>표준 모달 창(제목 + 닫기 버튼). body 에 내용을 채우면 됨.</summary>
        public static RectTransform Window(Transform parent, string title, Vector2 size, Action onClose, out RectTransform body, out Text titleText)
        {
            var dim = Panel(parent, "Window_" + title, new Color(0, 0, 0, 0.55f), false);
            Stretch(dim.rectTransform);
            var win = Panel(dim.transform, "Frame", PanelDark);
            Place(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var outline = win.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.55f, 0.8f, 0.6f);
            outline.effectDistance = new Vector2(2, -2);

            var header = Panel(win.transform, "Header", PanelLight);
            var hr = header.rectTransform;
            hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1); hr.anchoredPosition = Vector2.zero; hr.sizeDelta = new Vector2(0, 56);
            titleText = Label(header.transform, title, 28, TextAnchor.MiddleLeft, Gold);
            Stretch(titleText.rectTransform, 22, 70, 0, 0);
            titleText.fontStyle = FontStyle.Bold;
            if (onClose != null)
            {
                var close = Button(header.transform, "X", onClose, new Color(0.6f, 0.2f, 0.25f), 24);
                Place((RectTransform)close.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(44, 40));
            }
            body = Rect("Body", win.transform);
            Stretch(body, 18, 18, 70, 18);
            return dim.rectTransform;
        }

        public static string ColorHex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
        public static string Col(string s, Color c) { return "<color=#" + ColorHex(c) + ">" + s + "</color>"; }
    }

    /// <summary>체력/마나/경험치 등에 쓰는 가로 막대 (앵커 방식이라 스프라이트 설정 불필요)</summary>
    public class UIBar
    {
        public RectTransform root;
        public RectTransform fill;
        public RectTransform ghost; // 피격 시 천천히 줄어드는 잔상 막대
        public Text label;
        float ghostValue = 1f;
        float value = 1f;

        public UIBar(Transform parent, Color fillColor, Vector2 size, bool withLabel = true, int fontSize = 16)
        {
            var bg = UIFactory.Panel(parent, "Bar", new Color(0, 0, 0, 0.6f));
            root = bg.rectTransform;
            root.sizeDelta = size;
            var g = UIFactory.Panel(bg.transform, "Ghost", new Color(1f, 0.95f, 0.8f, 0.55f));
            ghost = g.rectTransform;
            UIFactory.Stretch(ghost, 2, 2, 2, 2);
            var f = UIFactory.Panel(bg.transform, "Fill", fillColor);
            fill = f.rectTransform;
            UIFactory.Stretch(fill, 2, 2, 2, 2);
            if (withLabel)
            {
                label = UIFactory.Label(bg.transform, "", fontSize, TextAnchor.MiddleCenter, Color.white);
                UIFactory.Stretch(label.rectTransform);
            }
        }

        public void Set(float ratio, string text = null)
        {
            value = Mathf.Clamp01(ratio);
            fill.anchorMax = new Vector2(value, 1);
            fill.gameObject.SetActive(value > 0.001f);
            if (value > ghostValue) ghostValue = value;
            if (label != null && text != null) label.text = text;
        }

        public void Tick(float dt)
        {
            ghostValue = Mathf.MoveTowards(ghostValue, value, dt * 0.6f);
            ghost.anchorMax = new Vector2(ghostValue, 1);
            ghost.gameObject.SetActive(ghostValue > 0.001f);
        }
    }
}
