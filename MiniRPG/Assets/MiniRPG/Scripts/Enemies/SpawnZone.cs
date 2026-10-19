using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>일정 구역에 몬스터를 유지/리스폰</summary>
    public class SpawnZone : MonoBehaviour
    {
        public string enemyId;
        public float radius = 10f;
        public int max = 5;
        public float respawnTime = 12f;
        public bool respawn = true;
        public bool active = true;   // false면 리스폰 중지 (봉인된 지역 등)
        readonly List<Enemy> alive = new List<Enemy>();
        float timer;

        public static SpawnZone Create(string enemyId, Vector3 center, float radius, int max, float respawnTime, bool respawn = true)
        {
            var go = new GameObject("Spawn_" + enemyId);
            go.transform.position = center;
            var z = go.AddComponent<SpawnZone>();
            z.enemyId = enemyId; z.radius = radius; z.max = max; z.respawnTime = respawnTime; z.respawn = respawn;
            return z;
        }

        public void FillAll()
        {
            ClearAll();
            for (int i = 0; i < max; i++) SpawnOne(false);
        }

        public void ClearAll()
        {
            foreach (var e in alive) if (e != null) Destroy(e.gameObject);
            alive.Clear();
        }

        public void OnDeath(Enemy e) { alive.Remove(e); timer = respawnTime; }

        void Update()
        {
            if (!respawn || !active || GameManager.I == null || GameManager.I.State == GameState.Title) return;
            alive.RemoveAll(x => x == null);
            if (alive.Count >= max) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) { if (SpawnOne(true)) timer = respawnTime * 0.5f; }
        }

        bool SpawnOne(bool avoidPlayer)
        {
            var p = GameManager.I.Player;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var r = Random.insideUnitCircle * radius;
                var pos = transform.position + new Vector3(r.x, 0.1f, r.y);
                if (avoidPlayer && p != null && Vector3.Distance(pos, p.transform.position) < 12f) continue;
                alive.Add(Enemy.Spawn(enemyId, pos, this));
                return true;
            }
            return false;
        }
    }
}
