using UnityEngine;
using UnityEngine.EventSystems;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MiniRPG
{
    /// <summary>
    /// 구 Input Manager / 신 Input System 어느 쪽 설정이든 동작하도록 감싼 입력 클래스.
    /// Project Settings > Player > Active Input Handling 값과 상관없이 컴파일/실행됩니다.
    /// </summary>
    public static class GameInput
    {
        public enum K { W, A, S, D, Up, Down, Left, Right, Space, J, E, I, Q, R, Esc, D1, D2, Shift, Enter, Tab, F }

#if ENABLE_LEGACY_INPUT_MANAGER
        static KeyCode Map(K k)
        {
            switch (k)
            {
                case K.W: return KeyCode.W;
                case K.A: return KeyCode.A;
                case K.S: return KeyCode.S;
                case K.D: return KeyCode.D;
                case K.Up: return KeyCode.UpArrow;
                case K.Down: return KeyCode.DownArrow;
                case K.Left: return KeyCode.LeftArrow;
                case K.Right: return KeyCode.RightArrow;
                case K.Space: return KeyCode.Space;
                case K.J: return KeyCode.J;
                case K.E: return KeyCode.E;
                case K.I: return KeyCode.I;
                case K.Q: return KeyCode.Q;
                case K.R: return KeyCode.R;
                case K.Esc: return KeyCode.Escape;
                case K.D1: return KeyCode.Alpha1;
                case K.D2: return KeyCode.Alpha2;
                case K.Shift: return KeyCode.LeftShift;
                case K.Enter: return KeyCode.Return;
                case K.Tab: return KeyCode.Tab;
                case K.F: return KeyCode.F;
            }
            return KeyCode.None;
        }
        public static bool Down(K k) { return Input.GetKeyDown(Map(k)); }
        public static bool Held(K k) { return Input.GetKey(Map(k)); }
        static bool MouseLeftDown() { return Input.GetMouseButtonDown(0); }
#elif ENABLE_INPUT_SYSTEM
        static Key Map(K k)
        {
            switch (k)
            {
                case K.W: return Key.W;
                case K.A: return Key.A;
                case K.S: return Key.S;
                case K.D: return Key.D;
                case K.Up: return Key.UpArrow;
                case K.Down: return Key.DownArrow;
                case K.Left: return Key.LeftArrow;
                case K.Right: return Key.RightArrow;
                case K.Space: return Key.Space;
                case K.J: return Key.J;
                case K.E: return Key.E;
                case K.I: return Key.I;
                case K.Q: return Key.Q;
                case K.R: return Key.R;
                case K.Esc: return Key.Escape;
                case K.D1: return Key.Digit1;
                case K.D2: return Key.Digit2;
                case K.Shift: return Key.LeftShift;
                case K.Enter: return Key.Enter;
                case K.Tab: return Key.Tab;
                case K.F: return Key.F;
            }
            return Key.None;
        }
        public static bool Down(K k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].wasPressedThisFrame; }
        public static bool Held(K k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].isPressed; }
        static bool MouseLeftDown() { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
#else
        public static bool Down(K k) { return false; }
        public static bool Held(K k) { return false; }
        static bool MouseLeftDown() { return false; }
#endif

        public static Vector2 Move()
        {
            float x = 0f, y = 0f;
            if (Held(K.A) || Held(K.Left)) x -= 1f;
            if (Held(K.D) || Held(K.Right)) x += 1f;
            if (Held(K.S) || Held(K.Down)) y -= 1f;
            if (Held(K.W) || Held(K.Up)) y += 1f;
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public static bool Attack { get { return Down(K.Space) || Down(K.J) || (MouseLeftDown() && !PointerOverUI()); } }
        public static bool Skill { get { return Down(K.R); } }
        public static bool Dash { get { return Down(K.Shift); } }
        public static bool Interact { get { return Down(K.E) || Down(K.F); } }
        public static bool Inventory { get { return Down(K.I) || Down(K.Tab); } }
        public static bool QuestLog { get { return Down(K.Q); } }
        public static bool Pause { get { return Down(K.Esc); } }
        public static bool Potion1 { get { return Down(K.D1); } }
        public static bool Potion2 { get { return Down(K.D2); } }
        public static bool Confirm { get { return Down(K.Space) || Down(K.Enter) || Down(K.E); } }
    }
}
