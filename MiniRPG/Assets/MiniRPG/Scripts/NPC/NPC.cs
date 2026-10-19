using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    public class NPC : MonoBehaviour
    {
        public static readonly List<NPC> All = new List<NPC>();

        public string id;
        public string displayName;
        public string role;           // 이름 아래 작은 글씨 (예: 촌장)
        public bool wanders;
        Transform model;
        Vector3 home, wanderTarget;
        float wanderTimer, bobSeed;
        Quaternion baseRot;

        RectTransform nameTag;
        Text nameText, markerText;

        public struct Look
        {
            public Color body, skin, hair, accent;
            public string hat;  // "none","hood","crown","cap","helmet","tophat","bandana"
            public float scale;
            public bool beard, apron;
        }

        public static NPC Create(string id, string name, string role, Vector3 pos, float yRot, Look look, bool wanders = false)
        {
            var go = new GameObject("NPC_" + id);
            go.transform.position = pos;
            var n = go.AddComponent<NPC>();
            n.id = id; n.displayName = name; n.role = role; n.wanders = wanders;
            n.home = pos; n.wanderTarget = pos;
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.9f * look.scale; col.radius = 0.4f; col.center = new Vector3(0, 0.95f * look.scale, 0);
            n.BuildModel(look, yRot);
            WorldBuilder.MapIcon(go.transform, new Color(0.3f, 0.95f, 1f), 1.9f);
            All.Add(n);
            return n;
        }

        void BuildModel(Look L, float yRot)
        {
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            model.localEulerAngles = new Vector3(0, yRot, 0);
            model.localScale = Vector3.one * L.scale;
            baseRot = model.rotation;
            bobSeed = Random.value * 10f;

            WorldBuilder.Prim(PrimitiveType.Capsule, model, new Vector3(0, 0.85f, 0), new Vector3(0.8f, 0.6f, 0.65f), L.body);
            if (L.apron) WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.75f, 0.3f), new Vector3(0.6f, 0.8f, 0.06f), L.accent);
            else WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.95f, 0.01f), new Vector3(0.82f, 0.12f, 0.67f), L.accent); // 허리띠
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.62f, 0), Vector3.one * 0.55f, L.skin);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(-0.1f, 1.65f, 0.25f), Vector3.one * 0.08f, Color.black);
            WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0.1f, 1.65f, 0.25f), Vector3.one * 0.08f, Color.black);
            if (L.beard) WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.45f, 0.18f), new Vector3(0.45f, 0.35f, 0.3f), L.hair);

            switch (L.hat)
            {
                case "hood":
                    WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.72f, -0.04f), new Vector3(0.66f, 0.5f, 0.66f), L.accent);
                    break;
                case "crown":
                    WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.76f, -0.05f), new Vector3(0.58f, 0.3f, 0.58f), L.hair);
                    WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 1.95f, 0), new Vector3(0.4f, 0.08f, 0.4f), UIFactory.Gold);
                    break;
                case "cap":
                    WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.8f, 0), new Vector3(0.58f, 0.3f, 0.58f), L.accent);
                    WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 1.78f, 0.28f), new Vector3(0.4f, 0.05f, 0.25f), L.accent);
                    break;
                case "helmet":
                    WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.75f, 0), new Vector3(0.62f, 0.45f, 0.62f), new Color(0.65f, 0.67f, 0.72f));
                    WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0, 2.02f, 0), new Vector3(0.06f, 0.18f, 0.4f), L.accent);
                    // 창
                    WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0.5f, 1.2f, 0.15f), new Vector3(0.07f, 1.2f, 0.07f), new Color(0.45f, 0.3f, 0.18f));
                    WorldBuilder.Prim(PrimitiveType.Cube, model, new Vector3(0.5f, 2.45f, 0.15f), new Vector3(0.12f, 0.3f, 0.04f), new Color(0.75f, 0.78f, 0.82f), false, new Vector3(0, 0, 45));
                    break;
                case "tophat":
                    WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 1.88f, 0), new Vector3(0.62f, 0.02f, 0.62f), new Color(0.1f, 0.08f, 0.12f));
                    WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 2.1f, 0), new Vector3(0.4f, 0.22f, 0.4f), new Color(0.12f, 0.1f, 0.15f));
                    WorldBuilder.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 1.95f, 0), new Vector3(0.41f, 0.04f, 0.41f), L.accent);
                    break;
                case "bandana":
                    WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.74f, -0.02f), new Vector3(0.6f, 0.32f, 0.6f), L.accent);
                    break;
                default:
                    WorldBuilder.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.75f, -0.05f), new Vector3(0.6f, 0.35f, 0.6f), L.hair);
                    break;
            }
        }

        void Start()
        {
            var layer = GameManager.I.Fx.worldLayer;
            nameTag = UIFactory.Rect("NpcTag_" + id, layer);
            nameTag.sizeDelta = new Vector2(220, 80);
            markerText = UIFactory.Label(nameTag, "", 40, TextAnchor.MiddleCenter, UIFactory.Gold);
            markerText.fontStyle = FontStyle.Bold;
            UIFactory.Place(markerText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 4), new Vector2(60, 44));
            markerText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);
            nameText = UIFactory.Label(nameTag, "", 17, TextAnchor.LowerCenter, new Color(0.7f, 1f, 1f));
            UIFactory.Place(nameText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(220, 40));
            nameText.text = "<size=13><color=#B8C4D6>" + role + "</color></size>\n" + displayName;
        }

        public void Interact()
        {
            FacePlayer();
            NpcDialogues.Open(this);
        }

        public void FacePlayer()
        {
            var p = GameManager.I.Player;
            if (p == null) return;
            var d = p.transform.position - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f) model.rotation = Quaternion.LookRotation(d);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null) return;
            float dt = Time.deltaTime;
            float dist = Vector3.Distance(gm.Player.transform.position, transform.position);

            // 숨쉬기
            model.localPosition = new Vector3(0, Mathf.Sin(Time.time * 2f + bobSeed) * 0.03f, 0);

            if (dist < 5f)
            {
                var d = gm.Player.transform.position - transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f) model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(d), 4f * dt);
            }
            else if (wanders && gm.State == GameState.Playing)
            {
                wanderTimer -= dt;
                var to = wanderTarget - transform.position; to.y = 0;
                if (to.magnitude > 0.3f)
                {
                    transform.position += to.normalized * 1.6f * dt;
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(to), 6f * dt);
                    model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.08f, 0);
                }
                else if (wanderTimer <= 0f)
                {
                    wanderTarget = home + new Vector3(Random.Range(-6f, 6f), 0, Random.Range(-5f, 5f));
                    wanderTimer = Random.Range(2f, 5f);
                }
            }
            else
            {
                model.rotation = Quaternion.Slerp(model.rotation, baseRot, 2f * dt);
            }
        }

        void LateUpdate()
        {
            if (nameTag == null) return;
            var gm = GameManager.I;
            var cam = Camera.main;
            bool show = cam != null && gm.State != GameState.Title && gm.Player != null &&
                        Vector3.Distance(gm.Player.transform.position, transform.position) < 22f;
            if (show)
            {
                var sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.35f);
                Vector2 lp;
                if (sp.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(gm.Fx.worldLayer, sp, null, out lp))
                    nameTag.anchoredPosition = lp + new Vector2(0, 30);
                else show = false;
            }
            if (nameTag.gameObject.activeSelf != show) nameTag.gameObject.SetActive(show);
            if (show)
            {
                string m = gm.Quests.MarkerFor(id);
                markerText.text = m;
                markerText.color = m == "?" ? new Color(0.5f, 1f, 0.55f) : UIFactory.Gold;
                markerText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 4f) * 0.08f);
            }
        }

        void OnDestroy() { All.Remove(this); }
    }
}
