using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>NPC별 대화 트리. 선택지에 따라 퀘스트/상점/도박/휴식 등으로 분기</summary>
    public static class NpcDialogues
    {
        static GameManager GM { get { return GameManager.I; } }
        static DialogueUI D { get { return GameManager.I.UI.Dialogue; } }

        static readonly string[] BlacksmithBase = { "iron_sword", "steel_blade", "dragon_sword", "leather_armor", "chain_armor", "knight_armor" };
        static readonly string[] BlacksmithRune = { "iron_sword", "steel_blade", "dragon_sword", "rune_blade", "leather_armor", "chain_armor", "knight_armor", "rune_armor" };
        static readonly string[] HerbBase = { "potion_s", "potion_l", "mana_potion", "return_scroll" };
        static readonly string[] HerbElixir = { "potion_s", "potion_l", "mana_potion", "elixir", "return_scroll" };
        // 그락을 쓰러뜨리면 봉인이 풀리며 새 물건이 입고됨
        static string[] BlacksmithShop { get { return GM.BossDefeated ? BlacksmithRune : BlacksmithBase; } }
        static string[] HerbShop { get { return GM.BossDefeated ? HerbElixir : HerbBase; } }
        public const int InnPrice = 30;

        public static void Open(NPC n)
        {
            switch (n.id)
            {
                case "chief": Chief(n); break;
                case "blacksmith": Blacksmith(n); break;
                case "herbalist": Herbalist(n); break;
                case "gambler": Gambler(n); break;
                case "innkeeper": Innkeeper(n); break;
                case "guard": Guard(n); break;
                case "kid": Kid(n); break;
                case "scholar": Scholar(n); break;
            }
        }

        static DialogueChoice C(string label, Action a) { return new DialogueChoice(label, a); }
        static DialogueChoice Bye(string label = "안녕히 계세요") { return new DialogueChoice(label, () => D.Close()); }

        /// <summary>해당 NPC의 퀘스트 상태에 따른 선택지를 추가</summary>
        static void AddQuestChoices(NPC n, List<DialogueChoice> list, Action back)
        {
            var q = GM.Quests.QuestFor(n.id);
            if (q == null) return;
            if (q.state == QuestState.Available)
                list.Insert(0, C("<color=#FFD24D>[!] " + q.title + "</color>", () => Offer(n, q, back)));
            else if (q.state == QuestState.Active)
                list.Insert(0, C("[진행 중] " + q.title, () =>
                    D.Show(n, q.activeText + "\n\n<color=#9FB2CC>" + q.ProgressText + "</color>", new List<DialogueChoice> { C("알겠습니다", back) })));
            else if (q.state == QuestState.ReadyToTurnIn)
                list.Insert(0, C("<color=#7CFF8A>[?] 의뢰를 완수했습니다!</color>", () =>
                {
                    GM.Quests.TurnIn(q);
                    if (q.state == QuestState.Done)
                        D.Show(n, q.completeText + "\n\n<color=#FFD24D>보상: " + q.RewardText + "</color>", new List<DialogueChoice> { C("감사합니다", back) });
                    else back();
                }));
        }

        static string AcceptLine(string id)
        {
            switch (id)
            {
                case "chief": return "고맙네! 조심해서 다녀오게.";
                case "blacksmith": return "좋아, 기다리고 있겠네. 망치를 달궈 두지!";
                case "herbalist": return "정말요? 고마워요! 무리하지는 마세요.";
                case "gambler": return "역시 내 친구야! 행운을 빌지, 흐흐.";
                case "guard": return "믿고 맡기겠소. 무운을 비오.";
                case "kid": return "진짜?! 형아(누나) 최고야! 꼭 찾아줘!";
                case "scholar": return "감사해요! 무사히 돌아오세요. 기록해 둘게요.";
                default: return "고마워요! 조심해서 다녀오세요.";
            }
        }

        static void Offer(NPC n, Quest q, Action back)
        {
            string text = q.offerText +
                          "\n\n<color=#9FB2CC>목표: " + q.GoalText + "</color>\n<color=#FFD24D>보상: " + q.RewardText + "</color>";
            D.Show(n, text, new List<DialogueChoice>
            {
                C("맡겨주세요!", () => { GM.Quests.Accept(q); D.Show(n, AcceptLine(n.id) + " <color=#9FB2CC>(Q키로 퀘스트 목록 확인)</color>", new List<DialogueChoice> { C("다녀오겠습니다", back) }); }),
                C("조금 생각해 볼게요", back)
            });
        }

        // ─────────────── 촌장 ───────────────
        static void Chief(NPC n)
        {
            var qm = GM.Quests;
            string greet;
            if (GM.TrueBossDefeated) greet = "두 번이나 이 땅을 구한 영웅이여! 이제 새벽빛 마을에 진정한 아침이 찾아왔네. 편히 쉬다 가게.";
            else if (GM.BossDefeated) greet = "오오, 새벽빛 마을의 영웅이 오셨군! 그런데 제단 북쪽의 봉인이 풀렸다는 소식이 있네. 학자 엘라라가 무언가 알고 있는 듯하더군.";
            else if (qm.IsDone("m3")) greet = "이제 남은 건 고블린 왕 그락뿐일세. 준비는 충분한가?";
            else if (qm.IsDone("m1")) greet = "자네가 온 뒤로 마을에 활기가 돌기 시작했네. 하지만 아직 위협이 남아 있어.";
            else greet = "어서 오게, 젊은 모험가여. 나는 이 마을의 촌장 에드릭이라네. 요즘 마을 밖이 영 뒤숭숭해서 걱정이야.";

            var list = new List<DialogueChoice>();
            list.Add(C("이 마을에 대해 알려주세요", () => D.ShowSequence(n, new[]
            {
                "새벽빛 마을은 왕국 북쪽 변방의 작은 마을이라네. 해가 가장 먼저 뜨는 곳이라 이런 이름이 붙었지.",
                "마을에는 대장장이 브론, 약초상 리나, 여관 '졸린 곰'의 마사, 그리고 떠돌이 학자 엘라라가 머물고 있네. 필요한 게 있으면 찾아가 보게.",
                "아, 천막의 잭은... 도박사야. 가끔 큰돈을 버는 사람도 있다지만, 대부분은 빈털터리로 나오더군. 적당히 하게나."
            }, () => Chief(n))));
            list.Add(C("조작법을 다시 알려주세요", () => D.ShowSequence(n, new[]
            {
                "<color=#FFD24D>WASD / 방향키</color>: 이동    <color=#FFD24D>Space / J / 좌클릭</color>: 공격\n<color=#FFD24D>R</color>: 회전베기 (MP " + PlayerController.SkillMpCost + ")    <color=#FFD24D>Shift</color>: 대시(무적)",
                "<color=#FFD24D>1</color>: 체력 포션    <color=#FFD24D>2</color>: 마나 포션    <color=#FFD24D>E</color>: 대화/상호작용\n<color=#FFD24D>I</color>: 인벤토리    <color=#FFD24D>Q</color>: 퀘스트    <color=#FFD24D>Esc</color>: 메뉴"
            }, () => Chief(n))));
            list.Add(Bye());
            AddQuestChoices(n, list, () => Chief(n));
            D.Show(n, greet, list);
        }

        // ─────────────── 대장장이 ───────────────
        static void Blacksmith(NPC n)
        {
            var w = GM.Inv.Weapon;
            string greet = w != null && w.id == "wood_sword"
                ? "흠, 그 목검으로 몬스터를 잡겠다고? 허허, 용기는 가상하군. 내 작품들 좀 보고 가게."
                : "어서 오게! 쇠는 뜨거울 때 두드려야 하는 법. 오늘은 뭘 찾나?";
            if (w != null && w.id == "dragon_sword") greet = "그... 그건 용살자의 검 아닌가! 평생 한 번 보기도 힘든 물건을 들고 다니는군!";

            var list = new List<DialogueChoice>
            {
                C("장비를 사고 싶어요", () => GM.UI.Shop.Open("브론의 대장간", BlacksmithShop, false)),
                C("물건을 팔고 싶어요", () => GM.UI.Shop.Open("브론의 대장간", BlacksmithShop, true)),
                C("좋은 장비를 추천해 주세요", () =>
                {
                    int lv = GM.Stats.level;
                    string rec = lv < 3 ? "초원이라면 철검 하나면 충분하네. 150G면 살 수 있지."
                               : lv < 6 ? "숲의 늑대는 빠르고 이빨이 날카롭지. 가죽 갑옷은 꼭 챙기게. 늑대 가죽을 가져오면 하나 만들어 주지."
                               : lv < 9 ? "고블린 전사를 상대하려면 강철 대검과 사슬 갑옷을 추천하네."
                               : !GM.BossDefeated ? "그락을 상대한다고? 기사의 판금갑옷 정도는 입어야 살아 돌아올 걸세."
                               : "폐허의 망자들에겐 보통 쇠붙이가 잘 안 먹힌다더군. 엘라라가 알려준 룬 문자를 새긴 '룬 새김 대검'과 '룬 판금갑옷'을 새로 만들었네!";
                    D.Show(n, rec, new List<DialogueChoice> { C("참고할게요", () => Blacksmith(n)) });
                }),
                Bye("다음에 올게요")
            };
            AddQuestChoices(n, list, () => Blacksmith(n));
            D.Show(n, greet, list);
        }

        // ─────────────── 약초상 ───────────────
        static void Herbalist(NPC n)
        {
            string greet = GM.Stats.hp < GM.Stats.MaxHP * 0.4f
                ? "어머, 많이 다치셨네요! 포션을 챙기시는 게 좋겠어요. 마을 안에서는 상처가 빨리 아물기도 하고요."
                : "안녕하세요! 리나의 약초 가게예요. 오늘 달인 포션은 특히 효과가 좋답니다.";
            var list = new List<DialogueChoice>
            {
                C("포션을 사고 싶어요", () => GM.UI.Shop.Open("리나의 약초 가게", HerbShop, false)),
                C("재료를 팔고 싶어요", () => GM.UI.Shop.Open("리나의 약초 가게", HerbShop, true)),
                C("귀환 주문서가 뭐예요?", () => D.Show(n, "읽는 즉시 이 마을로 돌아오는 마법 두루마리예요. 숲 깊은 곳에서 위험할 때 쓰면 목숨을 구할 수 있죠. 인벤토리(I)에서 사용하세요.",
                    new List<DialogueChoice> { C("유용하겠네요", () => Herbalist(n)) })),
                Bye("다음에 올게요")
            };
            AddQuestChoices(n, list, () => Herbalist(n));
            D.Show(n, greet, list);
        }

        // ─────────────── 도박사 ───────────────
        static void Gambler(NPC n)
        {
            var g = GM.Gamble;
            string greet;
            if (g.gamesPlayed == 0) greet = "헤이, 모험가 친구! 나는 '럭키' 잭. 운을 시험해 보고 싶다면 제대로 찾아왔어. 슬롯, 주사위, 블랙잭, 룰렛... 뭐든 있지.";
            else if (g.Net <= -1000) greet = "음... 친구, 오늘 꽤 많이 잃었군. 진심으로 말하는데, 오늘은 여기까지 하는 게 어때? 돈은 사냥으로도 벌 수 있잖아.";
            else if (g.Net >= 1000) greet = "이런이런, 오늘의 행운아가 오셨군! 하지만 기억해, 행운은 언제든 등을 돌릴 수 있다고.";
            else greet = "또 왔군! 오늘은 어떤 게임으로 운을 시험해 볼 텐가?";

            var list = new List<DialogueChoice>
            {
                C("<color=#FFD24D>슬롯머신</color> 돌리기", () => GM.UI.Gamble.Open(GambleUI.Game.Slots)),
                C("<color=#FFD24D>주사위 하이로우</color> 하기", () => GM.UI.Gamble.Open(GambleUI.Game.Dice)),
                C("<color=#FFD24D>블랙잭</color> 한 판", () => GM.UI.Gamble.Open(GambleUI.Game.Blackjack)),
                C("<color=#FFD24D>룰렛</color> 돌리기", () => GM.UI.Gamble.Open(GambleUI.Game.Roulette)),
                C("게임 규칙과 확률이 궁금해요", () => D.ShowSequence(n, new[]
                {
                    "<color=#FFD24D>슬롯머신</color>: 같은 그림 3개면 배당! 7-7-7은 무려 50배. 2개만 같아도 1.3배를 돌려주지.",
                    "<color=#FFD24D>주사위 하이로우</color>: 주사위 2개의 합을 맞혀. 낮음(2~6)·높음(8~12)은 2.2배, 딱 7이면 5.5배.",
                    "<color=#FFD24D>블랙잭</color>: 21에 가까운 쪽이 이겨. 딜러는 17 이상이면 멈추지. 블랙잭(첫 두 장 21)은 2.5배!",
                    "<color=#FFD24D>룰렛</color>: 0부터 36까지, 공이 멈출 칸을 맞혀. 빨강·검정·홀짝·절반은 2배, 12칸 묶음은 3배, 숫자 하나를 맞히면 무려 36배! 단, 0이 나오면 숫자 0에 건 사람 말고는 전부 내 몫이지.",
                    "솔직히 말해줄까? 모든 게임은 장기적으로 <color=#FF7777>하우스(나)</color>가 이기게 설계되어 있어. 슬롯·주사위는 판돈의 약 91%, 룰렛은 약 97%, 블랙잭은 잘해야 99% 정도가 돌아가지. 재미로만 즐기라고, 친구."
                }, () => Gambler(n))),
                C("오늘 전적 보기", () => D.Show(n,
                    "플레이 " + g.gamesPlayed + "판 · 승리 " + g.wins + "판\n총 베팅 " + g.totalBet + "G · 총 획득 " + g.totalWon + "G\n순이익: " +
                    (g.Net >= 0 ? UIFactory.Col("+" + g.Net + "G", UIFactory.Good) : UIFactory.Col(g.Net + "G", UIFactory.Bad)) +
                    (g.biggestWin > 0 ? "\n최고 당첨: " + g.biggestWin + "G" : ""),
                    new List<DialogueChoice> { C("돌아가기", () => Gambler(n)) })),
                Bye("다음에 할게요")
            };
            if (GM.HighRoller) list.Insert(4, C("<color=#C58CFF>[하이롤러 테이블 개방됨]</color> 최대 5000G", () => GM.UI.Gamble.Open(GambleUI.Game.Slots)));
            AddQuestChoices(n, list, () => Gambler(n));
            D.Show(n, greet, list);
        }

        // ─────────────── 여관 주인 ───────────────
        static readonly string[] Rumors =
        {
            "슬라임 젤리는 모아서 팔면 쏠쏠하대요. 리나나 브론에게 팔 수 있어요.",
            "숲의 늑대는 무리지어 다녀요. 회전베기(R)로 한 번에 베어버리면 편하다던데요?",
            "대시(Shift)를 쓰는 순간에는 공격을 맞지 않는대요. 고블린 왕의 내려찍기도 피할 수 있다나?",
            "잭의 천막에서 7-7-7을 띄운 사람이 있었다는 소문이 있어요. 물론 그 뒤로 전부 잃었다지만요.",
            "고블린 전사가 반짝이는 금화를 들고 다니는 걸 본 사람이 있대요. 잭이 잃어버린 그 금화 아닐까요?",
            "고블린 왕 그락은 체력이 줄어들면 분노해서 더 빨라진대요. 큰 포션을 넉넉히 챙기세요.",
            "마을 안에서는 체력과 마나가 금방 회복돼요. 급할 땐 귀환 주문서를 쓰세요.",
            "제단 북쪽에 보라색 빛이 일렁이는 문이 있대요. 학자 엘라라 씨가 그 문을 연구하러 왔다나 봐요.",
            "피피가 요즘 시무룩해요. 숲에서 소중한 인형을 잃어버렸대요. 누가 좀 찾아주면 좋을 텐데...",
            "경비병 토르 씨가 고블린 귀를 모으고 있대요. 왕국에 보고할 증거가 필요하다나요?"
        };

        static void Innkeeper(NPC n)
        {
            var list = new List<DialogueChoice>
            {
                C("휴식하기 (" + InnPrice + "G · HP/MP 완전 회복)", () =>
                {
                    var s = GM.Stats;
                    if (s.hp >= s.MaxHP && s.mp >= s.MaxMP) { D.Show(n, "보아하니 기운이 넘치시는데요? 쉬실 필요는 없어 보여요.", new List<DialogueChoice> { C("그렇네요", () => Innkeeper(n)) }); return; }
                    if (!s.Spend(InnPrice)) { D.Show(n, "어머, 돈이 조금 모자라시네요. 30G만 있으면 되는데...", new List<DialogueChoice> { C("다음에 올게요", () => Innkeeper(n)) }); return; }
                    s.hp = s.MaxHP; s.mp = s.MaxMP; s.Notify();
                    SoundFX.Play(Sfx.LevelUp, 0.6f);
                    GM.UI.FadeFlash();
                    D.Show(n, "푹 주무셨나요? 따뜻한 수프도 드셨으니 힘이 나실 거예요! (HP/MP 완전 회복)", new List<DialogueChoice> { C("개운하네요!", () => Innkeeper(n)) });
                }),
                C("모험을 기록하기 (저장)", () =>
                {
                    GM.Save();
                    D.Show(n, "모험 일지에 꼼꼼히 적어두었어요. (저장 완료)", new List<DialogueChoice> { C("고마워요", () => Innkeeper(n)) });
                }),
                C("요즘 도는 소문 없나요?", () => D.Show(n, Rumors[UnityEngine.Random.Range(0, Rumors.Length)], new List<DialogueChoice>
                {
                    C("다른 소문도 들려주세요", () => Innkeeper(n)),
                })),
                Bye("다음에 올게요")
            };
            D.Show(n, "어서 오세요, 여관 '졸린 곰'이에요! 푹신한 침대와 따뜻한 수프가 준비되어 있답니다.", list);
        }

        // ─────────────── 경비병 ───────────────
        static void Guard(NPC n)
        {
            int lv = GM.Stats.level;
            string msg = lv < 3 ? "북쪽 문을 나서면 슬라임 초원이오. 슬라임은 약하지만 여러 마리에게 둘러싸이지 않도록 조심하시오."
                       : lv < 6 ? "초원 너머 숲에는 늑대가 우글거리오. 레벨 3 이상이면 도전해 볼 만하지."
                       : lv < 9 ? "숲 북쪽 목책 너머가 고블린 야영지요. 놈들은 무리지어 싸우니 포션을 넉넉히 챙기시오."
                       : "당신이라면 그락을 쓰러뜨릴 수 있을지도 모르겠소. 행운을 비오, 용사여.";
            var glist = new List<DialogueChoice>
            {
                C("몬스터에 대해 알려주세요", () => D.ShowSequence(n, new[]
                {
                    "<color=#7CFF8A>슬라임</color>(초원): 느리고 약하오. 초보자 연습 상대로 제격이지.",
                    "<color=#C8C8D0>회색 늑대</color>(숲): 발이 빠르고 무리지어 다니오. 가죽은 대장장이가 좋아하지.",
                    "<color=#B5C66A>고블린 전사</color>(야영지): 몽둥이질이 매섭소. 방어구 없이는 버티기 힘들 거요.",
                    "<color=#C58CFF>고블린 왕 그락</color>(제단): 바닥이 붉게 물들면 즉시 원 밖으로 벗어나시오. 대시(Shift)도 도움이 될 거요.",
                    "<color=#E8E2D0>해골 병사</color>·<color=#8FF0F0>울부짖는 망령</color>(폐허): 봉인 너머의 망자들이오. 망령은 멀리서 구체를 쏘니 파고들어 싸우시오."
                }, () => Guard(n))),
                Bye("수고하세요")
            };
            AddQuestChoices(n, glist, () => Guard(n));
            D.Show(n, msg, glist);
        }

        // ─────────────── 마을 아이 ───────────────
        static readonly string[] KidLines =
        {
            "형아(누나)는 모험가야? 우와, 칼 멋있다!",
            "엄마가 잭 아저씨 천막엔 가지 말래. 거기 가면 돈이 없어진대!",
            "슬라임은 말랑말랑해! 근데 만지면 끈적끈적해...",
            "나도 크면 모험가가 될 거야! 고블린 왕도 혼내줄 거야!",
            "리나 누나가 초원에 반짝이는 풀이 있대. 그거 엄청 귀한 거래!",
            "촌장 할아버지 머리 위에 느낌표(!)가 보이면 부탁할 게 있다는 뜻이래!"
        };

        static void Kid(NPC n)
        {
            string line = GM.BossDefeated ? "형아(누나)가 고블린 왕을 물리쳤다며? 진짜 최고야! 나중에 나도 제자로 받아줘!"
                                          : KidLines[UnityEngine.Random.Range(0, KidLines.Length)];
            var q = GM.Quests.QuestFor(n.id);
            if (q != null && q.state == QuestState.Available) line = "훌쩍... 형아(누나), 나 인형 잃어버렸어...";
            var klist = new List<DialogueChoice> { C("또 이야기하자", () => Kid(n)), Bye("잘 놀아~") };
            AddQuestChoices(n, klist, () => Kid(n));
            D.Show(n, line, klist);
        }

        // ─────────────── 학자 ───────────────
        static void Scholar(NPC n)
        {
            string greet;
            if (GM.TrueBossDefeated) greet = "모르가스가 사라진 뒤로 폐허에 새소리가 들린대요. 당신의 이야기, 벌써 세 장째 쓰고 있어요!";
            else if (GM.BossDefeated) greet = "기다리고 있었어요! 봉인이 풀린 뒤로 폐허 쪽에서 불길한 기운이 흘러나오고 있어요.";
            else greet = "안녕하세요, 저는 고대 유적을 연구하는 학자 엘라라예요. 고블린 왕의 제단 북쪽에 아주 오래된 봉인이 있다는 걸 아시나요?";
            var list = new List<DialogueChoice>
            {
                C("봉인과 폐허에 대해 알려주세요", () => D.ShowSequence(n, new[]
                {
                    "옛날 이 땅에는 '망령 군주 모르가스'라는 강령술사가 있었어요. 죽은 자를 부려 왕국을 위협했죠.",
                    "기사단이 그를 폐허 깊은 곳에 봉인했는데... 문헌에 따르면 그 봉인은 '가장 강한 자의 기운'으로 유지된대요.",
                    GM.BossDefeated
                        ? "그락이 제단에 자리 잡으면서 그 기운이 봉인을 지탱했던 거예요. 그가 쓰러진 지금, <color=#C58CFF>봉인은 풀렸어요.</color>"
                        : "지금은 고블린 왕 그락의 힘이 봉인을 지탱하고 있어요. 아이러니하죠? 그가 쓰러지면... <color=#C58CFF>봉인도 풀릴 거예요.</color>",
                    "폐허에는 해골 병사와 울부짖는 망령이 떠돌아요. 망령의 정수를 가져다주시면 엘릭서를 만들어 드릴게요."
                }, () => Scholar(n))),
                C("폐허의 적에 대해 알려주세요", () => D.ShowSequence(n, new[]
                {
                    "<color=#E8E2D0>해골 병사</color>: 녹슨 검과 방패를 든 망자예요. 방어가 단단하니 공격력을 충분히 올리세요.",
                    "<color=#8FF0F0>울부짖는 망령</color>: 거리를 두고 푸른 구체를 쏴요. 가까이 가면 뒤로 물러나니 대시로 파고드세요.",
                    "<color=#C58CFF>망령 군주 모르가스</color>: 사방으로 구체를 흩뿌리고, 발밑에 보라색 어둠을 불러내요. 체력이 70%, 35%일 때 해골 병사를 소환해요."
                }, () => Scholar(n))),
                Bye("다음에 올게요")
            };
            AddQuestChoices(n, list, () => Scholar(n));
            D.Show(n, greet, list);
        }
    }
}
