using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRPG
{
    /// <summary>렌더 파이프라인(Built-in / URP) 상관없이 동작하는 머티리얼 캐시</summary>
    public static class Mats
    {
        static Material template;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        public static Material Get(Color c)
        {
            Material m;
            if (cache.TryGetValue(c, out m) && m != null) return m;
            if (template == null)
            {
                // 기본 프리미티브의 머티리얼은 현재 파이프라인에 맞는 셰이더를 사용함
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                template = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(tmp);
            }
            m = new Material(template);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.12f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            cache[c] = m;
            return m;
        }
    }

    public static class WorldBuilder
    {
        public const int MinimapLayer = 31;
        public static Transform Root;

        // 구역 (z 좌표 기준)
        public static readonly float[] ZoneStart = { -999f, 27f, 73f, 113f, 153f, 201f };
        public static readonly string[] ZoneName = { "새벽빛 마을", "슬라임 초원", "속삭이는 숲", "고블린 야영지", "고블린 왕의 제단", "잊혀진 폐허" };
        public static readonly string[] ZoneSub = { "안전 지대", "권장 Lv.1~3", "권장 Lv.3~6", "권장 Lv.6~9", "보스 지역 · 권장 Lv.9+", "망령의 영역 · 권장 Lv.11+" };

        public static int ZoneAt(Vector3 p)
        {
            int z = 0;
            for (int i = 0; i < ZoneStart.Length; i++) if (p.z >= ZoneStart[i]) z = i;
            return z;
        }

        public static readonly Vector3 PlayerSpawn = new Vector3(0, 0, -5);
        public static readonly List<Vector3> HerbSpots = new List<Vector3>();
        public static readonly Vector3 DollSpot = new Vector3(-31f, 0, 99f);
        static GameObject sealBarrier;
        static Transform sealRunes;

        /// <summary>제단 북쪽 봉인 개폐 (그락 처치 후 개방)</summary>
        public static void SetSealOpen(bool open)
        {
            if (sealBarrier != null) sealBarrier.SetActive(!open);
            if (sealRunes != null) sealRunes.gameObject.SetActive(!open);
        }

        public static bool SealOpen { get { return sealBarrier != null && !sealBarrier.activeSelf; } }

        // ─────────────── 기본 도형 ───────────────
        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color c, bool keepCollider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(t);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = Mats.Get(c);
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            return go;
        }

        public static Transform Group(string name, Transform parent, Vector3 pos, float yRot = 0)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0, yRot, 0);
            return g;
        }

        /// <summary>미니맵 카메라에만 보이는 아이콘</summary>
        public static GameObject MapIcon(Transform parent, Color c, float size, bool diamond = false)
        {
            var go = Prim(PrimitiveType.Cube, parent, new Vector3(0, 30f, 0), new Vector3(size, 0.1f, size), c, false,
                          diamond ? new Vector3(0, 45, 0) : (Vector3?)null);
            go.layer = MinimapLayer;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        public static TextMesh Sign3D(Transform parent, Vector3 pos, string text, Color color, float charSize = 0.35f)
        {
            var go = new GameObject("Sign_" + text);
            go.transform.SetParent(parent, false);
            go.transform.position = parent.position + pos;
            go.transform.rotation = Quaternion.Euler(50f, 0, 0);
            var tm = go.AddComponent<TextMesh>();
            var f = UIFactory.Font;
            if (f != null)
            {
                tm.font = f;
                go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            }
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize * 0.1f * 2f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            return tm;
        }

        // ─────────────── 월드 생성 ───────────────
        public static void Build()
        {
            Random.InitState(20260929);
            Root = new GameObject("World").transform;
            HerbSpots.Clear();

            SetupLighting();
            BuildGround();
            BuildVillage();
            BuildMeadow();
            BuildForest();
            BuildGoblinCamp();
            BuildBossAltar();
            BuildSealGate();
            BuildRuins();
            BuildBorders();
        }

        static void SetupLighting()
        {
            var old = GameObject.Find("Directional Light");
            if (old != null) old.SetActive(false);

            var lgo = new GameObject("Sun");
            lgo.transform.SetParent(Root, false);
            lgo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.95f, 0.86f);
            l.intensity = 1.15f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.65f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.5f, 0.58f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 38f;
            RenderSettings.fogEndDistance = 95f;
            RenderSettings.fogColor = new Color(0.62f, 0.75f, 0.88f);
        }

        static void Slab(Vector3 center, Vector3 size, Color c)
        {
            var go = Prim(PrimitiveType.Cube, Root, center + new Vector3(0, -size.y * 0.5f, 0), size, c, true);
            go.name = "Ground";
        }

        static void BuildGround()
        {
            Slab(new Vector3(0, 0, -5), new Vector3(110, 1, 64), new Color(0.47f, 0.66f, 0.36f));   // 마을
            Slab(new Vector3(0, 0, 50), new Vector3(110, 1, 46), new Color(0.52f, 0.72f, 0.34f));   // 초원
            Slab(new Vector3(0, 0, 93), new Vector3(110, 1, 40), new Color(0.26f, 0.45f, 0.26f));   // 숲
            Slab(new Vector3(0, 0, 133), new Vector3(110, 1, 40), new Color(0.5f, 0.45f, 0.32f));   // 야영지
            Slab(new Vector3(0, 0, 178), new Vector3(110, 1, 50), new Color(0.36f, 0.33f, 0.36f));  // 제단

            // 북쪽으로 이어지는 흙길
            for (float z = 26; z < 160; z += 4f)
            {
                float x = Mathf.Sin(z * 0.05f) * 3f;
                Prim(PrimitiveType.Cube, Root, new Vector3(x, 0.005f, z), new Vector3(4.2f, 0.02f, 4.4f), new Color(0.62f, 0.5f, 0.34f));
            }
            // 마을 광장 바닥
            Prim(PrimitiveType.Cylinder, Root, new Vector3(0, 0.0f, 2), new Vector3(20, 0.02f, 20), new Color(0.72f, 0.66f, 0.55f));
        }

        static readonly Color Wall = new Color(0.9f, 0.84f, 0.7f);
        static readonly Color Wood = new Color(0.45f, 0.3f, 0.18f);
        static readonly Color DarkWood = new Color(0.3f, 0.2f, 0.12f);
        static readonly Color WindowGlow = new Color(1f, 0.88f, 0.5f);

        public static Transform House(Vector3 pos, float yRot, Vector3 size, Color roof, string sign = null)
        {
            var h = Group("House", Root, pos, yRot);
            float w = size.x, ht = size.y, d = size.z;
            Prim(PrimitiveType.Cube, h, new Vector3(0, ht / 2, 0), new Vector3(w, ht, d), Wall, true);
            // 기둥
            foreach (var sx in new[] { -1, 1 })
                foreach (var sz in new[] { -1, 1 })
                    Prim(PrimitiveType.Cube, h, new Vector3(sx * (w / 2), ht / 2, sz * (d / 2)), new Vector3(0.35f, ht + 0.05f, 0.35f), Wood);
            Prim(PrimitiveType.Cube, h, new Vector3(0, ht, 0), new Vector3(w + 0.1f, 0.3f, d + 0.1f), Wood);
            // 박공 지붕 (45도 회전 큐브)
            float r = d / 1.414f + 0.35f;
            Prim(PrimitiveType.Cube, h, new Vector3(0, ht + 0.1f, 0), new Vector3(w + 0.8f, r, r), roof, false, new Vector3(45, 0, 0));
            // 문 & 창문 (+z 방향이 정면)
            Prim(PrimitiveType.Cube, h, new Vector3(0, 1.1f, d / 2 + 0.02f), new Vector3(1.2f, 2.2f, 0.12f), DarkWood);
            Prim(PrimitiveType.Sphere, h, new Vector3(0.4f, 1.1f, d / 2 + 0.1f), Vector3.one * 0.12f, UIFactory.Gold);
            foreach (var sx in new[] { -1, 1 })
            {
                Prim(PrimitiveType.Cube, h, new Vector3(sx * w * 0.3f, ht * 0.55f, d / 2 + 0.02f), new Vector3(1f, 0.9f, 0.1f), WindowGlow);
                Prim(PrimitiveType.Cube, h, new Vector3(sx * w * 0.3f, ht * 0.55f, d / 2 + 0.06f), new Vector3(1.1f, 0.08f, 0.06f), Wood);
            }
            // 굴뚝
            Prim(PrimitiveType.Cube, h, new Vector3(w * 0.3f, ht + r * 0.6f, -d * 0.15f), new Vector3(0.7f, 1.6f, 0.7f), new Color(0.55f, 0.35f, 0.3f));
            if (!string.IsNullOrEmpty(sign))
            {
                Prim(PrimitiveType.Cube, h, new Vector3(0, ht * 0.95f, d / 2 + 0.15f), new Vector3(2.6f, 0.8f, 0.12f), Wood);
                var tm = Sign3D(h, h.rotation * new Vector3(0, ht * 0.95f + 0.1f, d / 2 + 0.3f), sign, new Color(1f, 0.93f, 0.7f), 0.28f);
                tm.transform.SetParent(Root, true);
            }
            return h;
        }

        public static void Tree(Vector3 pos, float scale = 1f, bool pine = false)
        {
            var t = Group("Tree", Root, pos, Random.Range(0, 360f));
            t.localScale = Vector3.one * scale;
            var trunk = Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.1f, 0), new Vector3(0.45f, 1.1f, 0.45f), new Color(0.42f, 0.28f, 0.18f), true);
            trunk.name = "Trunk";
            if (pine)
            {
                var g = new Color(0.16f, 0.36f + Random.Range(-0.04f, 0.05f), 0.22f);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 2.3f, 0), new Vector3(2.4f, 1.3f, 2.4f), g, false, new Vector3(0, 45, 0));
                Prim(PrimitiveType.Cube, t, new Vector3(0, 3.3f, 0), new Vector3(1.7f, 1.1f, 1.7f), g * 1.1f, false, new Vector3(0, 20, 0));
                Prim(PrimitiveType.Cube, t, new Vector3(0, 4.1f, 0), new Vector3(1.0f, 0.9f, 1.0f), g * 1.2f, false, new Vector3(0, 60, 0));
            }
            else
            {
                var g = new Color(0.28f + Random.Range(-0.05f, 0.05f), 0.55f + Random.Range(-0.06f, 0.06f), 0.26f);
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 2.9f, 0), new Vector3(2.8f, 2.4f, 2.8f), g);
                Prim(PrimitiveType.Sphere, t, new Vector3(0.6f, 3.6f, 0.3f), new Vector3(1.7f, 1.5f, 1.7f), g * 1.12f);
                Prim(PrimitiveType.Sphere, t, new Vector3(-0.7f, 3.3f, -0.4f), new Vector3(1.5f, 1.3f, 1.5f), g * 0.92f);
            }
        }

        public static void Rock(Vector3 pos, float s)
        {
            var c = new Color(0.52f, 0.52f, 0.55f) * Random.Range(0.85f, 1.1f);
            c.a = 1;
            Prim(PrimitiveType.Sphere, Root, pos + new Vector3(0, s * 0.25f, 0), new Vector3(s * 1.3f, s * 0.8f, s), c, true,
                 new Vector3(Random.Range(-10, 10), Random.Range(0, 360), Random.Range(-10, 10)));
        }

        static void Flower(Vector3 pos)
        {
            Color[] cols = { new Color(1f, 0.5f, 0.6f), new Color(1f, 0.9f, 0.3f), new Color(0.7f, 0.6f, 1f), Color.white };
            Prim(PrimitiveType.Sphere, Root, pos + new Vector3(0, 0.15f, 0), Vector3.one * 0.25f, cols[Random.Range(0, cols.Length)]);
        }

        static void Lamp(Vector3 pos)
        {
            var g = Group("Lamp", Root, pos);
            Prim(PrimitiveType.Cylinder, g, new Vector3(0, 1.5f, 0), new Vector3(0.18f, 1.5f, 0.18f), new Color(0.2f, 0.2f, 0.22f), true);
            Prim(PrimitiveType.Sphere, g, new Vector3(0, 3.1f, 0), Vector3.one * 0.55f, new Color(1f, 0.9f, 0.55f));
            var lgo = new GameObject("Light");
            lgo.transform.SetParent(g, false);
            lgo.transform.localPosition = new Vector3(0, 3f, 0);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point; l.range = 7f; l.intensity = 1.2f; l.color = new Color(1f, 0.8f, 0.5f);
        }

        static void Fence(Vector3 a, Vector3 b)
        {
            float len = Vector3.Distance(a, b);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)n);
                Prim(PrimitiveType.Cube, Root, p + new Vector3(0, 0.6f, 0), new Vector3(0.25f, 1.2f, 0.25f), Wood);
            }
            var mid = (a + b) * 0.5f;
            var rot = Quaternion.LookRotation((b - a).normalized).eulerAngles;
            Prim(PrimitiveType.Cube, Root, mid + new Vector3(0, 0.8f, 0), new Vector3(0.12f, 0.15f, len), Wood, true, rot);
            Prim(PrimitiveType.Cube, Root, mid + new Vector3(0, 0.4f, 0), new Vector3(0.12f, 0.15f, len), Wood, false, rot);
        }

        public static void Stall(Vector3 pos, float yRot, Color canopy, string sign)
        {
            var s = Group("Stall", Root, pos, yRot);
            Prim(PrimitiveType.Cube, s, new Vector3(0, 0.5f, 0), new Vector3(3f, 1f, 1.2f), Wood, true);
            foreach (var sx in new[] { -1.4f, 1.4f })
                Prim(PrimitiveType.Cylinder, s, new Vector3(sx, 1.4f, -0.5f), new Vector3(0.12f, 1.4f, 0.12f), DarkWood);
            Prim(PrimitiveType.Cube, s, new Vector3(0, 2.8f, 0), new Vector3(3.4f, 0.15f, 2.2f), canopy, false, new Vector3(12, 0, 0));
            // 좌판 물건
            for (int i = 0; i < 5; i++)
                Prim(PrimitiveType.Sphere, s, new Vector3(-1.1f + i * 0.55f, 1.12f, 0.1f), Vector3.one * 0.3f,
                     Color.HSVToRGB(Random.value, 0.6f, 0.9f));
            if (!string.IsNullOrEmpty(sign))
            {
                var tm = Sign3D(s, s.rotation * new Vector3(0, 3.35f, 0.9f), sign, Color.white, 0.25f);
                tm.transform.SetParent(Root, true);
            }
        }

        // 카메라가 항상 북쪽(+z)을 바라보므로 주요 건물은 정면이 남쪽(-z)을 향하도록 배치
        static void BuildVillage()
        {
            House(new Vector3(-8, 0, 17), 180, new Vector3(8, 4.2f, 6), new Color(0.65f, 0.25f, 0.2f), "촌장의 집");
            House(new Vector3(-20, 0, 14), 180, new Vector3(7.5f, 4f, 6), new Color(0.3f, 0.45f, 0.65f), "여관 '졸린 곰'");
            House(new Vector3(9, 0, 17), 180, new Vector3(7, 3.6f, 6), new Color(0.35f, 0.35f, 0.38f), "브론의 대장간");
            House(new Vector3(-17, 0, -17), 180, new Vector3(6, 3.4f, 5), new Color(0.55f, 0.4f, 0.25f));
            House(new Vector3(17, 0, -17), 180, new Vector3(6, 3.4f, 5), new Color(0.45f, 0.55f, 0.3f));
            House(new Vector3(0, 0, -20), 180, new Vector3(6, 3.2f, 5), new Color(0.6f, 0.3f, 0.35f));

            // 대장간 소품: 모루와 화로
            var forge = Group("Forge", Root, new Vector3(8, 0, 11.5f));
            Prim(PrimitiveType.Cube, forge, new Vector3(0, 0.45f, 0), new Vector3(1.2f, 0.9f, 0.6f), new Color(0.2f, 0.2f, 0.22f), true);
            Prim(PrimitiveType.Cube, forge, new Vector3(0, 0.95f, 0), new Vector3(1.4f, 0.2f, 0.5f), new Color(0.3f, 0.3f, 0.33f));
            Prim(PrimitiveType.Cylinder, forge, new Vector3(3.2f, 0.5f, 0.5f), new Vector3(1.3f, 0.5f, 1.3f), new Color(0.35f, 0.3f, 0.3f), true);
            Prim(PrimitiveType.Sphere, forge, new Vector3(3.2f, 1.0f, 0.5f), new Vector3(0.9f, 0.4f, 0.9f), new Color(1f, 0.45f, 0.1f));
            var fl = new GameObject("ForgeLight").AddComponent<Light>();
            fl.transform.SetParent(forge, false);
            fl.transform.localPosition = new Vector3(3.2f, 1.6f, 0.5f);
            fl.type = LightType.Point; fl.color = new Color(1f, 0.5f, 0.2f); fl.range = 6; fl.intensity = 1.6f;
            fl.gameObject.AddComponent<Flicker>();

            // 우물 (광장 중앙)
            var well = Group("Well", Root, new Vector3(0, 0, 2));
            Prim(PrimitiveType.Cylinder, well, new Vector3(0, 0.5f, 0), new Vector3(2.4f, 0.5f, 2.4f), new Color(0.6f, 0.6f, 0.62f), true);
            Prim(PrimitiveType.Cylinder, well, new Vector3(0, 0.95f, 0), new Vector3(1.9f, 0.05f, 1.9f), new Color(0.2f, 0.4f, 0.7f));
            foreach (var sx in new[] { -1.05f, 1.05f })
                Prim(PrimitiveType.Cube, well, new Vector3(sx, 1.7f, 0), new Vector3(0.2f, 1.6f, 0.2f), Wood);
            Prim(PrimitiveType.Cube, well, new Vector3(0, 2.6f, 0), new Vector3(2.8f, 0.2f, 1.6f), new Color(0.6f, 0.25f, 0.2f));

            // 노점 (약초상) — NPC는 좌판 뒤(북쪽)에 섬
            Stall(new Vector3(-12, 0, 2.5f), 180, new Color(0.35f, 0.7f, 0.4f), "리나의 약초 가게");
            // 학자의 연구 좌판 — 책 더미와 촛불
            Stall(new Vector3(12, 0, 2.5f), 180, new Color(0.3f, 0.35f, 0.7f), "엘라라의 연구소");
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Cube, Root, new Vector3(14.3f, 0.12f + i * 0.2f, 1.2f), new Vector3(0.7f, 0.18f, 0.5f),
                     Color.HSVToRGB(0.55f + i * 0.1f, 0.5f, 0.6f), false, new Vector3(0, i * 17f, 0));
            Prim(PrimitiveType.Cylinder, Root, new Vector3(10.2f, 0.35f, 1.3f), new Vector3(0.7f, 0.35f, 0.7f), new Color(0.35f, 0.3f, 0.3f), true);

            // 도박사의 천막
            var tent = Group("CasinoTent", Root, new Vector3(20, 0, 14), 180);
            Prim(PrimitiveType.Cylinder, tent, new Vector3(0, 1.4f, 0), new Vector3(5f, 1.4f, 5f), new Color(0.45f, 0.2f, 0.55f), true);
            Prim(PrimitiveType.Cylinder, tent, new Vector3(0, 3.0f, 0), new Vector3(4.2f, 0.4f, 4.2f), new Color(0.95f, 0.8f, 0.25f));
            Prim(PrimitiveType.Sphere, tent, new Vector3(0, 3.4f, 0), new Vector3(4f, 1.6f, 4f), new Color(0.55f, 0.25f, 0.65f));
            Prim(PrimitiveType.Sphere, tent, new Vector3(0, 4.3f, 0), Vector3.one * 0.5f, UIFactory.Gold);
            Prim(PrimitiveType.Cube, tent, new Vector3(0, 1.2f, 2.45f), new Vector3(1.6f, 2.4f, 0.2f), new Color(0.15f, 0.05f, 0.2f));
            // 도박 테이블
            var table = Group("DiceTable", Root, new Vector3(17.5f, 0, 6.5f), 0);
            Prim(PrimitiveType.Cylinder, table, new Vector3(0, 0.45f, 0), new Vector3(0.3f, 0.45f, 0.3f), DarkWood, true);
            Prim(PrimitiveType.Cylinder, table, new Vector3(0, 0.92f, 0), new Vector3(1.8f, 0.05f, 1.8f), new Color(0.1f, 0.45f, 0.25f));
            Prim(PrimitiveType.Cube, table, new Vector3(-0.3f, 1.05f, 0.1f), Vector3.one * 0.2f, Color.white, false, new Vector3(0, 30, 0));
            Prim(PrimitiveType.Cube, table, new Vector3(0.2f, 1.05f, -0.15f), Vector3.one * 0.2f, Color.white, false, new Vector3(0, 70, 0));
            Sign3D(Root, new Vector3(20f, 5.4f, 11.2f), "행운의 천막", UIFactory.Gold, 0.3f);

            // 울타리 (북쪽 출입구만 개방)
            Fence(new Vector3(-27, 0, -27), new Vector3(27, 0, -27));
            Fence(new Vector3(-27, 0, -27), new Vector3(-27, 0, 26));
            Fence(new Vector3(27, 0, -27), new Vector3(27, 0, 26));
            Fence(new Vector3(-27, 0, 26), new Vector3(-5, 0, 26));
            Fence(new Vector3(5, 0, 26), new Vector3(27, 0, 26));
            // 정문
            foreach (var sx in new[] { -5f, 5f })
                Prim(PrimitiveType.Cube, Root, new Vector3(sx, 2f, 26), new Vector3(0.7f, 4f, 0.7f), Wood, true);
            Prim(PrimitiveType.Cube, Root, new Vector3(0, 4.1f, 26), new Vector3(11f, 0.5f, 0.6f), Wood);
            Sign3D(Root, new Vector3(0, 4.9f, 25.6f), "새벽빛 마을", new Color(1f, 0.93f, 0.7f), 0.35f);

            // 가로등, 나무, 꽃
            Lamp(new Vector3(-5, 0, 7)); Lamp(new Vector3(5, 0, 7)); Lamp(new Vector3(-5, 0, -5)); Lamp(new Vector3(5, 0, -5));
            Lamp(new Vector3(-3.5f, 0, 23)); Lamp(new Vector3(3.5f, 0, 23));
            Vector3[] trees = { new Vector3(-23, 0, -23), new Vector3(23, 0, -23), new Vector3(-24, 0, 0), new Vector3(24, 0, -2),
                                new Vector3(-13, 0, 23), new Vector3(14, 0, 23), new Vector3(-8, 0, -24), new Vector3(9, 0, -24),
                                new Vector3(-24, 0, 22), new Vector3(24, 0, 22) };
            foreach (var t in trees) Tree(t, Random.Range(0.9f, 1.2f));
            for (int i = 0; i < 45; i++)
            {
                var p = new Vector3(Random.Range(-25f, 25f), 0, Random.Range(-25f, 25f));
                if (new Vector2(p.x, p.z - 2).magnitude < 11f) continue;
                if (p.z > 8f && p.z < 21f) continue;
                Flower(p);
            }
            // 나무 상자, 통
            Prim(PrimitiveType.Cube, Root, new Vector3(-14.5f, 0.5f, 9), Vector3.one, Wood, true, new Vector3(0, 20, 0));
            Prim(PrimitiveType.Cube, Root, new Vector3(-13.6f, 0.4f, 10f), Vector3.one * 0.8f, Wood, true, new Vector3(0, 50, 0));
            Prim(PrimitiveType.Cylinder, Root, new Vector3(13.5f, 0.6f, 11), new Vector3(0.9f, 0.6f, 0.9f), new Color(0.5f, 0.35f, 0.2f), true);
        }

        static void BuildMeadow()
        {
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3(Random.Range(-44f, 44f), 0, Random.Range(28f, 72f));
                if (Mathf.Abs(p.x) < 4) continue;
                Flower(p);
            }
            for (int i = 0; i < 12; i++)
            {
                var p = new Vector3(Random.Range(-44f, 44f), 0, Random.Range(30f, 70f));
                if (Mathf.Abs(p.x) < 25f) p.x = Mathf.Sign(p.x) * Random.Range(25f, 44f);
                if (i % 3 == 0) Rock(p, Random.Range(0.8f, 1.8f)); else Tree(p, Random.Range(0.8f, 1.1f));
            }
            // 약초 위치
            Vector3[] herbs = { new Vector3(-14, 0, 36), new Vector3(12, 0, 41), new Vector3(-20, 0, 55), new Vector3(20, 0, 60),
                                new Vector3(-6, 0, 66), new Vector3(8, 0, 32), new Vector3(-28, 0, 44), new Vector3(27, 0, 48) };
            HerbSpots.AddRange(herbs);
            Sign3D(Root, new Vector3(6, 2.2f, 30), "↑ 슬라임 초원", Color.white, 0.25f);
            Prim(PrimitiveType.Cube, Root, new Vector3(6, 1f, 30.1f), new Vector3(0.2f, 2f, 0.2f), Wood, true);
        }

        static void BuildForest()
        {
            for (int i = 0; i < 110; i++)
            {
                var p = new Vector3(Random.Range(-46f, 46f), 0, Random.Range(74f, 112f));
                float pathX = Mathf.Sin(p.z * 0.05f) * 3f;
                if (Mathf.Abs(p.x - pathX) < 5f) continue;
                if (Mathf.Abs(p.x) < 16f && Mathf.Abs(p.z - 93f) < 12f && Random.value < 0.7f) continue; // 전투 공간 확보
                if (Vector3.Distance(p, DollSpot) < 5f) continue;                                      // 인형 퀘스트 공터
                Tree(p, Random.Range(0.9f, 1.4f), Random.value < 0.6f);
            }
            for (int i = 0; i < 10; i++) Rock(new Vector3(Random.Range(-40f, 40f), 0, Random.Range(76f, 110f)), Random.Range(0.6f, 1.5f));
            Rock(DollSpot + new Vector3(-2.2f, 0, 1.5f), 3.2f);   // "서쪽 큰 바위"
        }

        static void Tent(Vector3 pos, float yRot, Color c)
        {
            var t = Group("Tent", Root, pos, yRot);
            Prim(PrimitiveType.Cube, t, new Vector3(0, 0.2f, 0), new Vector3(3.2f, 2.6f, 2.6f), c, true, new Vector3(45, 0, 0));
            Prim(PrimitiveType.Cube, t, new Vector3(0, 0.5f, 1.3f), new Vector3(0.9f, 1f, 0.1f), new Color(0.15f, 0.1f, 0.08f));
            Prim(PrimitiveType.Cylinder, t, new Vector3(0, 2.2f, 0), new Vector3(0.1f, 0.5f, 0.1f), Wood);
        }

        static void Campfire(Vector3 pos)
        {
            var g = Group("Campfire", Root, pos);
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Cube, g, new Vector3(0, 0.12f, 0), new Vector3(1.4f, 0.2f, 0.25f), DarkWood, false, new Vector3(0, i * 45, 0));
            Prim(PrimitiveType.Sphere, g, new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.9f, 0.7f), new Color(1f, 0.5f, 0.1f));
            Prim(PrimitiveType.Sphere, g, new Vector3(0, 0.6f, 0), new Vector3(0.4f, 0.6f, 0.4f), new Color(1f, 0.85f, 0.3f));
            var l = new GameObject("FireLight").AddComponent<Light>();
            l.transform.SetParent(g, false);
            l.transform.localPosition = new Vector3(0, 1.5f, 0);
            l.type = LightType.Point; l.range = 9f; l.intensity = 1.8f; l.color = new Color(1f, 0.55f, 0.2f);
            l.gameObject.AddComponent<Flicker>();
        }

        static void BuildGoblinCamp()
        {
            // 목책
            for (float x = -44; x <= 44; x += 1.4f)
            {
                if (Mathf.Abs(x) < 6f) continue;
                Prim(PrimitiveType.Cylinder, Root, new Vector3(x, 1.3f, 115f), new Vector3(0.5f, 1.3f, 0.5f), new Color(0.4f, 0.28f, 0.16f), true);
            }
            Sign3D(Root, new Vector3(0, 3.5f, 114.5f), "고블린 야영지 — 출입 위험!", new Color(1f, 0.5f, 0.4f), 0.28f);
            Color[] tentCols = { new Color(0.55f, 0.45f, 0.3f), new Color(0.45f, 0.35f, 0.25f), new Color(0.5f, 0.3f, 0.25f) };
            Vector3[] tents = { new Vector3(-16, 0, 124), new Vector3(15, 0, 126), new Vector3(-20, 0, 140), new Vector3(19, 0, 142), new Vector3(-8, 0, 147), new Vector3(10, 0, 148) };
            for (int i = 0; i < tents.Length; i++) Tent(tents[i], Random.Range(0, 360f), tentCols[i % 3]);
            Campfire(new Vector3(-4, 0, 130)); Campfire(new Vector3(7, 0, 138));
            for (int i = 0; i < 14; i++) Rock(new Vector3(Random.Range(-42f, 42f), 0, Random.Range(117f, 150f)), Random.Range(0.6f, 1.6f));
            for (int i = 0; i < 20; i++)
            {
                var p = new Vector3(Random.Range(28f, 46f) * (Random.value < 0.5f ? -1 : 1), 0, Random.Range(116f, 150f));
                Tree(p, Random.Range(0.8f, 1.2f), true);
            }
            // 해골 깃발
            foreach (var x in new[] { -6f, 6f })
            {
                Prim(PrimitiveType.Cylinder, Root, new Vector3(x, 2.5f, 116f), new Vector3(0.15f, 2.5f, 0.15f), DarkWood, true);
                Prim(PrimitiveType.Cube, Root, new Vector3(x + 0.8f * Mathf.Sign(x), 4.3f, 116f), new Vector3(1.5f, 1f, 0.05f), new Color(0.6f, 0.15f, 0.12f));
            }
        }

        static void BuildBossAltar()
        {
            var c = new Vector3(0, 0, 172);
            Prim(PrimitiveType.Cylinder, Root, c + new Vector3(0, 0.01f, 0), new Vector3(30, 0.02f, 30), new Color(0.3f, 0.26f, 0.28f));
            Prim(PrimitiveType.Cylinder, Root, c + new Vector3(0, 0.02f, 0), new Vector3(24, 0.02f, 24), new Color(0.4f, 0.34f, 0.36f));
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 15.5f;
                if (Mathf.Abs(p.x) < 4f && p.z < c.z) continue; // 입구
                float h = Random.Range(3.5f, 5.5f);
                Prim(PrimitiveType.Cube, Root, p + new Vector3(0, h / 2, 0), new Vector3(1.4f, h, 1.4f), new Color(0.45f, 0.42f, 0.46f), true,
                     new Vector3(0, a * Mathf.Rad2Deg, Random.Range(-4f, 4f)));
                Prim(PrimitiveType.Sphere, Root, p + new Vector3(0, h + 0.4f, 0), Vector3.one * 0.6f, new Color(0.8f, 0.2f, 0.25f));
            }
            // 왕좌
            var throne = Group("Throne", Root, c + new Vector3(0, 0, 10), 180);
            Prim(PrimitiveType.Cube, throne, new Vector3(0, 0.6f, 0), new Vector3(4, 1.2f, 3), new Color(0.35f, 0.3f, 0.3f), true);
            Prim(PrimitiveType.Cube, throne, new Vector3(0, 2.4f, -1.2f), new Vector3(3, 3.6f, 0.6f), new Color(0.5f, 0.2f, 0.2f), true);
            Prim(PrimitiveType.Sphere, throne, new Vector3(0, 4.4f, -1.2f), Vector3.one * 1f, UIFactory.Gold);
            Campfire(c + new Vector3(-9, 0, 6)); Campfire(c + new Vector3(9, 0, 6));
            for (int i = 0; i < 16; i++)
            {
                var p = new Vector3(Random.Range(20f, 46f) * (Random.value < 0.5f ? -1 : 1), 0, Random.Range(154f, 198f));
                Rock(p, Random.Range(1f, 2.5f));
            }
        }

        // ─────────────── 봉인의 문 (제단 ↔ 폐허) ───────────────
        static void BuildSealGate()
        {
            var stone = new Color(0.38f, 0.36f, 0.42f);
            const float z = 200f;
            // 좌우 성벽 (충돌 있음)
            Prim(PrimitiveType.Cube, Root, new Vector3(-26f, 2.2f, z), new Vector3(45f, 4.4f, 1.6f), stone, true);
            Prim(PrimitiveType.Cube, Root, new Vector3(26f, 2.2f, z), new Vector3(45f, 4.4f, 1.6f), stone, true);
            for (float x = -47f; x <= 47f; x += 3f)
            {
                if (Mathf.Abs(x) < 5f) continue;
                Prim(PrimitiveType.Cube, Root, new Vector3(x, 4.8f, z), new Vector3(1.4f, 0.8f, 1.8f), stone * 1.1f);
            }
            // 아치 기둥
            foreach (var sx in new[] { -4.4f, 4.4f })
            {
                Prim(PrimitiveType.Cube, Root, new Vector3(sx, 3.2f, z), new Vector3(1.8f, 6.4f, 2.2f), stone * 0.9f, true);
                Prim(PrimitiveType.Sphere, Root, new Vector3(sx, 6.8f, z), Vector3.one * 0.9f, new Color(0.7f, 0.35f, 1f));
            }
            Prim(PrimitiveType.Cube, Root, new Vector3(0, 6.6f, z), new Vector3(10.6f, 1f, 2.2f), stone * 0.9f);
            Sign3D(Root, new Vector3(0, 7.6f, z - 1.2f), "잊혀진 폐허", new Color(0.85f, 0.7f, 1f), 0.32f);

            // 봉인 결계 (그락 처치 전까지 통행 불가)
            sealBarrier = Prim(PrimitiveType.Cube, Root, new Vector3(0, 3f, z), new Vector3(7f, 6f, 0.4f), new Color(0.55f, 0.25f, 0.85f), true);
            sealBarrier.name = "SealBarrier";
            var sl = new GameObject("SealLight").AddComponent<Light>();
            sl.transform.SetParent(sealBarrier.transform, false);
            sl.transform.localPosition = new Vector3(0, 0, -2f);
            sl.type = LightType.Point; sl.range = 8f; sl.intensity = 2f; sl.color = new Color(0.7f, 0.35f, 1f);
            sl.gameObject.AddComponent<Flicker>();
            sealRunes = Group("SealRunes", Root, new Vector3(0, 0, z - 1.4f));
            for (int i = 0; i < 5; i++)
            {
                var r = Prim(PrimitiveType.Cube, sealRunes, new Vector3(-2.4f + i * 1.2f, 1.2f + (i % 2) * 1.6f, 0), new Vector3(0.5f, 0.5f, 0.1f),
                             new Color(1f, 0.85f, 1f), false, new Vector3(0, 0, 45));
                r.AddComponent<Bobber>();
            }
            Sign3D(Root, new Vector3(0, 1.2f, z - 3.5f), "고블린 왕의 힘이 봉인을 지탱하고 있다", new Color(0.9f, 0.8f, 1f), 0.18f);
        }

        // ─────────────── 잊혀진 폐허 (후반 지역) ───────────────
        static void BuildRuins()
        {
            Slab(new Vector3(0, 0, 237.5f), new Vector3(110, 1, 69), new Color(0.3f, 0.31f, 0.36f));
            var stone = new Color(0.52f, 0.5f, 0.56f);
            var moss = new Color(0.3f, 0.42f, 0.33f);

            // 부서진 돌길
            for (float z = 202; z < 238; z += 3.2f)
            {
                if (Random.value < 0.2f) continue;
                Prim(PrimitiveType.Cube, Root, new Vector3(Random.Range(-0.4f, 0.4f), 0.01f, z), new Vector3(4.2f, 0.03f, 2.8f), new Color(0.45f, 0.44f, 0.48f),
                     false, new Vector3(0, Random.Range(-6f, 6f), 0));
            }
            // 길가의 부러진 기둥 행렬
            for (float z = 206; z < 238; z += 7f)
            {
                foreach (var sx in new[] { -7f, 7f })
                {
                    float h = Random.value < 0.35f ? Random.Range(1f, 2.5f) : Random.Range(4f, 6f);
                    Prim(PrimitiveType.Cylinder, Root, new Vector3(sx, h / 2f, z), new Vector3(1.1f, h / 2f, 1.1f), stone, true);
                    if (h < 3f) Prim(PrimitiveType.Cylinder, Root, new Vector3(sx + 1.8f, 0.4f, z + 0.5f), new Vector3(1f, 1.4f, 1f), stone * 0.9f, true, new Vector3(0, Random.Range(0, 360), 90));
                    else Prim(PrimitiveType.Cube, Root, new Vector3(sx, h + 0.2f, z), new Vector3(1.6f, 0.4f, 1.6f), stone * 1.05f);
                }
            }
            // 묘지 (서쪽)
            for (int i = 0; i < 16; i++)
            {
                var p = new Vector3(-28f + (i % 4) * 4f + Random.Range(-0.5f, 0.5f), 0, 208f + (i / 4) * 5f + Random.Range(-0.5f, 0.5f));
                Prim(PrimitiveType.Cube, Root, p + new Vector3(0, 0.6f, 0), new Vector3(1f, 1.2f, 0.3f), stone * Random.Range(0.8f, 1f), true,
                     new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-12f, 12f)));
                Prim(PrimitiveType.Cube, Root, p + new Vector3(0, 0.03f, -1f), new Vector3(1f, 0.06f, 1.6f), new Color(0.28f, 0.25f, 0.22f));
            }
            // 무너진 신전 벽 (동쪽)
            for (int i = 0; i < 6; i++)
            {
                float h = Random.Range(1.5f, 4.5f);
                Prim(PrimitiveType.Cube, Root, new Vector3(22f + i * 3.2f, h / 2f, 212f + (i % 2) * 0.4f), new Vector3(3f, h, 1f), i % 3 == 0 ? moss : stone, true);
            }
            for (int i = 0; i < 3; i++)
            {
                float h = Random.Range(2f, 4f);
                Prim(PrimitiveType.Cube, Root, new Vector3(21f, h / 2f, 216f + i * 3.2f), new Vector3(1f, h, 3f), stone, true);
            }
            // 유령불
            Vector3[] wisps = { new Vector3(-24, 1.6f, 214), new Vector3(-16, 1.4f, 224), new Vector3(26, 1.8f, 218), new Vector3(16, 1.5f, 230), new Vector3(-10, 1.7f, 236) };
            foreach (var w in wisps)
            {
                var g = Group("Wisp", Root, w);
                Prim(PrimitiveType.Sphere, g, Vector3.zero, Vector3.one * 0.35f, new Color(0.6f, 1f, 0.95f)).AddComponent<Bobber>();
                var l = new GameObject("Light").AddComponent<Light>();
                l.transform.SetParent(g, false);
                l.type = LightType.Point; l.range = 7f; l.intensity = 1.4f; l.color = new Color(0.45f, 0.95f, 0.9f);
                l.gameObject.AddComponent<Flicker>();
            }
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector3(Random.Range(-46f, 46f), 0, Random.Range(203f, 262f));
                if (Mathf.Abs(p.x) < 12f) continue;
                if (p.x < -12f && p.x > -32f && p.z < 228f) continue; // 묘지
                if (Random.value < 0.6f) DeadTree(p, Random.Range(0.8f, 1.3f));
                else Rock(p, Random.Range(0.8f, 2f));
            }

            // 망령 군주의 의식장
            var c = new Vector3(0, 0, 248);
            Prim(PrimitiveType.Cylinder, Root, c + new Vector3(0, 0.01f, 0), new Vector3(32, 0.02f, 32), new Color(0.2f, 0.17f, 0.25f));
            Prim(PrimitiveType.Cylinder, Root, c + new Vector3(0, 0.02f, 0), new Vector3(26, 0.02f, 26), new Color(0.35f, 0.2f, 0.45f));
            Prim(PrimitiveType.Cylinder, Root, c + new Vector3(0, 0.03f, 0), new Vector3(24.5f, 0.02f, 24.5f), new Color(0.24f, 0.18f, 0.3f));
            for (int i = 0; i < 12; i++)   // 룬 문양
            {
                float a = i * 30f;
                Prim(PrimitiveType.Cube, Root, c + Quaternion.Euler(0, a, 0) * new Vector3(0, 0.04f, 10.5f), new Vector3(0.5f, 0.02f, 1.6f), new Color(0.8f, 0.5f, 1f),
                     false, new Vector3(0, a, 0));
            }
            for (int i = 0; i < 8; i++)    // 오벨리스크
            {
                float a = i / 8f * Mathf.PI * 2f + Mathf.PI / 8f;
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 16f;
                if (Mathf.Abs(p.x) < 5f && p.z < c.z) continue; // 입구
                Prim(PrimitiveType.Cube, Root, p + new Vector3(0, 2.8f, 0), new Vector3(1.2f, 5.6f, 1.2f), new Color(0.18f, 0.16f, 0.22f), true, new Vector3(0, a * Mathf.Rad2Deg, 0));
                Prim(PrimitiveType.Cube, Root, p + new Vector3(0, 6f, 0), new Vector3(0.7f, 0.7f, 0.7f), new Color(0.75f, 0.35f, 1f), false, new Vector3(45, 45, 0)).AddComponent<Bobber>();
            }
            // 뼈의 왕좌
            var throne = Group("BoneThrone", Root, c + new Vector3(0, 0, 11), 180);
            Prim(PrimitiveType.Cube, throne, new Vector3(0, 0.7f, 0), new Vector3(4.5f, 1.4f, 3f), new Color(0.3f, 0.26f, 0.32f), true);
            Prim(PrimitiveType.Cube, throne, new Vector3(0, 3f, -1.2f), new Vector3(3.4f, 4.4f, 0.6f), new Color(0.25f, 0.12f, 0.3f), true);
            for (int i = 0; i < 5; i++)
                Prim(PrimitiveType.Cylinder, throne, new Vector3(-1.6f + i * 0.8f, 5.6f, -1.2f), new Vector3(0.18f, 0.6f + (i == 2 ? 0.4f : 0f), 0.18f), new Color(0.88f, 0.85f, 0.78f));
            Prim(PrimitiveType.Sphere, throne, new Vector3(0, 4.2f, -0.85f), Vector3.one * 0.9f, new Color(0.88f, 0.85f, 0.78f));
            var tl = new GameObject("ThroneLight").AddComponent<Light>();
            tl.transform.SetParent(throne, false);
            tl.transform.localPosition = new Vector3(0, 3f, 2f);
            tl.type = LightType.Point; tl.range = 14f; tl.intensity = 1.8f; tl.color = new Color(0.6f, 0.3f, 1f);
            tl.gameObject.AddComponent<Flicker>();
        }

        public static void DeadTree(Vector3 pos, float scale = 1f)
        {
            var t = Group("DeadTree", Root, pos, Random.Range(0, 360f));
            t.localScale = Vector3.one * scale;
            var bark = new Color(0.25f, 0.22f, 0.24f);
            Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.4f, 0), new Vector3(0.35f, 1.4f, 0.35f), bark, true);
            Prim(PrimitiveType.Cube, t, new Vector3(0.45f, 2.3f, 0), new Vector3(0.14f, 1.3f, 0.14f), bark, false, new Vector3(0, 0, -40));
            Prim(PrimitiveType.Cube, t, new Vector3(-0.4f, 2.7f, 0.1f), new Vector3(0.12f, 1.1f, 0.12f), bark, false, new Vector3(20, 0, 35));
            Prim(PrimitiveType.Cube, t, new Vector3(0.1f, 3.1f, -0.3f), new Vector3(0.1f, 0.9f, 0.1f), bark, false, new Vector3(-30, 0, 10));
        }

        static void BuildBorders()
        {
            // 보이지 않는 벽
            InvisibleWall(new Vector3(0, 3, -34), new Vector3(110, 6, 1));
            InvisibleWall(new Vector3(0, 3, 266), new Vector3(110, 6, 1));
            InvisibleWall(new Vector3(-48, 3, 115), new Vector3(1, 6, 304));
            InvisibleWall(new Vector3(48, 3, 115), new Vector3(1, 6, 304));
            // 경계 숲
            for (float z = -30; z < 265; z += 5f)
            {
                Tree(new Vector3(-50 + Random.Range(0f, 4f), 0, z + Random.Range(-1f, 1f)), Random.Range(1.1f, 1.5f), Random.value < 0.5f);
                Tree(new Vector3(50 - Random.Range(0f, 4f), 0, z + Random.Range(-1f, 1f)), Random.Range(1.1f, 1.5f), Random.value < 0.5f);
            }
            for (float x = -46; x < 46; x += 5f)
            {
                Tree(new Vector3(x, 0, -36 + Random.Range(0, 3f)), Random.Range(1.1f, 1.5f), Random.value < 0.4f);
                DeadTree(new Vector3(x, 0, 268 + Random.Range(0, 3f)), Random.Range(1.1f, 1.5f));
            }
        }

        static void InvisibleWall(Vector3 pos, Vector3 size)
        {
            var go = new GameObject("InvisibleWall");
            go.transform.SetParent(Root, false);
            go.transform.position = pos;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
        }
    }

}
