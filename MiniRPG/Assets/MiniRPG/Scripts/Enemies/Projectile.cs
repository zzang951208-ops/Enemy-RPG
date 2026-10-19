using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRPG
{
    /// <summary>적의 원거리 투사체 (망령·망령 군주). 대시 무적으로 통과 가능</summary>
    public class Projectile : MonoBehaviour
    {
        Vector3 dir;
        float speed, life;
        int damage;
        Transform core;
        const float HitRadius = 0.75f;

        public static Projectile Fire(Vector3 pos, Vector3 direction, float speed, int damage, Color color, float life = 4f, bool withLight = true)
        {
            var go = new GameObject("Projectile");
            go.transform.position = pos;
            var p = go.AddComponent<Projectile>();
            direction.y = 0;
            p.dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            p.speed = speed; p.damage = damage; p.life = life;
            var outer = WorldBuilder.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.6f, color);
            outer.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            p.core = WorldBuilder.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.32f, Color.Lerp(color, Color.white, 0.7f)).transform;
            p.core.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (withLight)
            {
                var l = new GameObject("Glow").AddComponent<Light>();
                l.transform.SetParent(go.transform, false);
                l.type = LightType.Point; l.range = 3.5f; l.intensity = 1.6f; l.color = color;
                l.shadows = LightShadows.None;
            }
            return p;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.State == GameState.Title) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            life -= dt;
            if (life <= 0f) { Pop(gm); return; }
            transform.position += dir * speed * dt;
            core.localScale = Vector3.one * (0.3f + Mathf.Sin(Time.time * 20f) * 0.05f);

            var p = gm.Player;
            if (p == null || gm.Stats.Dead || gm.State != GameState.Playing) return;
            var d = p.transform.position - transform.position; d.y = 0;
            if (d.sqrMagnitude < HitRadius * HitRadius)
            {
                p.Hurt(damage, transform.position - dir);
                Pop(gm);
            }
        }

        void Pop(GameManager gm)
        {
            gm.Fx.Burst(transform.position, new Color(0.7f, 0.5f, 1f), 6, 3f, 0.14f);
            Destroy(gameObject);
        }
    }
}
