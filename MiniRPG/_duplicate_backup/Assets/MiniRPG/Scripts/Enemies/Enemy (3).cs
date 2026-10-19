using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> Active = new List<Enemy>();

        public EnemyDef Def { get; private set; }
        public int HP { get; private set; }
        public bool IsDead { get; private set; }
        public float Radius { get; private set; }
        public SpawnZone Zone;
        public bool Aggro { get { return state == AIState.Chase || state == AIState.Attack; } }

        enum AIState { Idle, Wander, Chase, Attack, Return }
        AIState state = AIState.Idle;

        CharacterController cc;
        Transform model;
        Renderer[] renderers;
        Material[] baseMats;
        Vector3 home, wanderTarget, knock;
        float stateTimer, attackTimer, yVel, slamTimer, hopPhase;
        bool enraged, busy, specialToggle, summoned70, summoned35;
        float bobPhase;

        // 화면 공간 체력바
        RectTransform barRoot;
        UIBar bar;
        Text nameText;

        public static Enemy Spawn(string id, Vector3 pos, SpawnZone zone)
        {
            var def = EnemyDB.Get(id);
            var go = new GameObject(def.name);
            go.transform.position = pos;
            var e = go.AddComponent<Enemy>();
            e.Def = def; e.Zone = zone; e.home = pos;
            e.Build();
            return e;
        }

        void Build()
        {
            HP = Def.hp;
            cc = gameObject.AddComponent<CharacterController>();
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            model.localEulerAngles = new Vector3(0, Random.Range(0, 360f), 0);

            switch (Def.id)
            {
                case "slime": BuildSlime(); break;
                case "wolf": BuildWolf(); break;
                case "goblin": BuildGoblin(1f, false); break;
                case "skeleton": BuildSkeleton(); break;
                case "wraith": BuildWraith(); break;
                case "lich": BuildLich(); break;
                default: BuildGoblin(2.1f, true); break;
            }
            cc.radius = Radius; cc.height = Mathf.Max(Radius * 2f, cc.height); cc.center = new Vector3(0, cc.height * 0.5f, 0);
            renderers = model.GetComponentsInChildren<Renderer>();
            baseMats = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseMats[i] = renderers[i].sharedMaterial;

            bobPhase = Random.value * 10f;
            WorldBuilder.MapIcon(transform, Def.isBoss ? new Color(0.8f, 0.2f, 1f) : new Color(1f, 0.25f, 0.25f), Def.isBoss ? 4.5f : 1.8f);
            if (!Def.isBoss) CreateBar();
            Active.Add(this);
            StartCoroutine(SpawnPop());
        }

        void BuildSlime()
        {
            Radius = 0.55f; cc.height = 1f;
            var c = new Color(0.35f, 0.85f, 0.45f);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 0.45f, 0), new Vector3(1.1f, 0.85f, 1.1f), c);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.1f, 0.75f, 0.1f), new Vector3(0.35f, 0.2f, 0.35f), new Color(0.75f, 1f, 0.8f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.18f, 0.55f, 0.45f), Vector3.one * 0.14f, Color.black);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.18f, 0.55f, 0.45f), Vector3.one * 0.14f, Color.black);
        }

        void BuildWolf()
        {
            Radius = 0.6f; cc.height = 1.2f;
            var c = new Color(0.5f, 0.5f, 0.55f);
            var d = new Color(0.35f, 0.35f, 0.4f);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.75f, 0), new Vector3(0.7f, 0.6f, 1.5f), c);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 1.0f, 0.85f), new Vector3(0.55f, 0.5f, 0.55f), c);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.9f, 1.2f), new Vector3(0.3f, 0.25f, 0.4f), d);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(-0.17f, 1.35f, 0.8f), new Vector3(0.12f, 0.25f, 0.1f), d);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0.17f, 1.35f, 0.8f), new Vector3(0.12f, 0.25f, 0.1f), d);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.14f, 1.1f, 1.1f), Vector3.one * 0.09f, new Color(1f, 0.85f, 0.2f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.14f, 1.1f, 1.1f), Vector3.one * 0.09f, new Color(1f, 0.85f, 0.2f));
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.95f, -0.95f), new Vector3(0.15f, 0.15f, 0.6f), d, false, new Vector3(-30, 0, 0));
            foreach (var x in new[] { -0.25f, 0.25f })
                foreach (var z in new[] { -0.55f, 0.55f })
                    WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(x, 0.25f, z), new Vector3(0.16f, 0.5f, 0.16f), d);
        }

        void BuildGoblin(float s, bool king)
        {
            Radius = 0.45f * s; cc.height = 1.7f * s;
            var skin = king ? new Color(0.4f, 0.55f, 0.25f) : new Color(0.45f, 0.65f, 0.3f);
            var cloth = king ? new Color(0.55f, 0.12f, 0.15f) : new Color(0.45f, 0.3f, 0.18f);
            WorldBuilder.Prim(PrimitiveType.Capsule, model, new Vector3(0, 0.75f, 0) * s, new Vector3(0.75f, 0.55f, 0.6f) * s, cloth);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.45f, 0.05f) * s, Vector3.one * 0.6f * s, skin);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(-0.38f, 1.5f, 0) * s, new Vector3(0.35f, 0.1f, 0.12f) * s, skin, false, new Vector3(0, 0, 25));
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0.38f, 1.5f, 0) * s, new Vector3(0.35f, 0.1f, 0.12f) * s, skin, false, new Vector3(0, 0, -25));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.12f, 1.5f, 0.3f) * s, Vector3.one * 0.1f * s, new Color(1f, 0.2f, 0.1f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.12f, 1.5f, 0.3f) * s, Vector3.one * 0.1f * s, new Color(1f, 0.2f, 0.1f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.38f, 0.33f) * s, Vector3.one * 0.12f * s, skin * 0.85f);
            // 몽둥이
            var club = new GameObject("Club").transform;
            club.SetParent(model, false);
            club.localPosition = new Vector3(0.45f, 0.9f, 0.1f) * s;
            club.localEulerAngles = new Vector3(60, 0, 0);
            WorldBuilder.Prim(PrimitiveType.Cylinder, club, new Vector3(0, 0.35f, 0) * s, new Vector3(0.1f, 0.4f, 0.1f) * s, new Color(0.4f, 0.26f, 0.15f));
            WorldBuilder.Prim(PrimitiveType.Sphere, club, new Vector3(0, 0.8f, 0) * s, new Vector3(0.28f, 0.35f, 0.28f) * s, new Color(0.35f, 0.22f, 0.12f));
            if (king)
            {
                // 왕관
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 5f * Mathf.PI * 2f;
                    WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(Mathf.Cos(a) * 0.22f, 1.82f, Mathf.Sin(a) * 0.22f + 0.05f) * s,
                                      new Vector3(0.08f, 0.2f, 0.08f) * s, UIFactory.Gold);
                }
                WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 1.72f, 0.05f) * s, new Vector3(0.5f, 0.04f, 0.5f) * s, UIFactory.Gold);
                WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.95f, -0.32f) * s, new Vector3(0.8f, 1.1f, 0.06f) * s, new Color(0.35f, 0.08f, 0.4f), false, new Vector3(8, 0, 0));
            }
        }

        void BuildSkeleton()
        {
            Radius = 0.45f; cc.height = 1.8f;
            var bone = new Color(0.9f, 0.88f, 0.8f);
            var dark = new Color(0.25f, 0.22f, 0.2f);
            // 갈비뼈 몸통
            WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.9f, 0), new Vector3(0.12f, 0.45f, 0.12f), bone);
            for (int i = 0; i < 3; i++)
                WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.95f + i * 0.17f, 0.02f), new Vector3(0.62f - i * 0.06f, 0.07f, 0.35f), bone);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.62f, 0), new Vector3(0.5f, 0.14f, 0.28f), bone);
            // 다리
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(-0.15f, 0.3f, 0), new Vector3(0.1f, 0.6f, 0.1f), bone);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0.15f, 0.3f, 0), new Vector3(0.1f, 0.6f, 0.1f), bone);
            // 두개골
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.62f, 0.02f), new Vector3(0.48f, 0.5f, 0.48f), bone);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 1.43f, 0.12f), new Vector3(0.3f, 0.14f, 0.26f), bone);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.1f, 1.64f, 0.2f), Vector3.one * 0.13f, dark);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.1f, 1.64f, 0.2f), Vector3.one * 0.13f, dark);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.1f, 1.64f, 0.24f), Vector3.one * 0.06f, new Color(0.4f, 0.9f, 1f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.1f, 1.64f, 0.24f), Vector3.one * 0.06f, new Color(0.4f, 0.9f, 1f));
            // 녹슨 검과 방패
            var sword = new GameObject("Sword").transform;
            sword.SetParent(model, false);
            sword.localPosition = new Vector3(0.42f, 0.95f, 0.15f);
            sword.localEulerAngles = new Vector3(70, 0, 0);
            WorldBuilder.Prim(PrimitiveType.Cube, sword, new Vector3(0, 0.5f, 0), new Vector3(0.08f, 0.9f, 0.04f), new Color(0.55f, 0.45f, 0.38f));
            WorldBuilder.Prim(PrimitiveType.Cube, sword, new Vector3(0, 0.05f, 0), new Vector3(0.28f, 0.06f, 0.06f), dark);
            WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(-0.42f, 1.0f, 0.12f), new Vector3(0.6f, 0.03f, 0.6f), new Color(0.35f, 0.3f, 0.28f), false, new Vector3(0, 0, 90));
        }

        void BuildWraith()
        {
            Radius = 0.5f; cc.height = 1.8f;
            var robe = new Color(0.45f, 0.75f, 0.78f);
            var deep = new Color(0.12f, 0.2f, 0.25f);
            // 흩날리는 로브 (위로 갈수록 좁아지는 층)
            WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.55f, 0), new Vector3(0.8f, 0.4f, 0.8f), robe * 0.8f);
            WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 1.0f, 0), new Vector3(0.62f, 0.35f, 0.62f), robe);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.55f, 0), new Vector3(0.62f, 0.7f, 0.62f), robe);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.52f, 0.16f), new Vector3(0.42f, 0.46f, 0.36f), deep);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.09f, 1.56f, 0.33f), Vector3.one * 0.09f, new Color(0.7f, 1f, 1f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.09f, 1.56f, 0.33f), Vector3.one * 0.09f, new Color(0.7f, 1f, 1f));
            // 긴 팔
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(-0.42f, 1.05f, 0.25f), new Vector3(0.14f, 0.14f, 0.7f), robe, false, new Vector3(15, -20, 0));
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0.42f, 1.05f, 0.25f), new Vector3(0.14f, 0.14f, 0.7f), robe, false, new Vector3(15, 20, 0));
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(model, false);
            l.transform.localPosition = new Vector3(0, 1.4f, 0.4f);
            l.type = LightType.Point; l.range = 4f; l.intensity = 1.3f; l.color = new Color(0.5f, 1f, 1f);
            l.shadows = LightShadows.None;
        }

        void BuildLich()
        {
            const float s = 1.9f;
            Radius = 0.55f * s; cc.height = 1.9f * s;
            var robe = new Color(0.22f, 0.1f, 0.3f);
            var trim = new Color(0.65f, 0.45f, 0.15f);
            var bone = new Color(0.85f, 0.83f, 0.78f);
            WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.5f, 0) * s, new Vector3(1.0f, 0.5f, 1.0f) * s, robe);
            WorldBuilder.Prim(PrimitiveType.Capsule, model, new Vector3(0, 1.1f, 0) * s, new Vector3(0.75f, 0.55f, 0.6f) * s, robe);
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 1.35f, 0.26f) * s, new Vector3(0.14f, 0.9f, 0.06f) * s, trim);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.72f, 0.05f) * s, Vector3.one * 0.46f * s, bone);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.8f, -0.05f) * s, new Vector3(0.62f, 0.6f, 0.62f) * s, robe * 0.8f); // 두건
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.09f, 1.73f, 0.25f) * s, Vector3.one * 0.09f * s, new Color(0.8f, 0.3f, 1f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.09f, 1.73f, 0.25f) * s, Vector3.one * 0.09f * s, new Color(0.8f, 0.3f, 1f));
            // 뼈 왕관
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(Mathf.Cos(a) * 0.24f, 2.1f, Mathf.Sin(a) * 0.24f) * s, new Vector3(0.06f, 0.26f, 0.06f) * s, bone);
            }
            // 어깨 장식
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.42f, 1.45f, 0) * s, Vector3.one * 0.32f * s, trim);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.42f, 1.45f, 0) * s, Vector3.one * 0.32f * s, trim);
            // 지팡이
            var staff = new GameObject("Staff").transform;
            staff.SetParent(model, false);
            staff.localPosition = new Vector3(0.62f, 0.2f, 0.25f) * s;
            WorldBuilder.Prim(PrimitiveType.Cylinder, staff, new Vector3(0, 1.1f, 0) * s, new Vector3(0.08f, 1.1f, 0.08f) * s, new Color(0.3f, 0.2f, 0.15f));
            WorldBuilder.Prim(PrimitiveType.Sphere, staff, new Vector3(0, 2.3f, 0) * s, Vector3.one * 0.34f * s, new Color(0.75f, 0.35f, 1f));
            var l = new GameObject("StaffGlow").AddComponent<Light>();
            l.transform.SetParent(staff, false);
            l.transform.localPosition = new Vector3(0, 2.3f, 0) * s;
            l.type = LightType.Point; l.range = 9f; l.intensity = 2f; l.color = new Color(0.7f, 0.35f, 1f);
            l.gameObject.AddComponent<Flicker>();
        }

        void CreateBar()
        {
            var layer = GameManager.I.Fx.worldLayer;
            barRoot = UIFactory.Rect("EnemyBar", layer);
            barRoot.sizeDelta = new Vector2(90, 26);
            nameText = UIFactory.Label(barRoot, Def.name, 14, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.85f));
            UIFactory.Place(nameText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(160, 16));
            bar = new UIBar(barRoot, new Color(0.9f, 0.25f, 0.25f), new Vector2(80, 9), false);
            UIFactory.Place(bar.root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(80, 9));
            barRoot.gameObject.SetActive(false);
        }

        IEnumerator SpawnPop()
        {
            float t = 0; busy = true;
            while (t < 0.35f) { t += Time.deltaTime; model.localScale = Vector3.one * Mathf.SmoothStep(0.1f, 1f, t / 0.35f); yield return null; }
            model.localScale = Vector3.one; busy = false;
        }

        void Update()
        {
            if (IsDead) return;
            var gm = GameManager.I;
            if (gm == null || gm.State == GameState.Title) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;

            yVel = cc.isGrounded ? -2f : yVel - 25f * dt;
            knock = Vector3.MoveTowards(knock, Vector3.zero, 25f * dt);
            Vector3 move = Vector3.zero;

            var player = gm.Player;
            bool playerOk = player != null && !gm.Stats.Dead && gm.State == GameState.Playing;
            Vector3 toP = playerOk ? player.transform.position - transform.position : Vector3.zero; toP.y = 0;
            float distP = playerOk ? toP.magnitude : 999f;
            bool playerInVillage = playerOk && WorldBuilder.ZoneAt(player.transform.position) == 0;
            float leash = Def.isBoss ? 22f : 16f;
            float fromHome = Vector3.Distance(Flat(transform.position), Flat(home));

            if (!busy)
            {
                switch (state)
                {
                    case AIState.Idle:
                        stateTimer -= dt;
                        if (stateTimer <= 0) { state = AIState.Wander; wanderTarget = home + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f)); stateTimer = 4f; }
                        if (distP < Def.aggroRange && !playerInVillage) state = AIState.Chase;
                        break;
                    case AIState.Wander:
                        stateTimer -= dt;
                        var tw = wanderTarget - transform.position; tw.y = 0;
                        if (tw.magnitude < 0.5f || stateTimer <= 0) { state = AIState.Idle; stateTimer = Random.Range(1.5f, 4f); }
                        else move = tw.normalized * Def.speed * 0.4f;
                        if (distP < Def.aggroRange && !playerInVillage) state = AIState.Chase;
                        break;
                    case AIState.Chase:
                        if (!playerOk || playerInVillage || fromHome > leash) { state = AIState.Return; break; }
                        if (distP <= Def.attackRange + (Def.ranged ? -0.5f : 0.3f)) { state = AIState.Attack; attackTimer = Def.ranged ? 0.6f : 0.25f; break; }
                        move = toP.normalized * Def.speed * (enraged ? 1.35f : 1f);
                        break;
                    case AIState.Attack:
                        if (!playerOk) { state = AIState.Return; break; }
                        attackTimer -= dt;
                        if (distP > Def.attackRange + (Def.ranged ? 2f : 0.9f)) { state = AIState.Chase; break; }
                        // 원거리형: 너무 가까우면 뒤로 물러남
                        if (Def.ranged && distP < 4f && fromHome < leash) move = -toP.normalized * Def.speed * 0.75f;
                        if (attackTimer <= 0f) { StartCoroutine(AttackRoutine()); attackTimer = Def.attackInterval * (enraged ? 0.7f : 1f); }
                        break;
                    case AIState.Return:
                        var th = home - transform.position; th.y = 0;
                        if (th.magnitude < 1f) { state = AIState.Idle; stateTimer = 1f; }
                        else move = th.normalized * Def.speed * 1.2f;
                        // 복귀 중에는 체력 회복
                        if (HP < Def.hp) { HP = Mathf.Min(Def.hp, HP + Mathf.CeilToInt(Def.hp * 0.3f * dt)); UpdateBar(); }
                        break;
                }

                // 보스 특수 공격
                if (Def.isBoss && Aggro)
                {
                    slamTimer -= dt;
                    if (Def.id == "lich")
                    {
                        if (slamTimer <= 0f && distP < 14f)
                        {
                            slamTimer = enraged ? 3.8f : 5.2f;
                            specialToggle = !specialToggle;
                            StartCoroutine(specialToggle ? NovaRoutine() : DarkCircleRoutine());
                        }
                    }
                    else if (slamTimer <= 0f && distP < 10f) { slamTimer = enraged ? 5f : 7f; StartCoroutine(SlamRoutine()); }
                }
            }

            // 방향 전환
            Vector3 face = (state == AIState.Attack || busy) && playerOk ? toP : move;
            if (face.sqrMagnitude > 0.01f) model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(face), 10f * dt);

            // 이동 애니메이션
            if (Def.id == "slime")
            {
                hopPhase += dt * (move.sqrMagnitude > 0.1f ? 8f : 3f);
                float sq = 1f + Mathf.Sin(hopPhase) * 0.1f;
                if (!busy) model.localScale = new Vector3(1f / Mathf.Sqrt(sq), sq, 1f / Mathf.Sqrt(sq));
            }
            else if (Def.id == "wraith" || Def.id == "lich")
            {
                // 떠다니는 적
                if (!busy) model.localPosition = new Vector3(0, 0.3f + Mathf.Sin(Time.time * 2.2f + bobPhase) * 0.15f, 0);
            }
            else if (move.sqrMagnitude > 0.1f)
            {
                hopPhase += dt * Def.speed * 3f;
                model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(hopPhase)) * 0.12f, 0);
            }

            cc.Move((move + knock + Vector3.up * yVel) * dt);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        IEnumerator AttackRoutine()
        {
            busy = true;
            // 예비 동작 (빨갛게 번쩍)
            SetTint(new Color(1f, 0.5f, 0.4f));
            float t = 0;
            var baseScale = model.localScale;
            while (t < 0.35f) { t += Time.deltaTime; model.localScale = baseScale * (1f + 0.12f * (t / 0.35f)); yield return null; }
            RestoreTint();
            model.localScale = baseScale;
            if (!IsDead && Def.ranged)
            {
                var p0 = GameManager.I.Player;
                if (p0 != null)
                {
                    var dir = p0.transform.position - transform.position;
                    Projectile.Fire(transform.position + Vector3.up * 1.3f + model.forward * 0.6f, dir, 9f, Def.atk, new Color(0.45f, 0.95f, 1f));
                    SoundFX.Play(Sfx.Skill, 0.35f);
                }
                yield return new WaitForSeconds(0.15f);
            }
            else if (!IsDead)
            {
                // 돌진
                var gm = GameManager.I;
                var p = gm.Player;
                model.localPosition += model.forward * 0.3f;
                if (p != null)
                {
                    var d = p.transform.position - transform.position; d.y = 0;
                    if (d.magnitude <= Def.attackRange + 0.7f) p.Hurt(Mathf.RoundToInt(Def.atk * (enraged ? 1.25f : 1f)), transform.position);
                }
                yield return new WaitForSeconds(0.12f);
                model.localPosition = Vector3.zero;
            }
            busy = false;
        }

        IEnumerator SlamRoutine()
        {
            busy = true;
            var gm = GameManager.I;
            gm.UI.Hud.Toast(UIFactory.Col("그락이 몽둥이를 높이 치켜든다! 붉은 원을 피하세요!", UIFactory.Bad));
            float radius = 5.2f;
            var center = transform.position;
            var warn = WorldBuilder.Prim(PrimitiveType.Cylinder, null, center + Vector3.up * 0.06f, new Vector3(0.1f, 0.01f, 0.1f), new Color(0.85f, 0.15f, 0.12f));
            var edge = WorldBuilder.Prim(PrimitiveType.Cylinder, null, center + Vector3.up * 0.05f, new Vector3(radius * 2f, 0.01f, radius * 2f), new Color(0.35f, 0.08f, 0.08f));
            float t = 0, dur = 1.25f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(0.1f, radius * 2f, t / dur);
                warn.transform.localScale = new Vector3(s, 0.01f, s);
                model.localPosition = new Vector3(0, Mathf.Sin(t / dur * Mathf.PI * 0.5f) * 1.2f, 0);
                yield return null;
            }
            model.localPosition = Vector3.zero;
            Destroy(warn); Destroy(edge);
            SoundFX.Play(Sfx.Slam);
            gm.Cam.Shake(0.6f, 0.4f);
            gm.Fx.Ring(center, radius, new Color(0.9f, 0.6f, 0.3f), 0.35f);
            gm.Fx.Burst(center + Vector3.up * 0.3f, new Color(0.5f, 0.4f, 0.3f), 18, 9f, 0.35f);
            if (gm.Player != null && Vector3.Distance(Flat(gm.Player.transform.position), Flat(center)) < radius)
                gm.Player.Hurt(Mathf.RoundToInt(Def.atk * 1.6f), center);
            yield return new WaitForSeconds(0.3f);
            busy = false;
        }

        // ─────────────── 망령 군주 패턴 ───────────────
        IEnumerator NovaRoutine()
        {
            busy = true;
            var gm = GameManager.I;
            SetTint(new Color(0.75f, 0.35f, 1f));
            var baseScale = model.localScale;
            float t = 0;
            while (t < 0.7f) { t += Time.deltaTime; model.localScale = baseScale * (1f + 0.1f * (t / 0.7f)); yield return null; }
            model.localScale = baseScale;
            RestoreTint();
            int waves = enraged ? 2 : 1;
            for (int w = 0; w < waves && !IsDead; w++)
            {
                const int count = 12;
                float offset = Random.Range(0f, 360f) + w * 15f;
                for (int i = 0; i < count; i++)
                {
                    float a = offset + i * 360f / count;
                    var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                    Projectile.Fire(transform.position + Vector3.up * 1.6f + dir * 1.2f, dir, 7.5f, Mathf.RoundToInt(Def.atk * 0.8f),
                                    new Color(0.7f, 0.3f, 1f), 3.5f, i % 4 == 0);
                }
                SoundFX.Play(Sfx.Skill, 0.8f);
                gm.Fx.Ring(transform.position, 2.5f, new Color(0.7f, 0.3f, 1f), 0.3f);
                yield return new WaitForSeconds(0.45f);
            }
            busy = false;
        }

        IEnumerator DarkCircleRoutine()
        {
            busy = true;
            var gm = GameManager.I;
            var p = gm.Player;
            SetTint(new Color(0.5f, 0.2f, 0.8f));
            yield return new WaitForSeconds(0.45f);
            RestoreTint();
            if (p != null && !IsDead)
            {
                var c = p.transform.position; c.y = 0;
                StartCoroutine(DelayedAoe(c, 3.2f, 1.3f));
                if (enraged)
                {
                    float a = Random.Range(0f, 360f);
                    StartCoroutine(DelayedAoe(c + Quaternion.Euler(0, a, 0) * Vector3.forward * 5f, 3.2f, 1.5f));
                    StartCoroutine(DelayedAoe(c + Quaternion.Euler(0, a + 180f, 0) * Vector3.forward * 5f, 3.2f, 1.5f));
                }
                SoundFX.Play(Sfx.Skill, 0.5f);
            }
            yield return new WaitForSeconds(0.2f);
            busy = false;
        }

        IEnumerator DelayedAoe(Vector3 center, float radius, float dur)
        {
            var gm = GameManager.I;
            var edge = WorldBuilder.Prim(PrimitiveType.Cylinder, null, center + Vector3.up * 0.05f, new Vector3(radius * 2f, 0.01f, radius * 2f), new Color(0.2f, 0.08f, 0.3f));
            var fill = WorldBuilder.Prim(PrimitiveType.Cylinder, null, center + Vector3.up * 0.06f, new Vector3(0.1f, 0.01f, 0.1f), new Color(0.6f, 0.2f, 0.9f));
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float sc = Mathf.Lerp(0.1f, radius * 2f, t / dur);
                fill.transform.localScale = new Vector3(sc, 0.01f, sc);
                yield return null;
            }
            Destroy(edge); Destroy(fill);
            if (IsDead) yield break;
            SoundFX.Play(Sfx.Slam, 0.6f);
            gm.Cam.Shake(0.3f, 0.25f);
            gm.Fx.Ring(center, radius, new Color(0.7f, 0.3f, 1f), 0.35f);
            gm.Fx.Burst(center + Vector3.up * 0.3f, new Color(0.45f, 0.2f, 0.7f), 14, 7f, 0.3f);
            if (gm.Player != null && Vector3.Distance(Flat(gm.Player.transform.position), Flat(center)) < radius)
                gm.Player.Hurt(Mathf.RoundToInt(Def.atk * 1.4f), center);
        }

        IEnumerator SummonRoutine(int count)
        {
            var gm = GameManager.I;
            gm.UI.Hud.Toast(UIFactory.Col("모르가스가 망자들을 불러낸다!", new Color(0.8f, 0.5f, 1f)));
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, 360f);
                var pos = transform.position + Quaternion.Euler(0, a, 0) * Vector3.forward * Random.Range(4f, 6f);
                pos.y = 0.1f;
                gm.Fx.Ring(pos, 1.5f, new Color(0.7f, 0.3f, 1f), 0.4f);
                gm.Fx.Burst(pos + Vector3.up * 0.3f, new Color(0.5f, 0.3f, 0.7f), 10, 4f, 0.2f);
                yield return new WaitForSeconds(0.25f);
                if (IsDead) yield break;
                Enemy.Spawn("skeleton", pos, null);
            }
        }

        void SetTint(Color c)
        {
            var m = Mats.Get(c);
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].sharedMaterial = m;
        }

        void RestoreTint()
        {
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].sharedMaterial = baseMats[i];
        }

        public void TakeDamage(int dmg, bool crit, Vector3 from)
        {
            if (IsDead) return;
            HP -= dmg;
            var gm = GameManager.I;
            gm.Fx.FloatText(transform.position + Vector3.up * (cc.height + 0.6f), crit ? dmg + "!" : dmg.ToString(),
                            crit ? new Color(1f, 0.8f, 0.2f) : Color.white, crit ? 34 : 26);
            gm.Fx.Burst(transform.position + Vector3.up * cc.height * 0.5f, crit ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 1f, 0.85f), crit ? 10 : 5, 5f, 0.12f);
            SoundFX.Play(crit ? Sfx.Crit : Sfx.Hit, 0.8f);
            if (!Def.isBoss)
            {
                var d = transform.position - from; d.y = 0;
                knock = d.normalized * (crit ? 9f : 6f);
            }
            if (state == AIState.Idle || state == AIState.Wander || state == AIState.Return) state = AIState.Chase;
            if (Def.id == "lich" && HP > 0)
            {
                if (!summoned70 && HP < Def.hp * 0.7f) { summoned70 = true; StartCoroutine(SummonRoutine(2)); }
                if (!summoned35 && HP < Def.hp * 0.35f) { summoned35 = true; StartCoroutine(SummonRoutine(3)); }
            }
            if (Def.isBoss && !enraged && HP < Def.hp * 0.35f)
            {
                enraged = true;
                if (Def.id == "lich") gm.UI.Hud.Banner("모르가스가 진정한 힘을 드러낸다!", "구체가 두 번 발사되고 어둠의 원이 세 개로 늘어납니다");
                else gm.UI.Hud.Banner("그락이 분노했다!", "공격 속도와 이동 속도가 증가합니다");
                baseMats[0] = Mats.Get(new Color(0.75f, 0.1f, 0.12f));
            }
            UpdateBar();
            if (HP <= 0) Die();
            else if (!busy) StartCoroutine(HitFlash());
        }

        IEnumerator HitFlash()
        {
            SetTint(Color.white);
            yield return new WaitForSeconds(0.07f);
            if (!IsDead) RestoreTint();
        }

        void UpdateBar()
        {
            if (bar != null) bar.Set(HP / (float)Def.hp);
        }

        void LateUpdate()
        {
            if (barRoot == null) return;
            var cam = Camera.main;
            bool show = !IsDead && cam != null && (HP < Def.hp || Aggro) && GameManager.I.State != GameState.Title;
            if (show)
            {
                var sp = cam.WorldToScreenPoint(transform.position + Vector3.up * (cc.height + 0.5f));
                Vector2 lp;
                if (sp.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(GameManager.I.Fx.worldLayer, sp, null, out lp))
                    barRoot.anchoredPosition = lp;
                else show = false;
            }
            if (barRoot.gameObject.activeSelf != show) barRoot.gameObject.SetActive(show);
            if (show) bar.Tick(Time.deltaTime);
        }

        public float HpRatio { get { return Mathf.Clamp01(HP / (float)Def.hp); } }

        void Die()
        {
            IsDead = true;
            Active.Remove(this);
            if (barRoot != null) Destroy(barRoot.gameObject);
            cc.enabled = false;
            var gm = GameManager.I;
            gm.OnEnemyKilled(this);
            if (Zone != null) Zone.OnDeath(this);
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            GameManager.I.Fx.Burst(transform.position + Vector3.up * 0.6f, new Color(0.7f, 0.5f, 0.9f), Def.isBoss ? 30 : 12, 6f, 0.2f);
            float t = 0; var s0 = model.localScale;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                model.localScale = s0 * (1f - t / 0.4f);
                model.Rotate(0, 720f * Time.deltaTime, 0);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            Active.Remove(this);
            if (barRoot != null) Destroy(barRoot.gameObject);
        }
    }

}
