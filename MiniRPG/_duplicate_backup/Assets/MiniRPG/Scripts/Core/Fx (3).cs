using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>간단한 이펙트 모음 (파티클 시스템 없이 프리미티브로 구현)</summary>
    public class Fx : MonoBehaviour
    {
        public RectTransform worldLayer; // 화면 좌표로 따라다니는 UI 레이어

        public void FloatText(Vector3 worldPos, string text, Color color, int size = 26)
        {
            if (worldLayer == null) return;
            var t = UIFactory.Label(worldLayer, text, size, TextAnchor.MiddleCenter, color);
            t.fontStyle = FontStyle.Bold;
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.8f);
            t.rectTransform.sizeDelta = new Vector2(240, 50);
            StartCoroutine(FloatRoutine(t, worldPos + new Vector3(Random.Range(-0.4f, 0.4f), 0, 0)));
        }

        IEnumerator FloatRoutine(Text t, Vector3 wp)
        {
            float dur = 0.9f, e = 0f;
            var c = t.color;
            while (e < dur && t != null)
            {
                e += Time.deltaTime;
                float k = e / dur;
                var p = wp + Vector3.up * (k * 1.6f);
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(p);
                    Vector2 lp;
                    if (sp.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(worldLayer, sp, null, out lp))
                        t.rectTransform.anchoredPosition = lp;
                }
                float scale = k < 0.15f ? Mathf.Lerp(0.5f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, (k - 0.15f) * 3f);
                t.rectTransform.localScale = Vector3.one * scale;
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                t.color = c;
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        public void Ring(Vector3 pos, float radius, Color color, float dur = 0.3f)
        {
            var go = WorldBuilder.Prim(PrimitiveType.Cylinder, transform, pos + Vector3.up * 0.08f, new Vector3(0.2f, 0.02f, 0.2f), color);
            go.transform.SetParent(null, true);
            StartCoroutine(RingRoutine(go.transform, radius, dur));
        }

        IEnumerator RingRoutine(Transform t, float radius, float dur)
        {
            float e = 0;
            while (e < dur && t != null)
            {
                e += Time.deltaTime;
                float k = e / dur;
                float s = Mathf.Lerp(0.3f, radius * 2f, Mathf.Sqrt(k));
                t.localScale = new Vector3(s, 0.02f, s) * (k > 0.7f ? Mathf.Lerp(1f, 0.9f, (k - 0.7f) / 0.3f) : 1f);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        public void Burst(Vector3 pos, Color color, int count = 8, float speed = 5f, float size = 0.18f)
        {
            for (int i = 0; i < count; i++)
            {
                var go = WorldBuilder.Prim(PrimitiveType.Cube, transform, Vector3.zero, Vector3.one * size, color);
                go.transform.SetParent(null, false);
                go.transform.position = pos;
                go.transform.rotation = Random.rotation;
                var v = new Vector3(Random.Range(-1f, 1f), Random.Range(0.6f, 1.5f), Random.Range(-1f, 1f)).normalized * speed * Random.Range(0.5f, 1f);
                StartCoroutine(ParticleRoutine(go.transform, v, Random.Range(0.35f, 0.6f)));
            }
        }

        IEnumerator ParticleRoutine(Transform t, Vector3 v, float life)
        {
            float e = 0; var s0 = t.localScale;
            while (e < life && t != null)
            {
                float dt = Time.deltaTime;
                e += dt;
                v.y -= 14f * dt;
                t.position += v * dt;
                if (t.position.y < 0.05f) { t.position = new Vector3(t.position.x, 0.05f, t.position.z); v *= 0.5f; v.y = Mathf.Abs(v.y) * 0.3f; }
                t.Rotate(400f * dt, 300f * dt, 0);
                t.localScale = s0 * (1f - e / life);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        public void Dust(Vector3 pos) { Burst(pos + Vector3.up * 0.2f, new Color(0.8f, 0.78f, 0.7f), 6, 3f, 0.25f); }
    }

}
