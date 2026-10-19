using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    public enum GameState { Title, Playing, Dead, Ending }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int level, exp, hp, mp, gold, baseMaxHp, baseMaxMp, baseAtk, baseDef, kills;
        public List<ItemStack> items = new List<ItemStack>();
        public string weaponId, armorId;
        public List<QuestSave> quests = new List<QuestSave>();
        public GambleStats gamble = new GambleStats();
        public bool highRoller, bossDefeated, trueBossDefeated;
        public float playTime;
    }

    /// <summary>
    /// 게임 전체를 관리. 빈 씬에서 Play만 누르면 자동으로 생성되어 월드/UI/캐릭터를 모두 만든다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }
        const string SaveKey = "MiniRPG_Save";
        const float AutoSaveInterval = 90f;

        public PlayerStats Stats;
        public Inventory Inv;
        public QuestManager Quests;
        public GambleStats Gamble;
        public PlayerController Player;
        public CameraFollow Cam;
        public Fx Fx;
        public UIManager UI;
        public GameState State = GameState.Title;
        public bool HighRoller, BossDefeated, TrueBossDefeated;
        public float PlayTime;

        readonly List<SpawnZone> zones = new List<SpawnZone>();
        SpawnZone bossZone, lichZone;
        readonly List<SpawnZone> ruinZones = new List<SpawnZone>();
        int lastZone = -1;
        float autoSaveTimer;
        Coroutine endingRoutine;

        public bool InputBlocked { get { return State != GameState.Playing || UI == null || UI.AnyModalOpen; } }

        public Enemy ActiveBoss
        {
            get
            {
                for (int i = 0; i < Enemy.Active.Count; i++)
                {
                    var e = Enemy.Active[i];
                    if (e != null && e.Def != null && e.Def.isBoss) return e;
                }
                return null;
            }
        }

        // ═══════════════════════════ 부팅 ═══════════════════════════
        /// <summary>
        /// 'Enter Play Mode Options'에서 도메인 리로드를 끈 경우에도 이전 플레이의 정적 데이터가 남지 않도록 초기화
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            I = null;
            Enemy.Active.Clear();
            NPC.All.Clear();
            Herb.All.Clear();
            WorldBuilder.HerbSpots.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (I != null) return;
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            try { Init(); }
            catch (Exception ex)
            {
                ErrorReporter.Report(bootStep, ex);
                enabled = false;   // 부팅 실패 시 Update에서 연쇄 오류가 나지 않도록 중지
            }
        }

        string bootStep = "시작";
        void Step(string name) { bootStep = name; }

        void OnDestroy() { if (I == this) I = null; }

        void Init()
        {
            Step("기본 설정");
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;

            Inv = new Inventory();
            Stats = new PlayerStats(Inv);
            Quests = new QuestManager(Inv);
            Gamble = new GambleStats();
            Stats.LeveledUp += OnLevelUp;
            Inv.Reset();
            Stats.Reset();

            Step("사운드 생성");
            SoundFX.Init(gameObject);
            Fx = gameObject.AddComponent<Fx>();

            Step("월드 생성");
            WorldBuilder.Build();
            UnityEngine.Random.InitState(Environment.TickCount);

            Step("카메라 설정");
            // ── 메인 카메라 ──
            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
                cgo.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f;
            cam.cullingMask = ~(1 << WorldBuilder.MinimapLayer);
            Cam = cam.GetComponent<CameraFollow>();
            if (Cam == null) Cam = cam.gameObject.AddComponent<CameraFollow>();

            // ── 미니맵 카메라 ──
            var rt = new RenderTexture(256, 256, 16);
            rt.name = "MinimapRT";
            var mgo = new GameObject("MinimapCamera");
            var mcam = mgo.AddComponent<Camera>();
            mcam.orthographic = true;
            mcam.orthographicSize = 32f;
            mcam.clearFlags = CameraClearFlags.SolidColor;
            mcam.backgroundColor = new Color(0.15f, 0.2f, 0.15f);
            mcam.cullingMask = ~0;
            mcam.targetTexture = rt;
            mcam.depth = -5;
            mcam.farClipPlane = 120f;
            var mm = mgo.AddComponent<MinimapCam>();

            Step("UI 생성");
            // ── UI ──
            UI = UIManager.Build(this, rt);

            Step("플레이어 생성");
            // ── 플레이어 ──
            Player = PlayerController.Create(WorldBuilder.PlayerSpawn);
            Cam.target = Player.transform;
            mm.target = Player.transform;
            Player.RefreshLook();
            Cam.Snap();

            Step("NPC 생성");
            CreateNPCs();
            Step("몬스터 배치");

            zones.Add(SpawnZone.Create("slime", new Vector3(0, 0, 50), 16f, 9, 8f));
            zones.Add(SpawnZone.Create("wolf", new Vector3(0, 0, 93), 14f, 7, 12f));
            zones.Add(SpawnZone.Create("goblin", new Vector3(0, 0, 132), 14f, 7, 15f));
            bossZone = SpawnZone.Create("goblin_king", new Vector3(0, 0, 174), 1f, 1, 9999f, false);
            // 봉인 너머 폐허 (그락 처치 후 활성화)
            ruinZones.Add(SpawnZone.Create("skeleton", new Vector3(0, 0, 218), 13f, 7, 14f));
            ruinZones.Add(SpawnZone.Create("wraith", new Vector3(0, 0, 232), 12f, 5, 16f));
            lichZone = SpawnZone.Create("lich", new Vector3(0, 0, 249), 1f, 1, 9999f, false);

            foreach (var p in WorldBuilder.HerbSpots) Herb.Create(p);
            QuestPickup.Create("doll", "s_doll", WorldBuilder.DollSpot);

            Step("타이틀 화면");
            GoToTitle();
            Step("완료");
        }

        void CreateNPCs()
        {
            NPC.Create("chief", "촌장 에드릭", "촌장", new Vector3(-8, 0, 12.8f), 180, new NPC.Look
            {
                body = new Color(0.45f, 0.3f, 0.55f), skin = new Color(0.95f, 0.8f, 0.68f), hair = new Color(0.9f, 0.9f, 0.9f),
                accent = new Color(0.9f, 0.75f, 0.3f), hat = "none", scale = 1f, beard = true
            });
            NPC.Create("innkeeper", "여관주인 마사", "여관 '졸린 곰'", new Vector3(-20, 0, 10.3f), 180, new NPC.Look
            {
                body = new Color(0.75f, 0.35f, 0.3f), skin = new Color(1f, 0.84f, 0.72f), hair = new Color(0.55f, 0.3f, 0.15f),
                accent = Color.white, hat = "bandana", scale = 0.95f, apron = true
            });
            NPC.Create("blacksmith", "대장장이 브론", "무기 · 방어구", new Vector3(6.5f, 0, 12.5f), 180, new NPC.Look
            {
                body = new Color(0.35f, 0.3f, 0.28f), skin = new Color(0.85f, 0.65f, 0.5f), hair = new Color(0.25f, 0.15f, 0.1f),
                accent = new Color(0.5f, 0.35f, 0.2f), hat = "none", scale = 1.15f, beard = true, apron = true
            });
            NPC.Create("herbalist", "약초상 리나", "포션 · 재료", new Vector3(-12, 0, 3.9f), 180, new NPC.Look
            {
                body = new Color(0.35f, 0.65f, 0.4f), skin = new Color(1f, 0.86f, 0.75f), hair = new Color(0.85f, 0.55f, 0.25f),
                accent = new Color(0.9f, 0.9f, 0.6f), hat = "hood", scale = 0.92f
            });
            NPC.Create("gambler", "'럭키' 잭", "행운의 천막 (도박)", new Vector3(17.5f, 0, 8.4f), 180, new NPC.Look
            {
                body = new Color(0.55f, 0.1f, 0.15f), skin = new Color(0.95f, 0.78f, 0.62f), hair = new Color(0.1f, 0.1f, 0.1f),
                accent = UIFactory.Gold, hat = "tophat", scale = 1f
            });
            NPC.Create("guard", "경비병 토르", "북문 경비", new Vector3(4, 0, 23.5f), 180, new NPC.Look
            {
                body = new Color(0.55f, 0.58f, 0.65f), skin = new Color(0.9f, 0.72f, 0.58f), hair = new Color(0.3f, 0.2f, 0.1f),
                accent = new Color(0.25f, 0.35f, 0.7f), hat = "helmet", scale = 1.08f
            });
            NPC.Create("scholar", "학자 엘라라", "고대 유적 연구가", new Vector3(12, 0, 3.9f), 180, new NPC.Look
            {
                body = new Color(0.28f, 0.32f, 0.6f), skin = new Color(0.98f, 0.84f, 0.74f), hair = new Color(0.75f, 0.75f, 0.85f),
                accent = new Color(0.85f, 0.75f, 0.4f), hat = "hood", scale = 0.95f
            });
            NPC.Create("kid", "꼬마 피피", "마을 아이", new Vector3(0, 0, -4), 0, new NPC.Look
            {
                body = new Color(0.95f, 0.75f, 0.25f), skin = new Color(1f, 0.87f, 0.76f), hair = new Color(0.4f, 0.25f, 0.12f),
                accent = new Color(0.3f, 0.6f, 0.9f), hat = "cap", scale = 0.65f
            }, true);
        }

        // ═══════════════════════════ 게임 흐름 ═══════════════════════════
        public void GoToTitle()
        {
            if (endingRoutine != null) { StopCoroutine(endingRoutine); endingRoutine = null; }
            Time.timeScale = 1f;
            State = GameState.Title;
            UI.CloseAllWindows();
            UI.Hud.SetVisible(false);
            foreach (var z in zones) z.ClearAll();
            bossZone.ClearAll();
            foreach (var z in ruinZones) { z.ClearAll(); z.active = false; }
            lichZone.ClearAll();
            ClearSummons();
            WorldBuilder.SetSealOpen(false);
            Player.Teleport(new Vector3(0, 0, 4));
            Cam.Snap();
            UI.Menus.ShowTitle();
        }

        public void NewGame()
        {
            Stats.Reset();
            Inv.Reset();
            Quests.ResetAll();
            Gamble.Reset();
            HighRoller = false;
            BossDefeated = false;
            TrueBossDefeated = false;
            PlayTime = 0;
            StartPlaying();
            Save();
            UI.Dialogue.ShowSequence("촌장 에드릭", new[]
            {
                "오, 자네가 소문으로 듣던 그 떠돌이 모험가로군! 새벽빛 마을에 온 것을 환영하네.",
                "요즘 북쪽에서 몬스터들이 부쩍 늘어 마을 사람들이 두려움에 떨고 있다네. 모두 고블린 왕 '그락'이 나타난 뒤부터지.",
                "부디 우리 마을을 도와주게. 내 집 앞으로 오면 자세히 이야기해 주겠네. <color=#FFD24D>(머리 위 ! 표시가 있는 NPC에게 E키로 말을 거세요)</color>",
                "아, 광장 동쪽 천막의 '럭키' 잭은 조심하게. 재미로만 즐기는 게 좋을 걸세. 허허."
            }, () =>
            {
                UI.Dialogue.Close();
                UI.Hud.Banner(WorldBuilder.ZoneName[0], "WASD 이동 · Space 공격 · E 대화");
            });
        }

        public void ContinueGame()
        {
            if (!Load()) { NewGame(); return; }
            StartPlaying();
            UI.Hud.Banner("모험을 이어갑니다", "Lv." + Stats.level + " · " + Stats.gold.ToString("N0") + " G");
        }

        void StartPlaying()
        {
            UI.Menus.HideAll();
            UI.CloseAllWindows();
            Stats.hp = Mathf.Max(1, Stats.hp);
            Stats.Notify();

            foreach (var z in zones) z.FillAll();
            if (!BossDefeated) bossZone.FillAll(); else bossZone.ClearAll();
            ClearSummons();
            RefreshRuins();
            foreach (var h in Herb.All) h.ResetHerb();

            Player.Teleport(WorldBuilder.PlayerSpawn);
            Player.RefreshLook();
            Cam.Snap();
            lastZone = 0;
            autoSaveTimer = AutoSaveInterval;
            State = GameState.Playing;
            UI.Hud.SetVisible(true);
        }

        /// <summary>봉인 상태에 따라 폐허 지역을 열고 몬스터를 배치</summary>
        void RefreshRuins()
        {
            WorldBuilder.SetSealOpen(BossDefeated);
            foreach (var z in ruinZones)
            {
                z.active = BossDefeated;
                if (BossDefeated) z.FillAll(); else z.ClearAll();
            }
            if (BossDefeated && !TrueBossDefeated) lichZone.FillAll(); else lichZone.ClearAll();
        }

        /// <summary>보스가 소환한(구역에 속하지 않은) 몬스터 정리</summary>
        void ClearSummons()
        {
            foreach (var e in Enemy.Active.ToArray())
                if (e != null && e.Zone == null) Destroy(e.gameObject);
        }

        public void ContinueAfterEnding()
        {
            UI.Menus.HideAll();
            State = GameState.Playing;
            UI.Hud.SetVisible(true);
            UI.Hud.Toast("촌장 에드릭에게 돌아가 승리를 보고하세요!");
        }

        // ═══════════════════════════ 업데이트 ═══════════════════════════
        void Update()
        {
            if (UI == null || Player == null) return;
            if (State == GameState.Playing) PlayTime += Time.deltaTime;

            if (GameInput.Pause) HandleEsc();

            if (State != GameState.Playing || UI.Menus.PauseOpen) return;

            if (GameInput.Inventory && !UI.Dialogue.IsOpen && !UI.Shop.IsOpen && !UI.Gamble.IsOpen) UI.Inventory.Toggle();
            if (GameInput.QuestLog && !UI.Dialogue.IsOpen && !UI.Shop.IsOpen && !UI.Gamble.IsOpen) UI.QuestLog.Toggle();

            // 지역 이동 배너
            int zone = WorldBuilder.ZoneAt(Player.transform.position);
            if (zone != lastZone)
            {
                if (lastZone >= 0)
                {
                    UI.Hud.Banner(WorldBuilder.ZoneName[zone], WorldBuilder.ZoneSub[zone]);
                    if (zone == 4 && !BossDefeated) SoundFX.Play(Sfx.Slam, 0.5f);
                }
                lastZone = zone;
            }

            // 보스를 퀘스트 수락 전에 먼저 잡은 경우에도 메인 퀘스트를 완료할 수 있도록 보정
            if (BossDefeated)
            {
                var m4 = Quests.Get("m4");
                if (m4 != null && m4.state == QuestState.Active)
                {
                    m4.progress = m4.required;
                    m4.state = QuestState.ReadyToTurnIn;
                    Quests.UpdateUnlocks();
                    UI.Hud.Toast(UIFactory.Col("그락은 이미 쓰러졌습니다! 촌장에게 보고하세요", UIFactory.Gold));
                }
            }

            if (TrueBossDefeated)
            {
                var m6 = Quests.Get("m6");
                if (m6 != null && m6.state == QuestState.Active)
                {
                    m6.progress = m6.required;
                    m6.state = QuestState.ReadyToTurnIn;
                    Quests.UpdateUnlocks();
                }
            }

            // 자동 저장
            autoSaveTimer -= Time.deltaTime;
            if (autoSaveTimer <= 0f) { autoSaveTimer = AutoSaveInterval; Save(); }
        }

        void HandleEsc()
        {
            if (UI.Menus.HandleEsc()) return;
            if (State != GameState.Playing) return;
            if (UI.CloseTop()) return;
            UI.Menus.TogglePause();
        }

        // ═══════════════════════════ 아이템 ═══════════════════════════
        public bool UseItem(string id)
        {
            var d = ItemDB.Get(id);
            if (d == null || Inv.Count(id) <= 0) return false;

            if (d.type == ItemType.Weapon || d.type == ItemType.Armor)
            {
                Inv.Equip(id);
                Player.RefreshLook();
                return true;
            }
            if (d.type != ItemType.Consumable)
            {
                UI.Hud.Toast("사용할 수 없는 아이템입니다");
                return false;
            }

            if (d.effect == "return")
            {
                if (WorldBuilder.ZoneAt(Player.transform.position) == 0) { UI.Hud.Toast("이미 마을 안에 있습니다"); return false; }
                Inv.Remove(id);
                StartCoroutine(ReturnRoutine());
                return true;
            }

            bool needHp = d.hp > 0 && Stats.hp < Stats.MaxHP;
            bool needMp = d.mp > 0 && Stats.mp < Stats.MaxMP;
            if (!needHp && !needMp) { UI.Hud.Toast(d.hp > 0 ? "HP가 이미 가득 찼습니다" : "MP가 이미 가득 찼습니다"); return false; }

            Inv.Remove(id);
            var pos = Player.transform.position + Vector3.up * 2.2f;
            if (d.hp > 0) { Stats.Heal(d.hp); Fx.FloatText(pos, "+" + d.hp, new Color(0.4f, 1f, 0.5f), 28); }
            if (d.mp > 0) { Stats.RestoreMP(d.mp); Fx.FloatText(pos + Vector3.up * 0.4f, "+" + d.mp + " MP", new Color(0.5f, 0.7f, 1f), 26); }
            Fx.Burst(Player.transform.position + Vector3.up, d.color, 10, 3f, 0.15f);
            SoundFX.Play(Sfx.Pickup);
            return true;
        }

        IEnumerator ReturnRoutine()
        {
            SoundFX.Play(Sfx.Skill);
            Fx.Ring(Player.transform.position, 2.5f, new Color(1f, 0.9f, 0.5f), 0.5f);
            UI.FadeFlash();
            yield return new WaitForSecondsRealtime(0.5f);
            Player.Teleport(WorldBuilder.PlayerSpawn);
            Cam.Snap();
            UI.Hud.Toast("새벽빛 마을로 귀환했습니다");
        }

        // ═══════════════════════════ 전투 보상 ═══════════════════════════
        public void OnEnemyKilled(Enemy e)
        {
            var d = e.Def;
            var pos = e.transform.position + Vector3.up * 1.5f;
            Stats.kills++;

            int gold = UnityEngine.Random.Range(d.goldMin, d.goldMax + 1);
            Stats.AddGold(gold);
            Fx.FloatText(pos + Vector3.up * 0.6f, "+" + gold + " G", UIFactory.Gold, 22);
            Fx.FloatText(pos, "+" + d.exp + " EXP", new Color(0.7f, 0.85f, 1f), 20);
            SoundFX.Play(Sfx.Coin, 0.5f);

            if (!string.IsNullOrEmpty(d.drop) && UnityEngine.Random.value < d.dropChance)
            {
                Inv.Add(d.drop);
                var item = ItemDB.Get(d.drop);
                UI.Hud.Toast("획득: " + UIFactory.Col(item.name, item.color));
            }
            // 도박사 잭의 금화 (서브 퀘스트 진행 중일 때만)
            if (d.id == "goblin" && Quests.IsActive("s_coin") && !Inv.Owns("lucky_coin") && UnityEngine.Random.value < 0.35f)
            {
                Inv.Add("lucky_coin");
                SoundFX.Play(Sfx.Win, 0.7f);
                UI.Hud.Toast(UIFactory.Col("반짝이는 금화를 발견했다! (행운의 금화)", UIFactory.Gold));
            }

            Quests.OnKill(d.id);
            Stats.AddExp(d.exp);

            if (d.id == "goblin_king")
            {
                BossDefeated = true;
                Cam.Shake(0.5f, 0.8f);
                SoundFX.Play(Sfx.BigWin);
                UI.Hud.Banner("고블린 왕 그락 처치!", "새벽빛 마을에 평화가 찾아옵니다");
                RefreshRuins();
                UI.Hud.Banner("봉인이 풀렸다...", "제단 북쪽, 잊혀진 폐허로 가는 길이 열렸습니다");
                Save();
                endingRoutine = StartCoroutine(EndingRoutine(false));
            }
            else if (d.id == "lich")
            {
                TrueBossDefeated = true;
                Cam.Shake(0.6f, 1f);
                SoundFX.Play(Sfx.BigWin);
                UI.Hud.Banner("망령 군주 모르가스 처치!", "폐허를 뒤덮은 저주가 걷힙니다");
                foreach (var s in Enemy.Active.ToArray())
                    if (s != null && s.Zone == null && !s.IsDead)
                    {
                        // 소환된 해골도 함께 소멸 (보상 없음)
                        Fx.Burst(s.transform.position + Vector3.up, new Color(0.7f, 0.5f, 1f), 12, 5f, 0.2f);
                        Destroy(s.gameObject);
                    }
                Save();
                endingRoutine = StartCoroutine(EndingRoutine(true));
            }
        }

        IEnumerator EndingRoutine(bool trueEnding)
        {
            yield return new WaitForSecondsRealtime(4f);
            endingRoutine = null;
            if (State != GameState.Playing) yield break;
            UI.CloseAllWindows();
            State = GameState.Ending;
            UI.Hud.SetVisible(false);
            UI.Menus.ShowEnding(trueEnding);
        }

        public void OnQuestCompleted(Quest q)
        {
            if (q.id == "s_coin")
            {
                HighRoller = true;
                UI.Hud.Banner("하이롤러 테이블 개방", "도박 최대 베팅 5000G");
            }
            Save();
        }

        void OnLevelUp(int lv)
        {
            SoundFX.Play(Sfx.LevelUp);
            if (UI != null) UI.Hud.Banner("레벨 업!", "Lv." + lv + " 달성 · HP/MP 완전 회복 · 모든 능력치 상승");
            if (Player != null)
            {
                Fx.Ring(Player.transform.position, 3f, UIFactory.Gold, 0.6f);
                Fx.Burst(Player.transform.position + Vector3.up, UIFactory.Gold, 20, 5f, 0.18f);
            }
        }

        // ═══════════════════════════ 사망 / 부활 ═══════════════════════════
        int pendingGoldLoss;

        public void OnPlayerDeath()
        {
            if (State != GameState.Playing) return;
            State = GameState.Dead;
            UI.CloseAllWindows();
            SoundFX.Play(Sfx.Lose);
            Cam.Shake(0.4f, 0.4f);
            pendingGoldLoss = Stats.gold / 10;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            UI.Menus.ShowDeath(pendingGoldLoss);
        }

        public void Revive()
        {
            Stats.AddGold(-pendingGoldLoss);
            Stats.hp = Stats.MaxHP;
            Stats.mp = Stats.MaxMP;
            Stats.Notify();
            UI.Menus.HideAll();
            UI.FadeFlash();
            Player.Teleport(WorldBuilder.PlayerSpawn);
            Cam.Snap();
            lastZone = 0;
            State = GameState.Playing;
            if (pendingGoldLoss > 0) UI.Hud.Toast("치료비로 " + UIFactory.Col(pendingGoldLoss + " G", UIFactory.Gold) + "를 지불했습니다");
            pendingGoldLoss = 0;
            Save();
        }

        // ═══════════════════════════ 저장 / 불러오기 ═══════════════════════════
        public static bool HasSave { get { return PlayerPrefs.HasKey(SaveKey); } }

        public static string SaveSummary()
        {
            try
            {
                var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                int m = Mathf.FloorToInt(d.playTime / 60f);
                return "Lv." + d.level + " · " + d.gold.ToString("N0") + " G · 플레이 " + m + "분" + (d.trueBossDefeated ? " · 진 엔딩 달성" : d.bossDefeated ? " · 그락 처치" : "");
            }
            catch { return "손상됨"; }
        }

        public void Save()
        {
            if (State == GameState.Title) return;
            var d = new SaveData
            {
                level = Stats.level, exp = Stats.exp, hp = Mathf.Max(1, Stats.hp), mp = Stats.mp, gold = Stats.gold,
                baseMaxHp = Stats.baseMaxHp, baseMaxMp = Stats.baseMaxMp, baseAtk = Stats.baseAtk, baseDef = Stats.baseDef, kills = Stats.kills,
                weaponId = Inv.weaponId, armorId = Inv.armorId,
                quests = Quests.Save(), gamble = Gamble, highRoller = HighRoller, bossDefeated = BossDefeated, trueBossDefeated = TrueBossDefeated, playTime = PlayTime
            };
            foreach (var s in Inv.items) d.items.Add(new ItemStack(s.id, s.count));
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        bool Load()
        {
            if (!HasSave) return false;
            SaveData d;
            try { d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)); }
            catch (Exception ex) { Debug.LogWarning("[MiniRPG] 저장 데이터를 읽을 수 없습니다: " + ex.Message); return false; }
            if (d == null || d.level <= 0) return false;

            Inv.items.Clear();
            if (d.items != null) foreach (var s in d.items) if (ItemDB.Get(s.id) != null && s.count > 0) Inv.items.Add(new ItemStack(s.id, s.count));
            Inv.weaponId = ItemDB.Get(d.weaponId) != null ? d.weaponId : "wood_sword";
            Inv.armorId = ItemDB.Get(d.armorId) != null ? d.armorId : "cloth";

            Stats.level = d.level; Stats.exp = d.exp; Stats.gold = d.gold; Stats.kills = d.kills;
            Stats.baseMaxHp = d.baseMaxHp; Stats.baseMaxMp = d.baseMaxMp; Stats.baseAtk = d.baseAtk; Stats.baseDef = d.baseDef;
            Stats.hp = Mathf.Clamp(d.hp, 1, Stats.MaxHP); Stats.mp = Mathf.Clamp(d.mp, 0, Stats.MaxMP);

            Gamble = d.gamble ?? new GambleStats();
            HighRoller = d.highRoller;
            BossDefeated = d.bossDefeated;
            TrueBossDefeated = d.trueBossDefeated;
            PlayTime = d.playTime;
            Quests.Load(d.quests);
            Inv.Notify();
            Stats.Notify();
            return true;
        }

        void OnApplicationQuit()
        {
            if (State == GameState.Playing) Save();
        }
    }
}
