using System.Collections;
using UnityEngine;

namespace MiniRPG
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public const float MoveSpeed = 6.2f;
        public const float AttackCooldown = 0.42f;
        public const float SkillCooldown = 3.5f;
        public const int SkillMpCost = 15;
        public const float DashCooldown = 1.1f;

        CharacterController cc;
        Transform model, swordPivot, body;
        Renderer bodyRenderer, swordRenderer;
        float attackCd, skillCd, dashCd, dashTimer, invuln, lastHurtTime, regenAcc, mpAcc;
        Vector3 dashDir, knock;
        float yVel;
        bool swinging;

        public float SkillCdRatio { get { return Mathf.Clamp01(skillCd / SkillCooldown); } }
        public float DashCdRatio { get { return Mathf.Clamp01(dashCd / DashCooldown); } }
        public NPC NearbyNPC { get; private set; }

        PlayerStats S { get { return GameManager.I.Stats; } }

        public static PlayerController Create(Vector3 pos)
        {
            var go = new GameObject("Player");
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f; cc.radius = 0.45f; cc.center = new Vector3(0, 0.95f, 0);
            cc.slopeLimit = 50; cc.stepOffset = 0.35f;
            var p = go.AddComponent<PlayerController>();
            p.BuildModel();
            WorldBuilder.MapIcon(go.transform, new Color(1f, 0.9f, 0.2f), 3.2f, true);
            return p;
        }

        void BuildModel()
        {
            cc = GetComponent<CharacterController>();
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            var skin = new Color(1f, 0.82f, 0.68f);
            body = WorldBuilder.Prim(PrimitiveType.Capsule, model, new Vector3(0, 0.85f, 0), new Vector3(0.8f, 0.6f, 0.65f), new Color(0.3f, 0.45f, 0.75f)).transform;
            bodyRenderer = body.GetComponent<Renderer>();
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.62f, 0), Vector3.one * 0.55f, skin);
            // 머리카락 & 눈
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.75f, -0.05f), new Vector3(0.6f, 0.35f, 0.6f), new Color(0.35f, 0.22f, 0.12f));
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.1f, 1.65f, 0.25f), Vector3.one * 0.08f, Color.black);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.1f, 1.65f, 0.25f), Vector3.one * 0.08f, Color.black);
            // 망토
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.95f, -0.33f), new Vector3(0.7f, 1.0f, 0.06f), new Color(0.7f, 0.15f, 0.18f), false, new Vector3(8, 0, 0));
            // 방패 (왼손)
            WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(-0.48f, 0.95f, 0.12f), new Vector3(0.08f, 0.55f, 0.45f), new Color(0.55f, 0.4f, 0.25f));
            // 검 (오른손, 피벗 기준으로 휘두름)
            swordPivot = new GameObject("SwordPivot").transform;
            swordPivot.SetParent(model, false);
            swordPivot.localPosition = new Vector3(0.45f, 0.95f, 0.1f);
            var blade = WorldBuilder.Prim(PrimitiveType.Cube, swordPivot, new Vector3(0, 0, 0.65f), new Vector3(0.1f, 0.05f, 1.1f), new Color(0.6f, 0.45f, 0.3f));
            swordRenderer = blade.GetComponent<Renderer>();
            WorldBuilder.Prim(PrimitiveType.Cube, swordPivot, new Vector3(0, 0, 0.1f), new Vector3(0.35f, 0.07f, 0.08f), new Color(0.3f, 0.3f, 0.3f));
            swordPivot.localEulerAngles = new Vector3(0, -30, 0);
        }

        /// <summary>장비에 따라 외형 색 갱신</summary>
        public void RefreshLook()
        {
            var inv = GameManager.I.Inv;
            if (inv.Armor != null) bodyRenderer.sharedMaterial = Mats.Get(inv.Armor.color);
            if (inv.Weapon != null) swordRenderer.sharedMaterial = Mats.Get(inv.Weapon.color);
        }

        public void Teleport(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
            yVel = 0; knock = Vector3.zero;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.State == GameState.Title) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            attackCd -= dt; skillCd -= dt; dashCd -= dt; invuln -= dt;

            // 중력 & 넉백
            yVel = cc.isGrounded ? -2f : yVel - 25f * dt;
            knock = Vector3.MoveTowards(knock, Vector3.zero, 30f * dt);
            Vector3 motion = new Vector3(0, yVel, 0) + knock;

            if (!S.Dead)
            {
                Regen(dt);
                if (gm.InputBlocked)
                {
                    NearbyNPC = null;
                }
                else
                {
                    motion += HandleControl(dt);
                    FindNPC();
                    if (GameInput.Interact && NearbyNPC != null) NearbyNPC.Interact();
                }
            }
            cc.Move(motion * dt);
        }

        Vector3 HandleControl(float dt)
        {
            Vector3 move = Vector3.zero;
            if (dashTimer > 0f)
            {
                dashTimer -= dt;
                move = dashDir * 17f;
            }
            else
            {
                var m = GameInput.Move();
                var dir = new Vector3(m.x, 0, m.y);
                move = dir * MoveSpeed * (swinging ? 0.45f : 1f);
                if (dir.sqrMagnitude > 0.01f)
                {
                    var target = Quaternion.LookRotation(dir);
                    model.rotation = Quaternion.Slerp(model.rotation, target, 16f * dt);
                }
                // 걷기 흔들림
                float bob = dir.sqrMagnitude > 0.01f ? Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.08f : 0f;
                model.localPosition = new Vector3(0, bob, 0);

                if (GameInput.Dash && dashCd <= 0f)
                {
                    dashDir = dir.sqrMagnitude > 0.01f ? dir.normalized : model.forward;
                    dashTimer = 0.16f; dashCd = DashCooldown; invuln = 0.3f;
                    SoundFX.Play(Sfx.Dash, 0.6f);
                    GameManager.I.Fx.Dust(transform.position);
                }
            }

            if (GameInput.Attack && attackCd <= 0f) { attackCd = AttackCooldown; StartCoroutine(Swing()); }
            if (GameInput.Skill)
            {
                if (skillCd > 0f) { }
                else if (S.mp < SkillMpCost) GameManager.I.UI.Hud.Toast(UIFactory.Col("MP가 부족합니다", UIFactory.Bad));
                else { S.UseMP(SkillMpCost); skillCd = SkillCooldown; StartCoroutine(Spin()); }
            }
            if (GameInput.Potion1) UseBestPotion(true);
            if (GameInput.Potion2) UseBestPotion(false);
            return move;
        }

        void UseBestPotion(bool hp)
        {
            var inv = GameManager.I.Inv;
            string id = null;
            if (hp)
            {
                if (S.hp >= S.MaxHP) { GameManager.I.UI.Hud.Toast("HP가 이미 가득 찼습니다"); return; }
                int missing = S.MaxHP - S.hp;
                // 부족한 양이 크면 대형 포션 우선
                if (missing > 100 && inv.Count("potion_l") > 0) id = "potion_l";
                else if (inv.Count("potion_s") > 0) id = "potion_s";
                else if (inv.Count("potion_l") > 0) id = "potion_l";
            }
            else
            {
                if (S.mp >= S.MaxMP) { GameManager.I.UI.Hud.Toast("MP가 이미 가득 찼습니다"); return; }
                if (inv.Count("mana_potion") > 0) id = "mana_potion";
            }
            if (id == null) { GameManager.I.UI.Hud.Toast(UIFactory.Col(hp ? "체력 포션이 없습니다" : "마나 포션이 없습니다", UIFactory.Bad)); return; }
            GameManager.I.UseItem(id);
        }

        void Regen(float dt)
        {
            // 전투 이탈 4초 후 HP 자연 회복, MP는 항상 천천히 회복
            bool inVillage = WorldBuilder.ZoneAt(transform.position) == 0;
            if (Time.time - lastHurtTime > 4f && S.hp < S.MaxHP)
            {
                regenAcc += dt * (inVillage ? 12f : 1.5f);
                if (regenAcc >= 1f) { int n = (int)regenAcc; regenAcc -= n; S.Heal(n); }
            }
            if (S.mp < S.MaxMP)
            {
                mpAcc += dt * (inVillage ? 6f : 1.2f);
                if (mpAcc >= 1f) { int n = (int)mpAcc; mpAcc -= n; S.RestoreMP(n); }
            }
        }

        void FindNPC()
        {
            NPC best = null; float bestD = 3.2f;
            foreach (var n in NPC.All)
            {
                float d = Vector3.Distance(n.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = n; }
            }
            NearbyNPC = best;
        }

        IEnumerator Swing()
        {
            swinging = true;
            SoundFX.Play(Sfx.Swing, 0.7f);
            // 가장 가까운 적 쪽으로 살짝 방향 보정
            var target = NearestEnemy(4f);
            if (target != null)
            {
                var d = target.transform.position - transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f) model.rotation = Quaternion.LookRotation(d);
            }
            float t = 0f; bool hit = false;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float k = t / 0.18f;
                swordPivot.localEulerAngles = new Vector3(0, Mathf.Lerp(-80f, 90f, k), 0);
                if (!hit && k > 0.35f) { hit = true; DoHit(2.6f, 80f, 1f, false); }
                yield return null;
            }
            yield return new WaitForSeconds(0.06f);
            swordPivot.localEulerAngles = new Vector3(0, -30, 0);
            swinging = false;
        }

        IEnumerator Spin()
        {
            swinging = true;
            SoundFX.Play(Sfx.Skill, 0.9f);
            GameManager.I.Fx.Ring(transform.position, 3.8f, new Color(0.5f, 0.8f, 1f));
            float t = 0f;
            var baseRot = model.rotation;
            swordPivot.localEulerAngles = new Vector3(0, 90, 0);
            bool hit = false;
            while (t < 0.38f)
            {
                t += Time.deltaTime;
                model.rotation = baseRot * Quaternion.Euler(0, 720f * (t / 0.38f), 0);
                if (!hit && t > 0.12f) { hit = true; DoHit(3.8f, 360f, 1.9f, true); }
                yield return null;
            }
            model.rotation = baseRot;
            swordPivot.localEulerAngles = new Vector3(0, -30, 0);
            swinging = false;
        }

        Enemy NearestEnemy(float range)
        {
            Enemy best = null; float bestD = range;
            foreach (var e in Enemy.Active)
            {
                if (e.IsDead) continue;
                float d = Vector3.Distance(e.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        void DoHit(float range, float angle, float mult, bool skill)
        {
            var list = Enemy.Active.ToArray();
            bool any = false;
            foreach (var e in list)
            {
                if (e == null || e.IsDead) continue;
                var d = e.transform.position - transform.position; d.y = 0;
                float reach = range + e.Radius;
                if (d.magnitude > reach) continue;
                if (angle < 360f && d.sqrMagnitude > 0.01f && Vector3.Angle(model.forward, d) > angle * 0.5f) continue;
                bool crit = Random.value < 0.12f;
                float atk = S.Atk * mult * (crit ? 1.8f : 1f);
                int dmg = CombatMath.Damage(atk, e.Def.def);
                e.TakeDamage(dmg, crit, transform.position);
                any = true;
            }
            if (any) GameManager.I.Cam.Shake(skill ? 0.25f : 0.12f, 0.12f);
        }

        public void Hurt(int rawAtk, Vector3 from)
        {
            if (S.Dead || invuln > 0f || GameManager.I.State != GameState.Playing) return;
            int dmg = S.TakeDamage(rawAtk);
            lastHurtTime = Time.time;
            invuln = 0.45f;
            var dir = transform.position - from; dir.y = 0;
            knock = dir.normalized * 7f;
            SoundFX.Play(Sfx.Hurt, 0.8f);
            GameManager.I.Fx.FloatText(transform.position + Vector3.up * 2.2f, "-" + dmg, new Color(1f, 0.4f, 0.4f), 30);
            GameManager.I.Cam.Shake(0.3f, 0.18f);
            StartCoroutine(FlashHurt());
            if (S.Dead) GameManager.I.OnPlayerDeath();
        }

        IEnumerator FlashHurt()
        {
            var mat = bodyRenderer.sharedMaterial;
            bodyRenderer.sharedMaterial = Mats.Get(new Color(1f, 0.25f, 0.25f));
            yield return new WaitForSeconds(0.12f);
            bodyRenderer.sharedMaterial = mat;
            RefreshLook();
        }
    }
}
