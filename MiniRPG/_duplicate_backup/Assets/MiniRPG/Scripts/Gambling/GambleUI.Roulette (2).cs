using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>룰렛 (유럽식 싱글 제로, 0~36)</summary>
    public partial class GambleUI
    {
        // 유럽식 휠의 실제 숫자 배치 순서
        static readonly int[] WheelOrder =
        {
            0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10,
            5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26
        };
        static readonly HashSet<int> RedNumbers = new HashSet<int> { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

        static readonly Color RRed = new Color(0.78f, 0.12f, 0.14f);
        static readonly Color RBlack = new Color(0.1f, 0.1f, 0.12f);
        static readonly Color RGreen = new Color(0.1f, 0.55f, 0.25f);

        enum RBet { Straight, Red, Black, Odd, Even, Low, High, Dozen1, Dozen2, Dozen3 }

        RectTransform rouletteArea, wheel, ball;
        Text rSelText, rResultNum, rHistory;
        Button rSpinBtn;
        readonly List<Button> rBetButtons = new List<Button>();
        readonly Dictionary<Button, Color> rBaseColor = new Dictionary<Button, Color>();
        readonly Dictionary<Button, Func<bool>> rIsSelected = new Dictionary<Button, Func<bool>>();
        readonly List<int> rPast = new List<int>();
        RBet rBet = RBet.Red;
        int rNumber = 17;
        float wheelAngle;

        const float PocketRadius = 170f, BallOuterRadius = 205f, BallInnerRadius = 150f;

        static Color NumColor(int n) { return n == 0 ? RGreen : RedNumbers.Contains(n) ? RRed : RBlack; }

        void BuildRoulette()
        {
            // ── 휠 ──
            var rim = UIFactory.Panel(rouletteArea, "Rim", new Color(0.4f, 0.25f, 0.12f));
            rim.sprite = UIFactory.Circle; rim.type = Image.Type.Simple;
            UIFactory.Place(rim.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250, 0), new Vector2(460, 460));
            var rimRing = UIFactory.Panel(rim.transform, "Ring", UIFactory.Gold);
            rimRing.sprite = UIFactory.Ring; rimRing.type = Image.Type.Simple;
            UIFactory.Stretch(rimRing.rectTransform, 2, 2, 2, 2);

            wheel = UIFactory.Rect("Wheel", rim.transform);
            UIFactory.Place(wheel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 400));
            var wheelBg = UIFactory.Panel(wheel, "Bg", new Color(0.18f, 0.1f, 0.05f));
            wheelBg.sprite = UIFactory.Circle; wheelBg.type = Image.Type.Simple;
            UIFactory.Stretch(wheelBg.rectTransform);
            float step = 360f / WheelOrder.Length;
            for (int i = 0; i < WheelOrder.Length; i++)
            {
                int n = WheelOrder[i];
                float a = i * step;
                var pocket = UIFactory.Panel(wheel, "P" + n, NumColor(n));
                var pr = pocket.rectTransform;
                UIFactory.Place(pr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Polar(a, PocketRadius), new Vector2(30, 34));
                pr.localEulerAngles = new Vector3(0, 0, a - 90f);
                var t = UIFactory.Label(pocket.transform, n.ToString(), 15, TextAnchor.MiddleCenter, Color.white, false);
                UIFactory.Stretch(t.rectTransform);
                t.fontStyle = FontStyle.Bold;
            }
            var hub = UIFactory.Panel(wheel, "Hub", new Color(0.55f, 0.38f, 0.15f));
            hub.sprite = UIFactory.Circle; hub.type = Image.Type.Simple;
            UIFactory.Place(hub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
            var hubRing = UIFactory.Panel(hub.transform, "Ring", UIFactory.Gold);
            hubRing.sprite = UIFactory.Ring; hubRing.type = Image.Type.Simple;
            UIFactory.Stretch(hubRing.rectTransform);

            // 가운데 결과 숫자 (휠과 함께 돌지 않도록 rim의 자식)
            rResultNum = UIFactory.Label(rim.transform, "?", 64, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Place(rResultNum.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 90));
            rResultNum.fontStyle = FontStyle.Bold;

            var b = UIFactory.Panel(rim.transform, "Ball", Color.white);
            b.sprite = UIFactory.Circle; b.type = Image.Type.Simple;
            ball = b.rectTransform;
            UIFactory.Place(ball, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Polar(90f, BallOuterRadius), new Vector2(18, 18));
            b.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            // ── 베팅 보드 ──
            var board = UIFactory.Rect("Board", rouletteArea);
            UIFactory.Place(board, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(680, 440));

            const float cw = 48f, ch = 44f, gap = 3f, x0 = 58f;
            // 0
            MakeBetButton(board, "0", RGreen, new Vector2(0, 0), new Vector2(54, ch * 3 + gap * 2), () => SelectBet(RBet.Straight, 0),
                          () => rBet == RBet.Straight && rNumber == 0, 20);
            for (int n = 1; n <= 36; n++)
            {
                int num = n;
                int col = (n - 1) / 3, row = 2 - (n - 1) % 3;
                MakeBetButton(board, n.ToString(), NumColor(n), new Vector2(x0 + col * (cw + gap), -row * (ch + gap)), new Vector2(cw, ch),
                              () => SelectBet(RBet.Straight, num), () => rBet == RBet.Straight && rNumber == num, 19);
            }
            float y1 = -(ch + gap) * 3 - 4f;
            float dw = (cw + gap) * 4 - gap;
            string[] dz = { "1~12", "13~24", "25~36" };
            for (int i = 0; i < 3; i++)
            {
                RBet bt = RBet.Dozen1 + i;
                MakeBetButton(board, dz[i] + "  ×3", new Color(0.12f, 0.35f, 0.2f), new Vector2(x0 + i * (dw + gap), y1), new Vector2(dw, 42),
                              () => SelectBet(bt, rNumber), () => rBet == bt, 18);
            }
            float y2 = y1 - 46f;
            float ow = (cw + gap) * 2 - gap;
            string[] outs = { "1~18", "짝수", "빨강", "검정", "홀수", "19~36" };
            RBet[] outBets = { RBet.Low, RBet.Even, RBet.Red, RBet.Black, RBet.Odd, RBet.High };
            Color[] outCols = { new Color(0.12f, 0.35f, 0.2f), new Color(0.12f, 0.35f, 0.2f), RRed, RBlack, new Color(0.12f, 0.35f, 0.2f), new Color(0.12f, 0.35f, 0.2f) };
            for (int i = 0; i < 6; i++)
            {
                RBet bt = outBets[i];
                MakeBetButton(board, outs[i] + "  ×2", outCols[i], new Vector2(x0 + i * (ow + gap), y2), new Vector2(ow, 42),
                              () => SelectBet(bt, rNumber), () => rBet == bt, 18);
            }

            rSelText = UIFactory.Label(board, "", 21, TextAnchor.MiddleLeft, Color.white);
            UIFactory.Place(rSelText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y2 - 58f), new Vector2(680, 30));

            rSpinBtn = UIFactory.Button(board, "SPIN", RouletteSpin, new Color(0.8f, 0.55f, 0.15f), 28);
            UIFactory.Place((RectTransform)rSpinBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y2 - 100f), new Vector2(300, 62));

            var rule = UIFactory.Label(board, "숫자 적중 ×36 · 0이 나오면 숫자 0 베팅 외에는 모두 패배", 16, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Place(rule.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(316, y2 - 100f), new Vector2(370, 62));

            rHistory = UIFactory.Label(board, "", 18, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Place(rHistory.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y2 - 170f), new Vector2(680, 30));
        }

        static Vector2 Polar(float deg, float r)
        {
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
        }

        void MakeBetButton(RectTransform parent, string label, Color c, Vector2 pos, Vector2 size, Action onClick, Func<bool> isSel, int fontSize)
        {
            var btn = UIFactory.Button(parent, label, onClick, c, fontSize);
            UIFactory.Place((RectTransform)btn.transform, new Vector2(0, 1), new Vector2(0, 1), pos, size);
            rBetButtons.Add(btn);
            rBaseColor[btn] = c;
            rIsSelected[btn] = isSel;
            var ol = btn.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(1, 1, 1, 0.25f);
            ol.effectDistance = new Vector2(1, -1);
        }

        void SelectBet(RBet b, int number)
        {
            if (busy) return;
            rBet = b;
            rNumber = number;
            RouletteRefresh();
        }

        string BetName()
        {
            switch (rBet)
            {
                case RBet.Straight: return "숫자 " + rNumber;
                case RBet.Red: return "빨강";
                case RBet.Black: return "검정";
                case RBet.Odd: return "홀수";
                case RBet.Even: return "짝수";
                case RBet.Low: return "1~18";
                case RBet.High: return "19~36";
                case RBet.Dozen1: return "1~12";
                case RBet.Dozen2: return "13~24";
                default: return "25~36";
            }
        }

        int BetMultiplier() { return rBet == RBet.Straight ? 36 : rBet >= RBet.Dozen1 ? 3 : 2; }

        float BetChance()
        {
            int wins = rBet == RBet.Straight ? 1 : rBet >= RBet.Dozen1 ? 12 : 18;
            return wins / 37f;
        }

        static bool Wins(RBet b, int straightNum, int n)
        {
            switch (b)
            {
                case RBet.Straight: return n == straightNum;
                case RBet.Red: return n != 0 && RedNumbers.Contains(n);
                case RBet.Black: return n != 0 && !RedNumbers.Contains(n);
                case RBet.Odd: return n != 0 && n % 2 == 1;
                case RBet.Even: return n != 0 && n % 2 == 0;
                case RBet.Low: return n >= 1 && n <= 18;
                case RBet.High: return n >= 19;
                case RBet.Dozen1: return n >= 1 && n <= 12;
                case RBet.Dozen2: return n >= 13 && n <= 24;
                default: return n >= 25;
            }
        }

        void RouletteRefresh()
        {
            if (rSpinBtn == null) return;
            foreach (var b in rBetButtons)
            {
                bool sel = rIsSelected[b]();
                b.GetComponent<Image>().color = sel ? UIFactory.Gold : rBaseColor[b];
                UIFactory.ButtonLabel(b).color = sel ? Color.black : Color.white;
                b.interactable = !busy;
            }
            rSelText.text = "선택: " + UIFactory.Col(BetName(), UIFactory.Gold) + "   배당 ×" + BetMultiplier() +
                            "   적중 확률 " + (BetChance() * 100f).ToString("0.0") + "%";
            UIFactory.ButtonLabel(rSpinBtn).text = "SPIN  (" + bet + " G)";
            rSpinBtn.interactable = !busy;
        }

        void RouletteSpin()
        {
            if (busy) return;
            if (!TakeBet(bet)) return;
            busy = true;
            resultText.text = "";
            rResultNum.text = "";
            RefreshInfo();
            StartCoroutine(RouletteRoutine(bet, rBet, rNumber));
        }

        IEnumerator RouletteRoutine(int stake, RBet b, int straightNum)
        {
            int resultIndex = UnityEngine.Random.Range(0, WheelOrder.Length);
            int result = WheelOrder[resultIndex];
            float step = 360f / WheelOrder.Length;

            const float dur = 4.2f;
            float w0 = wheelAngle;
            float w1 = w0 + 360f * 2f + UnityEngine.Random.Range(0f, 360f);        // 휠: 반시계 방향
            float pocketWorld = w1 + resultIndex * step;                            // 멈출 때 결과 칸의 화면상 각도
            float b0 = 90f;
            float b1 = pocketWorld - 360f * 5f;                                     // 공: 시계 방향으로 5바퀴 이상
            while (b1 > b0 - 360f * 4f) b1 -= 360f;

            float t = 0, tickAcc = 0, lastBall = b0;
            while (t < dur)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float k = Mathf.Clamp01(t / dur);
                float easeW = 1f - Mathf.Pow(1f - k, 3f);
                float easeB = 1f - Mathf.Pow(1f - k, 2.4f);
                wheelAngle = Mathf.Lerp(w0, w1, easeW);
                wheel.localEulerAngles = new Vector3(0, 0, wheelAngle);
                float ba = Mathf.Lerp(b0, b1, easeB);
                // 후반부에 공이 안쪽으로 떨어지며 튀는 느낌
                float drop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.9f, k));
                float r = Mathf.Lerp(BallOuterRadius, PocketRadius, drop);
                if (k > 0.55f && k < 0.9f) r += Mathf.Abs(Mathf.Sin(k * 60f)) * 10f * (0.9f - k) * 3f;
                ball.anchoredPosition = Polar(ba, r);

                tickAcc += Mathf.Abs(ba - lastBall);
                lastBall = ba;
                if (tickAcc > step * 1.5f) { tickAcc = 0; SoundFX.Play(Sfx.Tick, k > 0.55f ? 0.35f : 0.12f); }
                yield return null;
            }
            wheelAngle = w1 % 360f;
            wheel.localEulerAngles = new Vector3(0, 0, wheelAngle);
            ball.anchoredPosition = Polar(pocketWorld, PocketRadius);
            SoundFX.Play(Sfx.Card, 0.9f);

            rResultNum.text = result.ToString();
            rResultNum.color = result == 0 ? new Color(0.4f, 1f, 0.55f) : RedNumbers.Contains(result) ? new Color(1f, 0.45f, 0.45f) : Color.white;
            StartCoroutine(Bounce(rResultNum.rectTransform));

            rPast.Add(result);
            if (rPast.Count > 14) rPast.RemoveAt(0);
            var sb = new System.Text.StringBuilder("최근 결과:  ");
            foreach (var n in rPast)
            {
                var c = n == 0 ? new Color(0.4f, 1f, 0.55f) : RedNumbers.Contains(n) ? new Color(1f, 0.45f, 0.45f) : new Color(0.85f, 0.85f, 0.9f);
                sb.Append(UIFactory.Col(n.ToString(), c)).Append("  ");
            }
            rHistory.text = sb.ToString();

            yield return new WaitForSecondsRealtime(0.35f);
            string colorName = result == 0 ? "초록" : RedNumbers.Contains(result) ? "빨강" : "검정";
            bool win = Wins(b, straightNum, result);
            int mult = b == RBet.Straight ? 36 : b >= RBet.Dozen1 ? 3 : 2;
            string head = result + " (" + colorName + ") — ";
            Settle(stake, win ? stake * mult : 0,
                   win ? UIFactory.Col(head + (b == RBet.Straight ? "숫자 적중!!! ×36" : "적중! ×" + mult), b == RBet.Straight ? UIFactory.Gold : UIFactory.Good)
                       : UIFactory.Col(head + "빗나갔습니다", UIFactory.TextDim));
        }
    }
}
