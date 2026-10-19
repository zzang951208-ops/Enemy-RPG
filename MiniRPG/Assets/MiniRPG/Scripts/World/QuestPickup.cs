using UnityEngine;

namespace MiniRPG
{
    /// <summary>특정 퀘스트 진행 중에만 나타나는 줍기 아이템 (예: 피피의 인형)</summary>
    public class QuestPickup : MonoBehaviour
    {
        string itemId, questId;
        Transform visual;
        GameObject icon;
        const float PickRadius = 1.8f;

        public static QuestPickup Create(string itemId, string questId, Vector3 pos)
        {
            var go = new GameObject("QuestPickup_" + itemId);
            go.transform.SetParent(WorldBuilder.Root, false);
            go.transform.position = pos;
            var q = go.AddComponent<QuestPickup>();
            q.itemId = itemId; q.questId = questId;
            q.visual = new GameObject("Visual").transform;
            q.visual.SetParent(go.transform, false);
            var item = ItemDB.Get(itemId);
            var c = item != null ? item.color : Color.white;
            // 작은 헝겊 인형 모양
            WorldBuilder.Prim(PrimitiveType.Capsule, q.visual, new Vector3(0, 0.35f, 0), new Vector3(0.35f, 0.25f, 0.3f), c);
            WorldBuilder.Prim(PrimitiveType.Sphere, q.visual, new Vector3(0, 0.75f, 0), Vector3.one * 0.32f, new Color(1f, 0.9f, 0.8f));
            WorldBuilder.Prim(PrimitiveType.Sphere, q.visual, new Vector3(-0.14f, 0.92f, 0), Vector3.one * 0.14f, c);
            WorldBuilder.Prim(PrimitiveType.Sphere, q.visual, new Vector3(0.14f, 0.92f, 0), Vector3.one * 0.14f, c);
            var l = new GameObject("Sparkle").AddComponent<Light>();
            l.transform.SetParent(q.visual, false);
            l.transform.localPosition = new Vector3(0, 1.2f, 0);
            l.type = LightType.Point; l.range = 4f; l.intensity = 1.5f; l.color = new Color(1f, 0.7f, 0.85f);
            q.icon = WorldBuilder.MapIcon(go.transform, new Color(1f, 0.5f, 0.8f), 2.2f, true);
            q.SetVisible(false);
            return q;
        }

        void SetVisible(bool v)
        {
            if (visual.gameObject.activeSelf != v) visual.gameObject.SetActive(v);
            if (icon != null && icon.activeSelf != v) icon.SetActive(v);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null) return;
            bool want = gm.State != GameState.Title && gm.Quests.IsActive(questId) && !gm.Inv.Owns(itemId);
            SetVisible(want);
            if (!want || gm.State != GameState.Playing || gm.Player == null) return;

            float t = Time.time;
            visual.localPosition = new Vector3(0, Mathf.Sin(t * 2.5f) * 0.12f, 0);
            visual.localRotation = Quaternion.Euler(0, t * 60f, 0);

            var d = gm.Player.transform.position - transform.position; d.y = 0;
            if (d.sqrMagnitude < PickRadius * PickRadius)
            {
                gm.Inv.Add(itemId);
                SoundFX.Play(Sfx.Win, 0.7f);
                gm.Fx.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.7f, 0.85f), 14, 4f, 0.15f);
                var item = ItemDB.Get(itemId);
                gm.UI.Hud.Toast("찾았다! " + UIFactory.Col(item != null ? item.name : itemId, UIFactory.Gold));
                SetVisible(false);
            }
        }
    }
}
