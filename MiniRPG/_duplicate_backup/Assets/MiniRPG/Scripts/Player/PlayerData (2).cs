using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;
        public ItemStack() { }
        public ItemStack(string id, int count) { this.id = id; this.count = count; }
        public ItemDef Def { get { return ItemDB.Get(id); } }
    }

    /// <summary>플레이어 소지품 (장비 포함)</summary>
    public class Inventory
    {
        public List<ItemStack> items = new List<ItemStack>();
        public string weaponId = "wood_sword";
        public string armorId = "cloth";
        public event Action Changed;

        public ItemDef Weapon { get { return ItemDB.Get(weaponId); } }
        public ItemDef Armor { get { return ItemDB.Get(armorId); } }

        public int Count(string id)
        {
            for (int i = 0; i < items.Count; i++) if (items[i].id == id) return items[i].count;
            return 0;
        }

        public void Add(string id, int n = 1)
        {
            if (ItemDB.Get(id) == null || n <= 0) return;
            for (int i = 0; i < items.Count; i++)
                if (items[i].id == id) { items[i].count += n; Notify(); return; }
            items.Add(new ItemStack(id, n));
            Notify();
        }

        public bool Remove(string id, int n = 1)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id != id) continue;
                if (items[i].count < n) return false;
                items[i].count -= n;
                if (items[i].count <= 0) items.RemoveAt(i);
                Notify();
                return true;
            }
            return false;
        }

        /// <summary>장비 교체: 기존 장비는 가방으로</summary>
        public void Equip(string id)
        {
            var d = ItemDB.Get(id);
            if (d == null || Count(id) <= 0) return;
            if (d.type == ItemType.Weapon)
            {
                Remove(id);
                if (!string.IsNullOrEmpty(weaponId)) Add(weaponId);
                weaponId = id;
            }
            else if (d.type == ItemType.Armor)
            {
                Remove(id);
                if (!string.IsNullOrEmpty(armorId)) Add(armorId);
                armorId = id;
            }
            Notify();
        }

        public bool Owns(string id) { return Count(id) > 0 || weaponId == id || armorId == id; }

        public void Reset()
        {
            items.Clear();
            weaponId = "wood_sword";
            armorId = "cloth";
            Add("potion_s", 3);
            Add("return_scroll", 1);
        }

        public void Notify() { if (Changed != null) Changed(); }
    }

    /// <summary>레벨/능력치/골드</summary>
    public class PlayerStats
    {
        public int level = 1;
        public int exp;
        public int hp, mp;
        public int gold;
        public int baseMaxHp = 120, baseMaxMp = 40, baseAtk = 8, baseDef = 2;
        public int kills;
        public Inventory inv;

        public event Action Changed;
        public event Action<int> LeveledUp;

        public PlayerStats(Inventory inv) { this.inv = inv; }

        public int MaxHP { get { return baseMaxHp; } }
        public int MaxMP { get { return baseMaxMp; } }
        public int Atk { get { var w = inv.Weapon; return baseAtk + (w != null ? w.atk : 0); } }
        public int Def { get { var a = inv.Armor; return baseDef + (a != null ? a.def : 0); } }
        public int ExpToNext { get { return Mathf.RoundToInt(45f * Mathf.Pow(level, 1.55f)); } }
        public bool Dead { get { return hp <= 0; } }

        public void Reset()
        {
            level = 1; exp = 0; gold = 150; kills = 0;
            baseMaxHp = 120; baseMaxMp = 40; baseAtk = 8; baseDef = 2;
            hp = MaxHP; mp = MaxMP;
            Notify();
        }

        public void AddExp(int amount)
        {
            exp += amount;
            while (exp >= ExpToNext)
            {
                exp -= ExpToNext;
                level++;
                baseMaxHp += 22;
                baseMaxMp += 8;
                baseAtk += 3;
                baseDef += 1;
                hp = MaxHP;
                mp = MaxMP;
                if (LeveledUp != null) LeveledUp(level);
            }
            Notify();
        }

        public void AddGold(int amount) { gold = Mathf.Max(0, gold + amount); Notify(); }
        public bool Spend(int amount) { if (gold < amount) return false; gold -= amount; Notify(); return true; }
        public void Heal(int amount) { hp = Mathf.Min(MaxHP, hp + amount); Notify(); }
        public void RestoreMP(int amount) { mp = Mathf.Min(MaxMP, mp + amount); Notify(); }
        public bool UseMP(int amount) { if (mp < amount) return false; mp -= amount; Notify(); return true; }

        public int TakeDamage(int rawAtk)
        {
            int dmg = CombatMath.Damage(rawAtk, Def);
            hp = Mathf.Max(0, hp - dmg);
            Notify();
            return dmg;
        }

        public void Notify() { if (Changed != null) Changed(); }
    }

    public static class CombatMath
    {
        public static int Damage(float atk, int def)
        {
            float v = atk * UnityEngine.Random.Range(0.9f, 1.1f) - def * 0.5f;
            return Mathf.Max(1, Mathf.RoundToInt(v));
        }
    }
}
