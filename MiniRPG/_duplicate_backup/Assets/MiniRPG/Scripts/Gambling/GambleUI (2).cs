using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>도박 전적 (저장됨)</summary>
    [Serializable]
    public class GambleStats
    {
        public int gamesPlayed, wins, totalBet, totalWon, biggestWin;
        public bool swordGiven;
        public int Net { get { return totalWon - totalBet; } }

        public void Record(int bet, int payout)
        {
            gamesPlayed++;
            totalBet += bet;
            totalWon += payout;
            if (payout > bet) wins++;
            if (payout > biggestWin) biggestWin = payout;
        }

        public void Reset() { gamesPlayed = wins = totalBet = totalWon = biggestWin = 0; swordGiven = false; }
    }

    /// <summary>잭의 행운 천막: 슬롯머신 / 주사위 하이로우 / 블랙잭 (게임 내 골드 전용)</summary>
    public partial class GambleUI : UIWindow
    {
        public enum Game { Slots, Dice, Blackjack, Roulette }

        Game game;
        bool busy;
        int bet = 10;
        int openedFrame;
        public override bool CanClose { get { return !busy; } }

        RectTransform slotArea, diceArea, bjArea, betRow, fxLayer;
        Button[] tabs = new Button[4];
        Text goldText, netText, resultText, footerText;
        readonly List<Button> betButtons = new List<Button>();
        readonly List<int> betValues = new List<int>();

        GameManager GM { get { return GameManager.I; } }
        static readonly Color Felt = new Color(0.08f, 0.32f, 0.2f, 1f);

        // ═══════════════════════════ 공통 ═══════════════════════════
        public void Build(RectTransform parent)
        {
            RectTransform body; Text title;
            root = UIFactory.Window(parent, "럭키 잭의 행운 천막", new Vector2(1240, 800), () => { if (CanClose) Close(); }, out body, out title);

            // 상단: 게임 탭 + 소지금
            var top = UIFactory.Rect("Top", body);
            top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1); top.anchoredPosition = Vector2.zero; top.sizeDelta = new Vector2(0, 52);
            string[] names = { "슬롯머신", "주사위", "블랙잭", "룰렛" };
            for (int i = 0; i < 4; i++)
            {
                int gi = i;
                tabs[i] = UIFactory.Button(top, names[i], () => SwitchGame((Game)gi), null, 21);
                UIFactory.Place((RectTransform)tabs[i].transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 176, 0), new Vector2(168, 46));
            }
            goldText = UIFactory.Label(top, "", 24, TextAnchor.MiddleRight, UIFactory.Gold);
            UIFactory.Place(goldText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 10), new Vector2(420, 30));
            netText = UIFactory.Label(top, "", 16, TextAnchor.MiddleRight, UIFactory.TextDim);
            UIFactory.Place(netText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -16), new Vector2(420, 22));

            // 게임 영역
            var area = UIFactory.Panel(body, "Table", Felt);
            var ar = area.rectTransform;
            ar.anchorMin = Vector2.zero; ar.anchorMax = Vector2.one;
            ar.offsetMin = new Vector2(0, 150); ar.offsetMax = new Vector2(0, -62);
            var ol = area.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.55f, 0.4f, 0.15f, 0.9f); ol.effectDistance = new Vector2(3, -3);
            slotArea = UIFactory.Stretch(UIFactory.Rect("Slots", area.transform));
            diceArea = UIFactory.Stretch(UIFactory.Rect("Dice", area.transform));
            bjArea = UIFactory.Stretch(UIFactory.Rect("Blackjack", area.transform));
            rouletteArea = UIFactory.Stretch(UIFactory.Rect("Roulette", area.transform));
            BuildSlots(); BuildDice(); BuildBlackjack(); BuildRoulette();

            // 하단: 결과 + 베팅
            resultText = UIFactory.Label(body, "", 30, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Place(resultText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(1100, 44));
            resultText.fontStyle = FontStyle.Bold;
            resultText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            var betLabel = UIFactory.Label(body, "베팅액", 20, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Place(betLabel.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(4, 52), new Vector2(90, 40));
            betRow = UIFactory.Rect("Bets", body);
            UIFactory.Place(betRow, new Vector2(0, 0), new Vector2(0, 0), new Vector2(90, 48), new Vector2(900, 48));
            var hl = UIFactory.HList(betRow.gameObject, 8, 0);
            hl.childAlignment = TextAnchor.MiddleLeft;

            footerText = UIFactory.Label(body, "", 15, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Place(footerText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(4, 4), new Vector2(1200, 38));

            fxLayer = UIFactory.Stretch(UIFactory.Rect("Fx", root));
            root.gameObject.SetActive(false);
        }

        public void Open(Game g)
        {
            if (GM.UI.Dialogue.IsOpen) GM.UI.Dialogue.Close();
            ShowRoot();
            openedFrame = Time.frameCount;
            BuildBets();
            resultText.text = "";
            SwitchGame(g);
        }

        public override void Close()
        {
            if (busy) return;
            base.Close();
        }

        void SwitchGame(Game g)
        {
            if (busy) return;
            game = g;
            slotArea.gameObject.SetActive(g == Game.Slots);
            rouletteArea.gameObject.SetActive(g == Game.Roulette);
            diceArea.gameObject.SetActive(g == Game.Dice);
            bjArea.gameObject.SetActive(g == Game.Blackjack);
            for (int i = 0; i < 4; i++) tabs[i].GetComponent<Image>().color = i == (int)g ? UIFactory.Accent : UIFactory.BtnNormal;
            resultText.text = "";
            if (g == Game.Blackjack) BjReset();
            RefreshInfo();
        }

        void BuildBets()
        {
            UIFactory.Clear(betRow);
            betButtons.Clear(); betValues.Clear();
            var vals = new List<int> { 10, 50, 100, 500 };
            if (GM.HighRoller) { vals.Add(1000); vals.Add(5000); }
            if (!vals.Contains(bet)) bet = 10;
            foreach (var v in vals)
            {
                int value = v;
                bool high = v >= 1000;
                var b = UIFactory.Button(betRow, v + " G", () => { if (!busy) { bet = value; RefreshInfo(); } }, high ? new Color(0.45f, 0.25f, 0.6f) : UIFactory.BtnNormal, 21);
                UIFactory.Size(b, 130, 46);
                betButtons.Add(b); betValues.Add(v);
            }
        }

        void RefreshInfo()
        {
            var s = GM.Stats; var g = GM.Gamble;
            goldText.text = "소지금  " + s.gold.ToString("N0") + " G";
            netText.text = "오늘의 순이익  " + (g.Net >= 0 ? UIFactory.Col("+" + g.Net + "G", UIFactory.Good) : UIFactory.Col(g.Net + "G", UIFactory.Bad)) +
                           "  ·  " + g.gamesPlayed + "판 " + g.wins + "승";
            for (int i = 0; i < betButtons.Count; i++)
            {
                var img = betButtons[i].GetComponent<Image>();
                bool sel = betValues[i] == bet;
                img.color = sel ? UIFactory.Gold : (betValues[i] >= 1000 ? new Color(0.45f, 0.25f, 0.6f) : UIFactory.BtnNormal);
                UIFactory.ButtonLabel(betButtons[i]).color = sel ? Color.black : UIFactory.TextMain;
                betButtons[i].interactable = !busy && s.gold >= betValues[i];
            }
            foreach (var t in tabs) t.interactable = !busy;

            if (g.Net <= -1000)
                footerText.text = UIFactory.Col("[!] 오늘 " + (-g.Net) + "G를 잃었습니다. 잠시 쉬어가는 건 어떨까요? 골드는 사냥과 퀘스트로 안정적으로 벌 수 있어요.", UIFactory.Bad);
            else
                footerText.text = "게임 내 골드 전용 미니게임  ·  기대 환급률: 슬롯 약 91% · 주사위 약 92% · 룰렛 약 97% · 블랙잭 약 99%(최적 전략 시)  ·  장기적으로는 하우스가 이깁니다";

            RouletteRefresh();
            if (spinBtn != null)
            {
                UIFactory.ButtonLabel(spinBtn).text = "SPIN  (" + bet + " G)";
                spinBtn.interactable = !busy;
            }
            for (int i = 0; i < diceBtns.Length; i++) if (diceBtns[i] != null) diceBtns[i].interactable = !busy;
            BjButtons();
        }

        /// <summary>판돈 차감. 실패 시 false</summary>
        bool TakeBet(int amount)
        {
            if (!GM.Stats.Spend(amount))
            {
                SoundFX.Play(Sfx.Lose, 0.4f);
                ShowResult(UIFactory.Col("골드가 부족합니다!", UIFactory.Bad));
                return false;
            }
            SoundFX.Play(Sfx.Coin, 0.6f);
            return true;
        }

        void Settle(int totalBet, int payout, string msg)
        {
            if (payout > 0) GM.Stats.AddGold(payout);
            GM.Gamble.Record(totalBet, payout);
            int profit = payout - totalBet;
            if (payout >= totalBet * 5) { SoundFX.Play(Sfx.BigWin); CoinRain(40); GM.Cam.Shake(0.15f, 0.3f); }
            else if (profit > 0) { SoundFX.Play(Sfx.Win); CoinRain(12); }
            else if (profit == 0) SoundFX.Play(Sfx.Coin, 0.5f);
            else SoundFX.Play(Sfx.Lose, 0.7f);
            string tail = profit > 0 ? UIFactory.Col("  +" + profit + " G", UIFactory.Gold)
                        : profit == 0 ? UIFactory.Col("  (본전)", UIFactory.TextDim)
                        : UIFactory.Col("  " + profit + " G", UIFactory.Bad);
            ShowResult(msg + tail);
            busy = false;
            RefreshInfo();
            GM.Save();
        }

        void ShowResult(string msg)
        {
            resultText.text = msg;
            StopCoroutine("PopText");
            StartCoroutine("PopText");
        }

        IEnumerator PopText()
        {
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                resultText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, t / 0.3f);
                yield return null;
            }
            resultText.rectTransform.localScale = Vector3.one;
        }

        void CoinRain(int n)
        {
            for (int i = 0; i < n; i++) StartCoroutine(Coin(UnityEngine.Random.Range(0f, 0.8f)));
        }

        IEnumerator Coin(float delay)
        {
            float d = 0;
            while (d < delay) { d += Time.unscaledDeltaTime; yield return null; }
            var img = UIFactory.Panel(fxLayer, "Coin", UIFactory.Gold);
            img.sprite = UIFactory.Circle; img.type = Image.Type.Simple; img.raycastTarget = false;
            var rt = img.rectTransform;
            float size = UnityEngine.Random.Range(18f, 34f);
            UIFactory.Place(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(UnityEngine.Random.Range(-580f, 580f), 20), new Vector2(size, size));
            float vy = UnityEngine.Random.Range(-100f, 50f), vx = UnityEngine.Random.Range(-60f, 60f), spin = UnityEngine.Random.Range(4f, 10f);
            float t = 0;
            while (t < 2.2f && rt != null)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt; vy -= 900f * dt;
                rt.anchoredPosition += new Vector2(vx, vy) * dt;
                rt.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * spin)), 1, 1);
                img.color = new Color(1f, 0.82f, 0.3f, Mathf.Clamp01((2.2f - t) * 2f));
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        // ═══════════════════════════ 슬롯머신 ═══════════════════════════
        static readonly string[] Sym = { "7", "BAR", "★", "◆", "♥", "♠" };
        static readonly int[] SymWeight = { 1, 2, 3, 4, 5, 5 };
        static readonly float[] SymMult = { 50f, 25f, 15f, 8f, 5f, 5f };
        static readonly Color[] SymColor =
        {
            new Color(1f, 0.25f, 0.25f), new Color(0.95f, 0.85f, 0.45f), new Color(1f, 0.9f, 0.2f),
            new Color(0.35f, 0.85f, 1f), new Color(1f, 0.45f, 0.65f), new Color(0.7f, 0.6f, 1f)
        };
        const float PairMult = 1.3f;
        const int JackpotSwordMinBet = 100;

        Text[,] reelText = new Text[3, 3]; // [reel, row] row 0=위,1=가운데,2=아래
        Image[] reelFrame = new Image[3];
        Button spinBtn;

        void BuildSlots()
        {
            var machine = UIFactory.Panel(slotArea, "Machine", new Color(0.35f, 0.08f, 0.1f));
            UIFactory.Place(machine.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(660, 540));
            var mo = machine.gameObject.AddComponent<Outline>();
            mo.effectColor = UIFactory.Gold; mo.effectDistance = new Vector2(3, -3);
            var header = UIFactory.Label(machine.transform, "★ LUCKY JACK ★", 34, TextAnchor.MiddleCenter, UIFactory.Gold);
            UIFactory.Place(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(600, 50));
            header.fontStyle = FontStyle.Bold;

            for (int r = 0; r < 3; r++)
            {
                var frame = UIFactory.Panel(machine.transform, "Reel" + r, new Color(0.97f, 0.95f, 0.9f));
                reelFrame[r] = frame;
                UIFactory.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((r - 1) * 200, 30), new Vector2(180, 300));
                frame.gameObject.AddComponent<RectMask2D>();
                for (int row = 0; row < 3; row++)
                {
                    var t = UIFactory.Label(frame.transform, Sym[UnityEngine.Random.Range(0, Sym.Length)], row == 1 ? 84 : 60, TextAnchor.MiddleCenter, Color.black, false);
                    UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, (1 - row) * 100), new Vector2(180, 100));
                    t.fontStyle = FontStyle.Bold;
                    reelText[r, row] = t;
                }
                SetReelColors(r);
            }
            // 페이라인
            var lineL = UIFactory.Label(machine.transform, "▶", 40, TextAnchor.MiddleCenter, UIFactory.Gold);
            UIFactory.Place(lineL.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-315, 30), new Vector2(40, 60));
            var lineR = UIFactory.Label(machine.transform, "◀", 40, TextAnchor.MiddleCenter, UIFactory.Gold);
            UIFactory.Place(lineR.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(315, 30), new Vector2(40, 60));

            spinBtn = UIFactory.Button(machine.transform, "SPIN", Spin, new Color(0.8f, 0.2f, 0.2f), 30);
            UIFactory.Place((RectTransform)spinBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(340, 70));

            // 배당표
            var pay = UIFactory.Panel(slotArea, "Paytable", new Color(0, 0, 0, 0.35f));
            UIFactory.Place(pay.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(430, 540));
            var sb = new System.Text.StringBuilder();
            sb.Append(UIFactory.Col("<b>배당표</b>", UIFactory.Gold)).Append("  <size=16>(가운데 줄 기준)</size>\n\n");
            for (int i = 0; i < Sym.Length; i++)
            {
                string s3 = Sym[i] + " " + Sym[i] + " " + Sym[i];
                sb.Append(UIFactory.Col(s3, SymColor[i])).Append("    ×").Append(SymMult[i]).Append('\n');
            }
            sb.Append("\n아무 그림 2개 일치    ×").Append(PairMult).Append('\n');
            sb.Append("\n").Append(UIFactory.Col("<size=17>7-7-7을 " + JackpotSwordMinBet + "G 이상 걸고 맞히면\n전설의 무기를 얻는다는 소문이...</size>", new Color(1f, 0.6f, 0.4f)));
            var pt = UIFactory.Label(pay.transform, sb.ToString(), 22, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch(pt.rectTransform, 26, 20, 22, 20);
            pt.lineSpacing = 1.2f;
        }

        void SetReelColors(int r)
        {
            for (int row = 0; row < 3; row++)
            {
                var t = reelText[r, row];
                int idx = Array.IndexOf(Sym, t.text);
                var c = idx >= 0 ? SymColor[idx] * 0.85f : Color.black;
                c.a = row == 1 ? 1f : 0.35f;
                t.color = c;
            }
        }

        static int RollSymbol()
        {
            int total = 0;
            foreach (var w in SymWeight) total += w;
            int r = UnityEngine.Random.Range(0, total);
            for (int i = 0; i < SymWeight.Length; i++) { if (r < SymWeight[i]) return i; r -= SymWeight[i]; }
            return SymWeight.Length - 1;
        }

        void Spin()
        {
            if (busy) return;
            if (!TakeBet(bet)) return;
            busy = true;
            resultText.text = "";
            RefreshInfo();
            StartCoroutine(SpinRoutine(bet));
        }

        IEnumerator SpinRoutine(int stake)
        {
            var result = new int[3];
            for (int i = 0; i < 3; i++) { result[i] = RollSymbol(); reelFrame[i].color = new Color(0.97f, 0.95f, 0.9f); }
            bool[] stopped = new bool[3];
            float t = 0, step = 0;
            float[] stopAt = { 0.9f, 1.35f, 1.85f };
            // 앞 두 릴이 7이면 마지막 릴은 긴장감 있게 더 오래 돈다
            bool tease = false;
            while (!stopped[2])
            {
                float dt = Time.unscaledDeltaTime;
                t += dt; step += dt;
                if (step >= 0.055f)
                {
                    step = 0;
                    for (int r = 0; r < 3; r++)
                    {
                        if (stopped[r]) continue;
                        reelText[r, 2].text = reelText[r, 1].text;
                        reelText[r, 1].text = reelText[r, 0].text;
                        reelText[r, 0].text = Sym[UnityEngine.Random.Range(0, Sym.Length)];
                        SetReelColors(r);
                    }
                    SoundFX.Play(Sfx.Tick, 0.18f);
                }
                for (int r = 0; r < 3; r++)
                {
                    if (stopped[r] || t < stopAt[r]) continue;
                    stopped[r] = true;
                    reelText[r, 1].text = Sym[result[r]];
                    reelText[r, 0].text = Sym[UnityEngine.Random.Range(0, Sym.Length)];
                    reelText[r, 2].text = Sym[UnityEngine.Random.Range(0, Sym.Length)];
                    SetReelColors(r);
                    SoundFX.Play(Sfx.Card, 0.8f);
                    StartCoroutine(Bounce(reelFrame[r].rectTransform));
                    if (r == 1 && result[0] == result[1] && !tease)
                    {
                        tease = true;
                        stopAt[2] += 0.9f;
                        ShowResult(UIFactory.Col("두근두근...!", UIFactory.Gold));
                    }
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.25f);

            float mult = 0;
            string msg;
            if (result[0] == result[1] && result[1] == result[2])
            {
                mult = SymMult[result[0]];
                msg = result[0] == 0 ? UIFactory.Col("잭팟!!! 7-7-7", new Color(1f, 0.4f, 0.3f)) : UIFactory.Col("트리플! " + Sym[result[0]] + " ×" + mult, UIFactory.Gold);
                for (int r = 0; r < 3; r++) reelFrame[r].color = new Color(1f, 0.9f, 0.55f);
            }
            else if (result[0] == result[1] || result[1] == result[2] || result[0] == result[2])
            {
                mult = PairMult;
                msg = "2개 일치 ×" + PairMult;
                for (int r = 0; r < 3; r++)
                {
                    int o1 = result[(r + 1) % 3], o2 = result[(r + 2) % 3];
                    if (result[r] == o1 || result[r] == o2) reelFrame[r].color = new Color(0.85f, 1f, 0.85f);
                }
            }
            else msg = UIFactory.Col("꽝...", UIFactory.TextDim);

            int payout = Mathf.FloorToInt(stake * mult);
            Settle(stake, payout, msg);

            if (result[0] == 0 && result[1] == 0 && result[2] == 0 && stake >= JackpotSwordMinBet && !GM.Gamble.swordGiven)
            {
                GM.Gamble.swordGiven = true;
                GM.Inv.Add("dragon_sword");
                GM.UI.Hud.Banner("전설의 무기 획득!", "잭팟 보너스: 용살자의 검 (인벤토리에서 장착)");
                GM.Save();
            }
        }

        IEnumerator Bounce(RectTransform rt)
        {
            float t = 0;
            var basePos = rt.anchoredPosition;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = basePos + new Vector2(0, Mathf.Sin(t / 0.18f * Mathf.PI) * -14f);
                yield return null;
            }
            rt.anchoredPosition = basePos;
        }

        // ═══════════════════════════ 주사위 하이로우 ═══════════════════════════
        const float HiLoMult = 2.2f, SevenMult = 5.5f;
        Image[][] pips = new Image[2][];
        RectTransform[] dieRects = new RectTransform[2];
        Text diceSum, diceHistory;
        Button[] diceBtns = new Button[3];
        readonly List<int> history = new List<int>();

        // 3x3 격자 인덱스 (0 1 2 / 3 4 5 / 6 7 8)
        static readonly int[][] Faces =
        {
            new[] { 4 }, new[] { 0, 8 }, new[] { 0, 4, 8 }, new[] { 0, 2, 6, 8 }, new[] { 0, 2, 4, 6, 8 }, new[] { 0, 2, 3, 5, 6, 8 }
        };

        void BuildDice()
        {
            for (int d = 0; d < 2; d++)
            {
                var die = UIFactory.Panel(diceArea, "Die" + d, new Color(0.97f, 0.96f, 0.93f));
                dieRects[d] = die.rectTransform;
                UIFactory.Place(die.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(d == 0 ? -120 : 120, -40), new Vector2(190, 190));
                die.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6, -6);
                pips[d] = new Image[9];
                for (int i = 0; i < 9; i++)
                {
                    var p = UIFactory.Panel(die.transform, "Pip", new Color(0.12f, 0.12f, 0.15f));
                    p.sprite = UIFactory.Circle; p.type = Image.Type.Simple;
                    int x = i % 3, y = i / 3;
                    UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((x - 1) * 52, (1 - y) * 52), new Vector2(34, 34));
                    pips[d][i] = p;
                }
                SetFace(d, d == 0 ? 3 : 4);
            }
            diceSum = UIFactory.Label(diceArea, "합계를 예측하세요", 34, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Place(diceSum.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(700, 50));
            diceSum.fontStyle = FontStyle.Bold;

            string[] labels =
            {
                "낮음 (2~6)\n<size=18>×" + HiLoMult + " · 확률 41.7%</size>",
                "럭키 7\n<size=18>×" + SevenMult + " · 확률 16.7%</size>",
                "높음 (8~12)\n<size=18>×" + HiLoMult + " · 확률 41.7%</size>"
            };
            Color[] cols = { new Color(0.25f, 0.45f, 0.8f), new Color(0.8f, 0.55f, 0.15f), new Color(0.8f, 0.3f, 0.3f) };
            for (int i = 0; i < 3; i++)
            {
                int choice = i;
                diceBtns[i] = UIFactory.Button(diceArea, labels[i], () => RollDice(choice), cols[i], 26);
                UIFactory.Place((RectTransform)diceBtns[i].transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((i - 1) * 300, 70), new Vector2(280, 100));
            }
            diceHistory = UIFactory.Label(diceArea, "", 19, TextAnchor.MiddleCenter, UIFactory.TextDim);
            UIFactory.Place(diceHistory.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(1000, 34));
        }

        void SetFace(int die, int value)
        {
            var on = Faces[Mathf.Clamp(value, 1, 6) - 1];
            for (int i = 0; i < 9; i++) pips[die][i].gameObject.SetActive(Array.IndexOf(on, i) >= 0);
        }

        void RollDice(int choice)
        {
            if (busy) return;
            if (!TakeBet(bet)) return;
            busy = true;
            resultText.text = "";
            RefreshInfo();
            StartCoroutine(DiceRoutine(choice, bet));
        }

        IEnumerator DiceRoutine(int choice, int stake)
        {
            int a = UnityEngine.Random.Range(1, 7), b = UnityEngine.Random.Range(1, 7);
            string[] picks = { "낮음", "럭키 7", "높음" };
            diceSum.text = "선택: " + picks[choice] + " ...";
            float t = 0, step = 0;
            while (t < 1.1f)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt; step += dt;
                if (step > 0.07f)
                {
                    step = 0;
                    SetFace(0, UnityEngine.Random.Range(1, 7));
                    SetFace(1, UnityEngine.Random.Range(1, 7));
                    SoundFX.Play(Sfx.Dice, 0.35f);
                }
                for (int d = 0; d < 2; d++)
                {
                    dieRects[d].localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 40f + d) * 18f * (1.1f - t));
                    dieRects[d].anchoredPosition = new Vector2(d == 0 ? -120 : 120, -40 + Mathf.Abs(Mathf.Sin(t * 14f + d)) * 30f * (1.1f - t));
                }
                yield return null;
            }
            for (int d = 0; d < 2; d++)
            {
                dieRects[d].localEulerAngles = Vector3.zero;
                dieRects[d].anchoredPosition = new Vector2(d == 0 ? -120 : 120, -40);
            }
            SetFace(0, a); SetFace(1, b);
            SoundFX.Play(Sfx.Dice, 0.9f);
            int sum = a + b;
            diceSum.text = a + " + " + b + " = " + UIFactory.Col(sum.ToString(), sum == 7 ? UIFactory.Gold : sum < 7 ? new Color(0.5f, 0.7f, 1f) : new Color(1f, 0.5f, 0.5f));

            history.Add(sum);
            if (history.Count > 14) history.RemoveAt(0);
            var sb = new System.Text.StringBuilder("최근 결과:  ");
            foreach (var h in history)
                sb.Append(UIFactory.Col(h.ToString(), h == 7 ? UIFactory.Gold : h < 7 ? new Color(0.5f, 0.7f, 1f) : new Color(1f, 0.5f, 0.5f))).Append("  ");
            diceHistory.text = sb.ToString();

            yield return new WaitForSecondsRealtime(0.2f);
            bool win = (choice == 0 && sum <= 6) || (choice == 1 && sum == 7) || (choice == 2 && sum >= 8);
            float mult = win ? (choice == 1 ? SevenMult : HiLoMult) : 0f;
            Settle(stake, Mathf.FloorToInt(stake * mult), win ? UIFactory.Col("적중! ×" + mult, UIFactory.Good) : UIFactory.Col("빗나갔습니다", UIFactory.TextDim));
        }

        // ═══════════════════════════ 블랙잭 ═══════════════════════════
        struct Card { public int rank, suit; }
        static readonly string[] Suits = { "♠", "♥", "♦", "♣" };
        static readonly string[] Ranks = { "", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        readonly List<Card> deck = new List<Card>();
        readonly List<Card> playerCards = new List<Card>(), dealerCards = new List<Card>();
        RectTransform dealerRow, playerRow;
        Text dealerScore, playerScore, bjInfo;
        Button dealBtn, hitBtn, standBtn, doubleBtn;
        bool playerTurn, holeHidden;
        int bjStake;

        void BuildBlackjack()
        {
            var dl = UIFactory.Label(bjArea, "딜러", 22, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Place(dl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -16), new Vector2(200, 30));
            dealerScore = UIFactory.Label(bjArea, "", 24, TextAnchor.MiddleLeft, Color.white);
            UIFactory.Place(dealerScore.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -50), new Vector2(200, 34));
            dealerRow = UIFactory.Rect("DealerCards", bjArea);
            UIFactory.Place(dealerRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(760, 160));
            var h1 = UIFactory.HList(dealerRow.gameObject, 12, 0); h1.childControlHeight = false; h1.childControlWidth = false;

            bjInfo = UIFactory.Label(bjArea, "", 21, TextAnchor.MiddleCenter, UIFactory.TextDim);
            UIFactory.Place(bjInfo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(900, 36));

            var pl = UIFactory.Label(bjArea, "나", 22, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Place(pl.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 250), new Vector2(200, 30));
            playerScore = UIFactory.Label(bjArea, "", 24, TextAnchor.MiddleLeft, Color.white);
            UIFactory.Place(playerScore.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 214), new Vector2(200, 34));
            playerRow = UIFactory.Rect("PlayerCards", bjArea);
            UIFactory.Place(playerRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(760, 160));
            var h2 = UIFactory.HList(playerRow.gameObject, 12, 0); h2.childControlHeight = false; h2.childControlWidth = false;

            dealBtn = UIFactory.Button(bjArea, "딜 (시작)", BjDeal, new Color(0.8f, 0.55f, 0.15f), 24);
            hitBtn = UIFactory.Button(bjArea, "히트", BjHit, new Color(0.25f, 0.55f, 0.32f), 24);
            standBtn = UIFactory.Button(bjArea, "스탠드", BjStand, new Color(0.7f, 0.25f, 0.25f), 24);
            doubleBtn = UIFactory.Button(bjArea, "더블다운", BjDouble, new Color(0.45f, 0.3f, 0.65f), 24);
            Button[] bs = { dealBtn, hitBtn, standBtn, doubleBtn };
            for (int i = 0; i < 4; i++)
                UIFactory.Place((RectTransform)bs[i].transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((i - 1.5f) * 200, 24), new Vector2(185, 64));
            BjReset();
        }

        void BjReset()
        {
            if (busy) return;
            playerCards.Clear(); dealerCards.Clear();
            if (dealerRow != null) { UIFactory.Clear(dealerRow); UIFactory.Clear(playerRow); }
            playerTurn = false; holeHidden = false;
            if (dealerScore != null)
            {
                dealerScore.text = ""; playerScore.text = "";
                bjInfo.text = "딜러는 17 이상에서 멈춥니다 · 블랙잭 ×2.5 · 승리 ×2 · 무승부 시 판돈 반환";
            }
            BjButtons();
        }

        void BjButtons()
        {
            if (dealBtn == null) return;
            dealBtn.gameObject.SetActive(!busy);
            hitBtn.gameObject.SetActive(busy);
            standBtn.gameObject.SetActive(busy);
            doubleBtn.gameObject.SetActive(busy);
            hitBtn.interactable = playerTurn;
            standBtn.interactable = playerTurn;
            doubleBtn.interactable = playerTurn && playerCards.Count == 2 && GM.Stats.gold >= bjStake;
        }

        void NewDeck()
        {
            deck.Clear();
            for (int s = 0; s < 4; s++) for (int r = 1; r <= 13; r++) deck.Add(new Card { rank = r, suit = s });
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = deck[i]; deck[i] = deck[j]; deck[j] = tmp;
            }
        }

        Card Draw()
        {
            if (deck.Count == 0) NewDeck();
            var c = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return c;
        }

        static int Score(List<Card> cards)
        {
            int total = 0, aces = 0;
            foreach (var c in cards)
            {
                if (c.rank == 1) { aces++; total += 11; }
                else total += Mathf.Min(10, c.rank);
            }
            while (total > 21 && aces > 0) { total -= 10; aces--; }
            return total;
        }

        static bool IsBlackjack(List<Card> cards) { return cards.Count == 2 && Score(cards) == 21; }

        void AddCardUI(RectTransform row, Card c, bool hidden)
        {
            var card = UIFactory.Panel(row, "Card", hidden ? new Color(0.2f, 0.3f, 0.65f) : Color.white);
            ((RectTransform)card.transform).sizeDelta = new Vector2(108, 154);
            card.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -4);
            if (hidden)
            {
                var inner = UIFactory.Panel(card.transform, "Pattern", new Color(0.3f, 0.45f, 0.85f));
                UIFactory.Stretch(inner.rectTransform, 8, 8, 8, 8);
                var q = UIFactory.Label(card.transform, "?", 60, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f));
                UIFactory.Stretch(q.rectTransform);
            }
            else
            {
                Color col = (c.suit == 1 || c.suit == 2) ? new Color(0.85f, 0.1f, 0.15f) : new Color(0.1f, 0.1f, 0.15f);
                var rk = UIFactory.Label(card.transform, Ranks[c.rank] + "\n" + Suits[c.suit], 24, TextAnchor.UpperLeft, col, false);
                UIFactory.Stretch(rk.rectTransform, 10, 6, 6, 6);
                rk.lineSpacing = 0.9f;
                rk.fontStyle = FontStyle.Bold;
                var big = UIFactory.Label(card.transform, Suits[c.suit], 64, TextAnchor.MiddleCenter, col, false);
                UIFactory.Stretch(big.rectTransform, 0, 0, 18, 0);
            }
            StartCoroutine(PopIn((RectTransform)card.transform));
            SoundFX.Play(Sfx.Card, 0.8f);
        }

        IEnumerator PopIn(RectTransform rt)
        {
            float t = 0;
            while (t < 0.18f && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float k = t / 0.18f;
                rt.localScale = new Vector3(k, 1f, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        void RefreshScores()
        {
            playerScore.text = playerCards.Count > 0 ? "합계 " + UIFactory.Col(Score(playerCards).ToString(), UIFactory.Gold) : "";
            if (dealerCards.Count == 0) dealerScore.text = "";
            else if (holeHidden) dealerScore.text = "합계 " + UIFactory.Col(Score(new List<Card> { dealerCards[0] }) + " + ?", UIFactory.Gold);
            else dealerScore.text = "합계 " + UIFactory.Col(Score(dealerCards).ToString(), UIFactory.Gold);
        }

        void RedrawDealer()
        {
            UIFactory.Clear(dealerRow);
            for (int i = 0; i < dealerCards.Count; i++) AddCardUI(dealerRow, dealerCards[i], holeHidden && i == 1);
        }

        void BjDeal()
        {
            if (busy) return;
            if (!TakeBet(bet)) return;
            BjReset();
            busy = true;
            bjStake = bet;
            resultText.text = "";
            NewDeck();
            RefreshInfo();
            StartCoroutine(DealRoutine());
        }

        IEnumerator DealRoutine()
        {
            var wait = new WaitForSecondsRealtime(0.28f);
            holeHidden = true;
            bjInfo.text = "";
            playerCards.Add(Draw()); AddCardUI(playerRow, playerCards[0], false); RefreshScores(); yield return wait;
            dealerCards.Add(Draw()); AddCardUI(dealerRow, dealerCards[0], false); RefreshScores(); yield return wait;
            playerCards.Add(Draw()); AddCardUI(playerRow, playerCards[1], false); RefreshScores(); yield return wait;
            dealerCards.Add(Draw()); AddCardUI(dealerRow, dealerCards[1], true); RefreshScores(); yield return wait;

            bool pBJ = IsBlackjack(playerCards), dBJ = IsBlackjack(dealerCards);
            if (pBJ || dBJ)
            {
                holeHidden = false; RedrawDealer(); RefreshScores();
                yield return wait;
                if (pBJ && dBJ) Settle(bjStake, bjStake, "둘 다 블랙잭! 무승부");
                else if (pBJ) Settle(bjStake, Mathf.FloorToInt(bjStake * 2.5f), UIFactory.Col("블랙잭!! ×2.5", UIFactory.Gold));
                else Settle(bjStake, 0, UIFactory.Col("딜러 블랙잭...", UIFactory.Bad));
                BjButtons();
                yield break;
            }
            playerTurn = true;
            bjInfo.text = "히트: 카드 한 장 더 · 스탠드: 멈추기 · 더블다운: 판돈 2배로 한 장만 받기";
            BjButtons();
        }

        void BjHit()
        {
            if (!playerTurn) return;
            var c = Draw();
            playerCards.Add(c);
            AddCardUI(playerRow, c, false);
            RefreshScores();
            int s = Score(playerCards);
            if (s > 21) { playerTurn = false; StartCoroutine(BustRoutine()); }
            else if (s == 21) BjStand();
            BjButtons();
        }

        IEnumerator BustRoutine()
        {
            BjButtons();
            yield return new WaitForSecondsRealtime(0.4f);
            holeHidden = false; RedrawDealer(); RefreshScores();
            Settle(bjStake, 0, UIFactory.Col("버스트! (21 초과)", UIFactory.Bad));
            BjButtons();
        }

        void BjStand()
        {
            if (!playerTurn) return;
            playerTurn = false;
            BjButtons();
            StartCoroutine(DealerRoutine());
        }

        void BjDouble()
        {
            if (!playerTurn || playerCards.Count != 2) return;
            if (!TakeBet(bjStake)) return;
            bjStake *= 2;
            RefreshInfo();
            var c = Draw();
            playerCards.Add(c);
            AddCardUI(playerRow, c, false);
            RefreshScores();
            playerTurn = false;
            if (Score(playerCards) > 21) StartCoroutine(BustRoutine());
            else StartCoroutine(DealerRoutine());
            BjButtons();
        }

        IEnumerator DealerRoutine()
        {
            var wait = new WaitForSecondsRealtime(0.55f);
            yield return new WaitForSecondsRealtime(0.3f);
            holeHidden = false; RedrawDealer(); RefreshScores();
            yield return wait;
            while (Score(dealerCards) < 17)
            {
                var c = Draw();
                dealerCards.Add(c);
                AddCardUI(dealerRow, c, false);
                RefreshScores();
                yield return wait;
            }
            int p = Score(playerCards), d = Score(dealerCards);
            if (d > 21) Settle(bjStake, bjStake * 2, UIFactory.Col("딜러 버스트! 승리 ×2", UIFactory.Good));
            else if (p > d) Settle(bjStake, bjStake * 2, UIFactory.Col("승리! " + p + " 대 " + d + " ×2", UIFactory.Good));
            else if (p == d) Settle(bjStake, bjStake, "무승부 " + p + " 대 " + d + " · 판돈 반환");
            else Settle(bjStake, 0, UIFactory.Col("패배... " + p + " 대 " + d, UIFactory.Bad));
            BjButtons();
        }

        void Update()
        {
            if (!IsOpen) return;
            // 잔액/버튼 상태 주기적 동기화 (포션 사용 등 외부 변화 반영)
            if (Time.frameCount % 20 == 0) RefreshInfo();
            if (!busy && GameInput.Down(GameInput.K.Space) && Time.frameCount - openedFrame > 2)
            {
                if (game == Game.Slots) Spin();
                else if (game == Game.Roulette) RouletteSpin();
            }
        }
    }
}
