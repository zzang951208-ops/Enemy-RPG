using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    public enum ItemType { Consumable, Weapon, Armor, Material, Special }

    public class ItemDef
    {
        public string id, name, desc;
        public ItemType type;
        public int price;          // 구매가 (판매가는 절반)
        public int hp, mp, atk, def;
        public string effect;      // 특수 효과 (return = 귀환)
        public Color color = Color.white;
        public bool sellable = true;

        public int SellPrice { get { return Mathf.Max(1, price / 2); } }

        public string TypeName
        {
            get
            {
                switch (type)
                {
                    case ItemType.Consumable: return "소모품";
                    case ItemType.Weapon: return "무기";
                    case ItemType.Armor: return "방어구";
                    case ItemType.Material: return "재료";
                    default: return "특수";
                }
            }
        }

        public string StatText
        {
            get
            {
                var s = new List<string>();
                if (atk > 0) s.Add("공격력 +" + atk);
                if (def > 0) s.Add("방어력 +" + def);
                if (hp > 0) s.Add("HP " + hp + " 회복");
                if (mp > 0) s.Add("MP " + mp + " 회복");
                return string.Join(" · ", s.ToArray());
            }
        }
    }

    public static class ItemDB
    {
        static readonly Dictionary<string, ItemDef> all = new Dictionary<string, ItemDef>();
        public static IEnumerable<ItemDef> All { get { return all.Values; } }

        static ItemDB()
        {
            // 소모품
            Add(new ItemDef { id = "potion_s", name = "체력 포션(소)", type = ItemType.Consumable, price = 30, hp = 60, color = new Color(1f, 0.35f, 0.4f), desc = "붉은 약초를 달인 물약." });
            Add(new ItemDef { id = "potion_l", name = "체력 포션(대)", type = ItemType.Consumable, price = 90, hp = 180, color = new Color(0.9f, 0.1f, 0.2f), desc = "진하게 농축한 고급 물약." });
            Add(new ItemDef { id = "mana_potion", name = "마나 포션", type = ItemType.Consumable, price = 40, mp = 40, color = new Color(0.35f, 0.5f, 1f), desc = "푸른 빛이 은은하게 도는 물약." });
            Add(new ItemDef { id = "return_scroll", name = "귀환 주문서", type = ItemType.Consumable, price = 50, effect = "return", color = new Color(0.9f, 0.85f, 0.6f), desc = "읽는 즉시 새벽빛 마을로 돌아간다." });
            Add(new ItemDef { id = "elixir", name = "엘릭서", type = ItemType.Consumable, price = 260, hp = 400, mp = 120, color = new Color(1f, 0.85f, 0.35f), desc = "학자 엘라라가 고대 문헌대로 조제한 만능 영약." });
            // 무기
            Add(new ItemDef { id = "wood_sword", name = "낡은 목검", type = ItemType.Weapon, price = 10, atk = 2, color = new Color(0.6f, 0.45f, 0.3f), desc = "모험가 입문용 목검." });
            Add(new ItemDef { id = "iron_sword", name = "철검", type = ItemType.Weapon, price = 150, atk = 7, color = new Color(0.75f, 0.78f, 0.82f), desc = "대장장이 브론의 기본 작품." });
            Add(new ItemDef { id = "steel_blade", name = "강철 대검", type = ItemType.Weapon, price = 480, atk = 14, color = new Color(0.55f, 0.65f, 0.8f), desc = "묵직하고 날카로운 강철 대검." });
            Add(new ItemDef { id = "dragon_sword", name = "용살자의 검", type = ItemType.Weapon, price = 2500, atk = 26, color = new Color(1f, 0.45f, 0.2f), desc = "전설 속 용을 베었다는 붉은 검." });
            Add(new ItemDef { id = "rune_blade", name = "룬 새김 대검", type = ItemType.Weapon, price = 4200, atk = 38, color = new Color(0.45f, 0.9f, 1f), desc = "폐허의 룬 문자를 새겨 넣은 대검. 망령을 벤다." });
            // 방어구
            Add(new ItemDef { id = "cloth", name = "여행자의 옷", type = ItemType.Armor, price = 10, def = 1, color = new Color(0.3f, 0.45f, 0.75f), desc = "먼 길을 함께 한 낡은 옷." });
            Add(new ItemDef { id = "leather_armor", name = "가죽 갑옷", type = ItemType.Armor, price = 130, def = 4, color = new Color(0.55f, 0.36f, 0.2f), desc = "질긴 가죽으로 만든 갑옷." });
            Add(new ItemDef { id = "chain_armor", name = "사슬 갑옷", type = ItemType.Armor, price = 400, def = 9, color = new Color(0.55f, 0.58f, 0.62f), desc = "촘촘히 엮은 쇠사슬 갑옷." });
            Add(new ItemDef { id = "knight_armor", name = "기사의 판금갑옷", type = ItemType.Armor, price = 1100, def = 16, color = new Color(0.85f, 0.85f, 0.92f), desc = "왕국 기사단의 정식 갑옷." });
            Add(new ItemDef { id = "rune_armor", name = "룬 판금갑옷", type = ItemType.Armor, price = 3200, def = 25, color = new Color(0.55f, 0.75f, 0.95f), desc = "고대 룬이 새겨져 저주를 튕겨내는 갑옷." });
            // 재료
            Add(new ItemDef { id = "jelly", name = "슬라임 젤리", type = ItemType.Material, price = 10, color = new Color(0.4f, 0.9f, 0.5f), desc = "말랑말랑한 젤리. 상점에 팔 수 있다." });
            Add(new ItemDef { id = "wolf_pelt", name = "늑대 가죽", type = ItemType.Material, price = 24, color = new Color(0.55f, 0.55f, 0.6f), desc = "대장장이가 탐내는 두꺼운 가죽." });
            Add(new ItemDef { id = "goblin_ear", name = "고블린 귀", type = ItemType.Material, price = 40, color = new Color(0.5f, 0.6f, 0.3f), desc = "토벌의 증표." });
            Add(new ItemDef { id = "herb", name = "달빛 약초", type = ItemType.Material, price = 16, color = new Color(0.7f, 1f, 0.8f), desc = "초원에서 자라는 빛나는 약초." });
            Add(new ItemDef { id = "bone_dust", name = "뼛가루", type = ItemType.Material, price = 36, color = new Color(0.9f, 0.88f, 0.8f), desc = "해골 병사가 부서지며 남긴 가루." });
            Add(new ItemDef { id = "ecto", name = "망령의 정수", type = ItemType.Material, price = 70, color = new Color(0.6f, 0.95f, 0.9f), desc = "차갑게 일렁이는 푸른 빛 덩어리." });
            // 특수
            Add(new ItemDef { id = "lucky_coin", name = "행운의 금화", type = ItemType.Special, price = 0, sellable = false, color = UIFactory.Gold, desc = "도박사 잭이 잃어버린 금화." });
            Add(new ItemDef { id = "king_crown", name = "고블린 왕의 왕관", type = ItemType.Special, price = 0, sellable = false, color = UIFactory.Gold, desc = "고블린 왕을 쓰러뜨린 증거." });
            Add(new ItemDef { id = "doll", name = "낡은 헝겊 인형", type = ItemType.Special, price = 0, sellable = false, color = new Color(0.95f, 0.6f, 0.7f), desc = "꼬마 피피가 숲에서 잃어버린 인형." });
            Add(new ItemDef { id = "lich_orb", name = "망령 군주의 보주", type = ItemType.Special, price = 0, sellable = false, color = new Color(0.7f, 0.4f, 1f), desc = "모르가스의 힘이 깃들어 있던 보주. 이제는 조용하다." });
        }

        static void Add(ItemDef d) { all[d.id] = d; }
        public static ItemDef Get(string id)
        {
            ItemDef d;
            return id != null && all.TryGetValue(id, out d) ? d : null;
        }
    }

    public class EnemyDef
    {
        public string id, name;
        public int hp, atk, def, exp, goldMin, goldMax;
        public float speed, attackRange, aggroRange, attackInterval;
        public string drop;
        public float dropChance;
        public bool isBoss;
        public bool ranged;   // 원거리 공격 (투사체)
    }

    public static class EnemyDB
    {
        static readonly Dictionary<string, EnemyDef> all = new Dictionary<string, EnemyDef>();

        static EnemyDB()
        {
            Add(new EnemyDef { id = "slime", name = "슬라임", hp = 32, atk = 7, def = 0, exp = 12, goldMin = 3, goldMax = 8, speed = 2.2f, attackRange = 1.5f, aggroRange = 8f, attackInterval = 1.5f, drop = "jelly", dropChance = 0.55f });
            Add(new EnemyDef { id = "wolf", name = "회색 늑대", hp = 80, atk = 15, def = 3, exp = 30, goldMin = 8, goldMax = 16, speed = 4.4f, attackRange = 1.9f, aggroRange = 11f, attackInterval = 1.2f, drop = "wolf_pelt", dropChance = 0.5f });
            Add(new EnemyDef { id = "goblin", name = "고블린 전사", hp = 140, atk = 24, def = 7, exp = 60, goldMin = 15, goldMax = 32, speed = 3.4f, attackRange = 2.1f, aggroRange = 12f, attackInterval = 1.4f, drop = "goblin_ear", dropChance = 0.45f });
            Add(new EnemyDef { id = "goblin_king", name = "고블린 왕 그락", hp = 1200, atk = 40, def = 12, exp = 800, goldMin = 600, goldMax = 600, speed = 3.1f, attackRange = 3.2f, aggroRange = 16f, attackInterval = 1.6f, drop = "king_crown", dropChance = 1f, isBoss = true });
            Add(new EnemyDef { id = "skeleton", name = "해골 병사", hp = 300, atk = 44, def = 14, exp = 140, goldMin = 30, goldMax = 55, speed = 3.2f, attackRange = 2.0f, aggroRange = 12f, attackInterval = 1.3f, drop = "bone_dust", dropChance = 0.5f });
            Add(new EnemyDef { id = "wraith", name = "울부짖는 망령", hp = 200, atk = 38, def = 8, exp = 130, goldMin = 35, goldMax = 60, speed = 3.0f, attackRange = 9f, aggroRange = 14f, attackInterval = 2.2f, drop = "ecto", dropChance = 0.45f, ranged = true });
            Add(new EnemyDef { id = "lich", name = "망령 군주 모르가스", hp = 3000, atk = 60, def = 20, exp = 2500, goldMin = 2000, goldMax = 2000, speed = 2.6f, attackRange = 3.0f, aggroRange = 17f, attackInterval = 1.8f, drop = "lich_orb", dropChance = 1f, isBoss = true });
        }

        static void Add(EnemyDef d) { all[d.id] = d; }
        public static EnemyDef Get(string id) { EnemyDef d; return all.TryGetValue(id, out d) ? d : null; }
    }
}
