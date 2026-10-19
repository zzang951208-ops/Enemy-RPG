using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>달빛 약초: 가까이 가면 자동으로 채집, 일정 시간 후 다시 자람</summary>
    public class Herb : MonoBehaviour
    {
        public static readonly List<Herb> All = new List<Herb>();
        const float PickRadius = 1.6f;
        const float RespawnTime = 45f;

        Transform visual;
        GameObject icon;
        float regrow;
        bool available = true;
        float phase;

        public static Herb Create(Vector3 pos)
        {
            var go = new GameObject("Herb");
            go.transform.SetParent(WorldBuilder.Root, false);
            go.transform.position = pos;
            var h = go.AddComponent<Herb>();
            h.phase = Random.value * 10f;
            h.visual = new GameObject("Visual").transform;
            h.visual.SetParent(go.transform, false);
            var leaf = new Color(0.45f, 0.95f, 0.65f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                WorldBuilder.Prim(PrimitiveType.Cube, h.visual, Quaternion.Euler(0, a, 0) * new Vector3(0, 0.22f, 0.14f),
                                  new Vector3(0.09f, 0.42f, 0.2f), leaf, false, new Vector3(25f, a, 0));
            }
            WorldBuilder.Prim(PrimitiveType.Sphere, h.visual, new Vector3(0, 0.5f, 0), Vector3.one * 0.2f, new Color(0.85f, 1f, 0.95f));
            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(h.visual, false);
            glow.transform.localPosition = new Vector3(0, 0.6f, 0);
            glow.type = LightType.Point;
            glow.color = new Color(0.6f, 1f, 0.85f);
            glow.range = 3.5f;
            glow.intensity = 1.2f;
            h.icon = WorldBuilder.MapIcon(go.transform, new Color(0.5f, 1f, 0.7f), 1.4f);
            All.Add(h);
            return h;
        }

        public void ResetHerb()
        {
            available = true;
            regrow = 0;
            visual.gameObject.SetActive(true);
            if (icon != null) icon.SetActive(true);
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.State != GameState.Playing) return;
            if (!available)
            {
                regrow -= Time.deltaTime;
                if (regrow <= 0f) ResetHerb();
                return;
            }
            // 은은한 흔들림
            float t = Time.time + phase;
            visual.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.5f) * 0.06f);
            visual.localRotation = Quaternion.Euler(0, t * 20f, 0);

            var p = gm.Player;
            if (p == null || gm.Stats.Dead) return;
            var d = p.transform.position - transform.position; d.y = 0;
            if (d.sqrMagnitude < PickRadius * PickRadius) Pick(gm);
        }

        void Pick(GameManager gm)
        {
            available = false;
            regrow = RespawnTime;
            visual.gameObject.SetActive(false);
            if (icon != null) icon.SetActive(false);
            gm.Inv.Add("herb");
            SoundFX.Play(Sfx.Pickup);
            gm.Fx.Burst(transform.position + Vector3.up * 0.4f, new Color(0.6f, 1f, 0.8f), 10, 3f, 0.14f);
            gm.Fx.FloatText(transform.position + Vector3.up * 1.2f, "+ 달빛 약초", new Color(0.6f, 1f, 0.8f), 22);
        }
    }
}
