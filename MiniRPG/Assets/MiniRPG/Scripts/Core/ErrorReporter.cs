using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// 실행 중 발생한 오류를 화면 위쪽에 바로 보여주는 진단 도구.
    /// 콘솔을 열지 않아도 어떤 단계에서 무엇이 잘못됐는지 확인할 수 있고, 빌드한 실행 파일에서도 동작한다.
    /// F1: 오류 창 숨기기/보이기
    /// </summary>
    public class ErrorReporter : MonoBehaviour
    {
        struct Entry { public string msg, stack; public int count; }

        static readonly List<Entry> entries = new List<Entry>();
        static ErrorReporter instance;
        static bool hooked;
        bool hidden;
        Vector2 scroll;
        GUIStyle box, label, small;

        /// <summary>씬 로드 전에 로그 수신을 먼저 연결 (부팅 중 오류도 잡기 위해)</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            entries.Clear();
            instance = null;
            if (!hooked)
            {
                Application.logMessageReceived += OnLog;
                hooked = true;
            }
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if (!Application.isPlaying) return;
            // 우리 게임 코드(MiniRPG 네임스페이스)에서 발생한 오류만 표시.
            // 에디터 내부 오류(예: Search 인덱싱 등)는 스택 트레이스에 MiniRPG가 없으므로 여기서 걸러진다.
            if (string.IsNullOrEmpty(stackTrace) || !stackTrace.Contains("MiniRPG")) return;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].msg == condition)
                {
                    var e = entries[i]; e.count++; entries[i] = e;
                    return;
                }
            }
            if (entries.Count >= 8) return;
            entries.Add(new Entry { msg = condition, stack = stackTrace, count = 1 });
            EnsureInstance();
        }

        /// <summary>부팅 단계 이름과 함께 직접 기록</summary>
        public static void Report(string step, System.Exception ex)
        {
            Debug.LogError("[MiniRPG] '" + step + "' 단계에서 오류: " + ex);
            EnsureInstance();
        }

        static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("MiniRPG_ErrorReporter");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ErrorReporter>();
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1)) hidden = !hidden;
#elif ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) hidden = !hidden;
#endif
        }

        void OnGUI()
        {
            if (entries.Count == 0 || hidden) return;
            if (box == null)
            {
                var font = UIFactory.Font;
                box = new GUIStyle(GUI.skin.box);
                box.normal.background = MakeTex(new Color(0.35f, 0.02f, 0.04f, 0.92f));
                label = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = false };
                label.normal.textColor = Color.white;
                small = new GUIStyle(label) { fontSize = 12 };
                small.normal.textColor = new Color(1f, 0.8f, 0.8f);
                if (font != null) { label.font = font; small.font = font; }
            }
            float w = Mathf.Min(Screen.width - 20f, 1100f);
            float h = Mathf.Min(Screen.height * 0.45f, 420f);
            GUILayout.BeginArea(new Rect(10, 10, w, h), box);
            GUILayout.Label("오류가 발생했습니다 (" + entries.Count + "종류) — 아래 첫 줄을 복사해서 알려주세요.  [F1: 숨기기]", label);
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var e in entries)
            {
                GUILayout.Label("• " + e.msg + (e.count > 1 ? "  (x" + e.count + ")" : ""), label);
                if (!string.IsNullOrEmpty(e.stack))
                {
                    var lines = e.stack.Split('\n');
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < lines.Length && i < 4; i++) sb.Append("    ").Append(lines[i].Trim()).Append('\n');
                    GUILayout.Label(sb.ToString(), small);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
