using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    public enum QuestState { Locked, Available, Active, ReadyToTurnIn, Done }
    public enum QuestKind { Kill, Collect }

    public class Quest
    {
        public string id, title, giver, desc, target;
        public QuestKind kind;
        public int required, progress;
        public int rewardGold, rewardExp, rewardItemCount;
        public string rewardItem;
        public string prereq;
        public bool main;
        public QuestState state = QuestState.Locked;
        public string offerText, activeText, completeText;

        public string TargetName
        {
            get
            {
                if (kind == QuestKind.Kill) { var e = EnemyDB.Get(target); return e != null ? e.name : target; }
                var i = ItemDB.Get(target); return i != null ? i.name : target;
            }
        }

        public string ProgressText
        {
            get
            {
                string verb = kind == QuestKind.Kill ? "처치" : "수집";
                return TargetName + " " + verb + " " + Mathf.Min(progress, required) + "/" + required;
            }
        }

        public string GoalText
        {
            get { return TargetName + " " + required + (kind == QuestKind.Kill ? "마리 처치" : "개 모으기"); }
        }

        public string RewardText
        {
            get
            {
                var parts = new List<string>();
                if (rewardGold > 0) parts.Add(rewardGold + "G");
                if (rewardExp > 0) parts.Add("경험치 " + rewardExp);
                var d = ItemDB.Get(rewardItem);
                if (d != null) parts.Add(d.name + (rewardItemCount > 1 ? " x" + rewardItemCount : ""));
                return string.Join(", ", parts.ToArray());
            }
        }
    }

    [Serializable]
    public class QuestSave { public string id; public int state; public int progress; }

    public class QuestManager
    {
        public readonly List<Quest> quests = new List<Quest>();
        public event Action Changed;
        readonly Inventory inv;

        public QuestManager(Inventory inv)
        {
            this.inv = inv;
            inv.Changed += RefreshCollect;
            Define();
        }

        void Define()
        {
            // ── 메인 퀘스트 (촌장 에드릭) ──
            quests.Add(new Quest
            {
                id = "m1", main = true, giver = "chief", title = "초원의 골칫거리", kind = QuestKind.Kill, target = "slime", required = 5,
                rewardGold = 120, rewardExp = 60, rewardItem = "potion_s", rewardItemCount = 3,
                desc = "북쪽 초원의 슬라임들이 밭을 망치고 있다. 슬라임 5마리를 처치하자.",
                offerText = "요즘 북쪽 초원에 슬라임이 부쩍 늘어 밭이 엉망이라네. 자네가 다섯 마리만 정리해 줄 수 있겠나?",
                activeText = "슬라임은 느리지만 방심은 금물이네. 체력이 떨어지면 포션을 쓰게(1번 키).",
                completeText = "벌써 해냈군! 역시 내 눈은 틀리지 않았어. 약소하지만 받아두게."
            });
            quests.Add(new Quest
            {
                id = "m2", main = true, giver = "chief", prereq = "m1", title = "숲의 울음소리", kind = QuestKind.Kill, target = "wolf", required = 6,
                rewardGold = 250, rewardExp = 180,
                desc = "속삭이는 숲의 늑대 무리가 상인들을 습격한다. 늑대 6마리를 처치하자.",
                offerText = "초원 너머 '속삭이는 숲'에서 늑대들이 상인 마차를 노리고 있네. 늑대는 빠르니 장비를 먼저 갖추는 걸 추천하지. 대장장이 브론을 찾아가 보게.",
                activeText = "늑대는 무리지어 다니네. R키 회전베기로 한꺼번에 상대하게.",
                completeText = "상인들이 다시 길을 오갈 수 있겠군. 마을을 대표해 고맙네."
            });
            quests.Add(new Quest
            {
                id = "m3", main = true, giver = "chief", prereq = "m2", title = "고블린 야영지 소탕", kind = QuestKind.Kill, target = "goblin", required = 8,
                rewardGold = 450, rewardExp = 450, rewardItem = "potion_l", rewardItemCount = 3,
                desc = "숲 북쪽에 고블린들이 야영지를 세웠다. 고블린 전사 8마리를 처치하자.",
                offerText = "나쁜 소식일세. 늑대들이 날뛴 이유가 있었어. 숲 북쪽에 고블린 야영지가 생겼다네. 놈들의 전력을 줄여주게.",
                activeText = "고블린 전사는 공격이 매섭네. 방어구를 꼭 챙기게.",
                completeText = "야영지가 크게 흔들렸다는 보고가 들어왔네! 하지만... 아직 끝이 아니야."
            });
            quests.Add(new Quest
            {
                id = "m4", main = true, giver = "chief", prereq = "m3", title = "고블린 왕 그락", kind = QuestKind.Kill, target = "goblin_king", required = 1,
                rewardGold = 1000, rewardExp = 500,
                desc = "야영지 가장 북쪽 제단의 고블린 왕 그락을 쓰러뜨리자. 바닥의 붉은 원을 피할 것!",
                offerText = "놈들의 우두머리, 고블린 왕 그락이 북쪽 제단에 있네. 그가 땅을 내려찍기 전 바닥이 붉게 물드니 반드시 피하게. 마을의 운명을 자네에게 맡기겠네.",
                activeText = "그락은 체력이 줄면 더 난폭해진다고 하네. 포션을 넉넉히 챙기게.",
                completeText = "자네가... 정말로 그락을 쓰러뜨렸군! 새벽빛 마을은 영원히 자네를 기억할 걸세! 그런데... 제단 북쪽의 봉인이 풀렸다는 소식이 있네. 광장 동쪽의 학자 엘라라를 찾아가 보게."
            });

            // ── 후반 메인 퀘스트 (학자 엘라라) ──
            quests.Add(new Quest
            {
                id = "m5", main = true, giver = "scholar", prereq = "m4", title = "봉인 너머의 망령", kind = QuestKind.Kill, target = "skeleton", required = 8,
                rewardGold = 800, rewardExp = 900, rewardItem = "elixir", rewardItemCount = 2,
                desc = "그락이 쓰러지며 풀린 봉인 너머 '잊혀진 폐허'. 되살아난 해골 병사 8마리를 처치하자.",
                offerText = "그락은 봉인의 '쐐기'였어요. 그가 쓰러지자 제단 북쪽의 봉인이 풀렸고, 폐허의 망자들이 깨어나고 있어요. 해골 병사들부터 정리해 줄 수 있나요?",
                activeText = "해골 병사는 방어가 단단해요. 룬 장비를 갖추면 한결 수월할 거예요. 브론 씨가 새 장비를 들여놨대요.",
                completeText = "대단해요! 망자들의 움직임이 눈에 띄게 줄었어요. 이제 남은 건... 그들을 부리는 자뿐이에요."
            });
            quests.Add(new Quest
            {
                id = "m6", main = true, giver = "scholar", prereq = "m5", title = "망령 군주 모르가스", kind = QuestKind.Kill, target = "lich", required = 1,
                rewardGold = 3000, rewardExp = 1500,
                desc = "폐허 가장 깊은 곳의 망령 군주 모르가스를 쓰러뜨리자. 보라색 구체와 바닥의 어둠을 피할 것!",
                offerText = "고문헌에 따르면 폐허의 주인은 '망령 군주 모르가스'. 사방으로 저주의 구체를 흩뿌리고, 발밑에 어둠을 불러낸대요. 체력이 줄면 해골 병사를 불러내기도 하고요. 부디 조심하세요.",
                activeText = "구체는 대시(Shift)로 뚫고 지나갈 수 있어요. 보라색 원이 보이면 바로 벗어나세요!",
                completeText = "해냈군요...! 모르가스의 저주가 걷히고 있어요. 당신은 두 번이나 이 땅을 구했어요. 이 이야기는 제가 꼭 책으로 남길게요."
            });

            // ── 서브 퀘스트 ──
            quests.Add(new Quest
            {
                id = "s_herb", giver = "herbalist", title = "달빛 약초 채집", kind = QuestKind.Collect, target = "herb", required = 5,
                rewardGold = 80, rewardExp = 50, rewardItem = "mana_potion", rewardItemCount = 3,
                desc = "슬라임 초원에서 빛나는 달빛 약초 5개를 모아 리나에게 가져가자.",
                offerText = "포션 재료인 '달빛 약초'가 다 떨어졌어요. 초원에 반짝이는 풀이 보이면 다섯 개만 부탁해도 될까요?",
                activeText = "약초는 초원 여기저기서 은은하게 빛나요. 가까이 가면 자동으로 주워져요.",
                completeText = "와, 싱싱하네요! 이걸로 좋은 약을 만들 수 있겠어요. 감사의 표시예요."
            });
            quests.Add(new Quest
            {
                id = "s_pelt", giver = "blacksmith", title = "대장장이의 부탁", kind = QuestKind.Collect, target = "wolf_pelt", required = 3,
                rewardGold = 60, rewardExp = 90, rewardItem = "leather_armor", rewardItemCount = 1,
                desc = "브론에게 늑대 가죽 3장을 가져다주자.",
                offerText = "갑옷 안감으로 쓸 늑대 가죽이 필요하네. 3장만 구해오면 가죽 갑옷 하나 뚝딱 만들어 주지.",
                activeText = "늑대는 숲에 있네. 가죽이 안 나오면 좀 더 잡아보게.",
                completeText = "좋은 가죽이군! 약속대로 갑옷을 주지. 인벤토리(I)에서 장착하게."
            });
            quests.Add(new Quest
            {
                id = "s_coin", giver = "gambler", title = "잃어버린 행운", kind = QuestKind.Collect, target = "lucky_coin", required = 1,
                rewardGold = 300, rewardExp = 120,
                desc = "고블린에게 빼앗긴 잭의 '행운의 금화'를 되찾자. (고블린 처치 시 확률 획득)",
                offerText = "이봐 친구, 내 '행운의 금화'를 고블린 녀석이 채갔어. 그걸 찾아주면... 하이롤러 테이블을 열어주지. 판돈이 훨씬 커진다고, 흐흐.",
                activeText = "고블린 전사들 중 하나가 갖고 있을 거야. 반짝이는 걸 좋아하거든.",
                completeText = "오오, 내 금화! 약속대로 하이롤러 테이블을 열어주지. 이제 최대 5000G까지 걸 수 있다고!"
            });
            quests.Add(new Quest
            {
                id = "s_ear", giver = "guard", title = "야영지 정찰 보고", kind = QuestKind.Collect, target = "goblin_ear", required = 6,
                rewardGold = 150, rewardExp = 260, rewardItem = "chain_armor", rewardItemCount = 1,
                desc = "경비병 토르에게 고블린 귀 6개를 가져가 야영지의 규모를 보고하자.",
                offerText = "왕국에 고블린 야영지의 규모를 보고해야 하오. 토벌의 증표인 고블린 귀 6개를 가져다주면, 예비로 둔 사슬 갑옷을 내어 주겠소.",
                activeText = "고블린 귀는 고블린 전사를 쓰러뜨리면 얻을 수 있소. 상점에 팔지 않도록 주의하시오.",
                completeText = "훌륭하오. 이 정도면 왕국도 지원군을 보내겠지. 약속한 갑옷이오."
            });
            quests.Add(new Quest
            {
                id = "s_doll", giver = "kid", title = "피피의 보물", kind = QuestKind.Collect, target = "doll", required = 1,
                rewardGold = 120, rewardExp = 120, rewardItem = "potion_l", rewardItemCount = 2,
                desc = "꼬마 피피가 숲 서쪽에서 잃어버린 헝겊 인형을 찾아주자. (미니맵의 분홍 점)",
                offerText = "훌쩍... 숲에서 놀다가 인형을 떨어뜨렸어. 서쪽 큰 바위 근처였는데... 늑대가 무서워서 못 가겠어. 찾아줄 수 있어?",
                activeText = "숲 서쪽 큰 바위 근처야! 미니맵에 분홍색 점으로 보일 거야.",
                completeText = "내 인형이다!! 고마워 형아(누나)! 이거, 엄마가 비상용으로 준 약인데 형아가 더 필요할 것 같아!"
            });
            quests.Add(new Quest
            {
                id = "s_ecto", giver = "scholar", prereq = "m4", title = "망령 연구", kind = QuestKind.Collect, target = "ecto", required = 4,
                rewardGold = 600, rewardExp = 450, rewardItem = "elixir", rewardItemCount = 3,
                desc = "학자 엘라라의 연구를 위해 울부짖는 망령에게서 망령의 정수 4개를 모으자.",
                offerText = "망령의 정수를 분석하면 엘릭서를 더 만들 수 있을 거예요. 폐허의 망령들에게서 4개만 구해 주실래요? 멀리서 구체를 쏘니까 조심하시고요.",
                activeText = "망령은 거리를 벌리려고 해요. 대시로 파고들어서 붙어 싸우세요!",
                completeText = "완벽한 표본이에요! 약속대로 갓 조제한 엘릭서를 드릴게요."
            });
        }

        public Quest Get(string id)
        {
            for (int i = 0; i < quests.Count; i++) if (quests[i].id == id) return quests[i];
            return null;
        }

        public bool IsActive(string id) { var q = Get(id); return q != null && (q.state == QuestState.Active || q.state == QuestState.ReadyToTurnIn); }
        public bool IsDone(string id) { var q = Get(id); return q != null && q.state == QuestState.Done; }

        public void ResetAll()
        {
            foreach (var q in quests) { q.progress = 0; q.state = QuestState.Locked; }
            UpdateUnlocks();
        }

        public void UpdateUnlocks()
        {
            foreach (var q in quests)
                if (q.state == QuestState.Locked && (string.IsNullOrEmpty(q.prereq) || IsDone(q.prereq)))
                    q.state = QuestState.Available;
            Notify();
        }

        /// <summary>NPC가 보여줄 대표 퀘스트 (완료 가능 > 진행 중 > 수락 가능)</summary>
        public Quest QuestFor(string npcId)
        {
            Quest best = null; int bestRank = 0;
            foreach (var q in quests)
            {
                if (q.giver != npcId) continue;
                int r = q.state == QuestState.ReadyToTurnIn ? 3 : q.state == QuestState.Active ? 2 : q.state == QuestState.Available ? 1 : 0;
                if (r > bestRank) { bestRank = r; best = q; }
            }
            return best;
        }

        /// <summary>NPC 머리 위 표시: "!" 수락 가능, "?" 보고 가능</summary>
        public string MarkerFor(string npcId)
        {
            var q = QuestFor(npcId);
            if (q == null) return "";
            if (q.state == QuestState.ReadyToTurnIn) return "?";
            if (q.state == QuestState.Available) return "!";
            return "";
        }

        public void Accept(Quest q)
        {
            if (q.state != QuestState.Available) return;
            q.state = QuestState.Active;
            q.progress = 0;
            if (q.kind == QuestKind.Collect) RefreshCollectOne(q);
            SoundFX.Play(Sfx.Quest);
            GameManager.I.UI.Hud.Banner("새 퀘스트", q.title);
            Notify();
        }

        public void TurnIn(Quest q)
        {
            if (q.state != QuestState.ReadyToTurnIn) return;
            if (q.kind == QuestKind.Collect && !inv.Remove(q.target, q.required)) { RefreshCollectOne(q); return; }
            q.state = QuestState.Done;
            var gm = GameManager.I;
            if (q.rewardGold > 0) gm.Stats.AddGold(q.rewardGold);
            if (!string.IsNullOrEmpty(q.rewardItem)) inv.Add(q.rewardItem, Mathf.Max(1, q.rewardItemCount));
            if (q.rewardExp > 0) gm.Stats.AddExp(q.rewardExp);
            SoundFX.Play(Sfx.Quest);
            gm.UI.Hud.Banner("퀘스트 완료!", q.title + "  (보상: " + q.RewardText + ")");
            UpdateUnlocks();
            gm.OnQuestCompleted(q);
        }

        public void OnKill(string enemyId)
        {
            bool changed = false;
            foreach (var q in quests)
            {
                if (q.state != QuestState.Active || q.kind != QuestKind.Kill || q.target != enemyId) continue;
                q.progress++;
                changed = true;
                if (q.progress >= q.required)
                {
                    q.state = QuestState.ReadyToTurnIn;
                    SoundFX.Play(Sfx.Quest, 0.7f);
                    GameManager.I.UI.Hud.Toast(UIFactory.Col("퀘스트 목표 달성: ", UIFactory.Gold) + q.title + " — 의뢰인에게 돌아가세요");
                }
            }
            if (changed) Notify();
        }

        void RefreshCollect()
        {
            bool changed = false;
            foreach (var q in quests)
                if (q.kind == QuestKind.Collect && (q.state == QuestState.Active || q.state == QuestState.ReadyToTurnIn))
                    changed |= RefreshCollectOne(q);
            if (changed) Notify();
        }

        bool RefreshCollectOne(Quest q)
        {
            int before = q.progress; var s = q.state;
            q.progress = Mathf.Min(inv.Count(q.target), q.required);
            q.state = q.progress >= q.required ? QuestState.ReadyToTurnIn : QuestState.Active;
            if (s == QuestState.Active && q.state == QuestState.ReadyToTurnIn && GameManager.I != null && GameManager.I.UI != null)
                GameManager.I.UI.Hud.Toast(UIFactory.Col("퀘스트 목표 달성: ", UIFactory.Gold) + q.title + " — 의뢰인에게 돌아가세요");
            return before != q.progress || s != q.state;
        }

        public List<QuestSave> Save()
        {
            var list = new List<QuestSave>();
            foreach (var q in quests) list.Add(new QuestSave { id = q.id, state = (int)q.state, progress = q.progress });
            return list;
        }

        public void Load(List<QuestSave> data)
        {
            ResetAll();
            if (data == null) return;
            foreach (var s in data)
            {
                var q = Get(s.id);
                if (q == null) continue;
                q.state = (QuestState)s.state;
                q.progress = s.progress;
            }
            UpdateUnlocks();
        }

        void Notify() { if (Changed != null) Changed(); }
    }
}
