using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>캐릭터 정보 + 가방</summary>
    public class InventoryUI : UIWindow
    {
        RectTransform list;
        Text statsText, equipText, detailText;
        Button useBtn;
        ScrollRect scroll;
        string selected;

        GameManager GM { get { return GameManager.I; } }

        public void Build(RectTransform parent)
        {
            RectTransform body; Text title;
            root = UIFactory.Window(parent, "캐릭터 & 인벤토리  (I)", new Vector2(1180, 720), Close, out body, out title);

            // 좌측: 능력치
            var left = UIFactory.Panel(body, "Stats", UIFactory.PanelMid);
            var lr = left.rectTransform;
            lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0.36f, 1);
            lr.offsetMin = Vector2.zero; lr.offsetMax = new Vector2(-8, 0);
            var h1 = UIFactory.Label(left.transform, "능력치", 24, TextAnchor.UpperLeft, UIFactory.Gold);
            UIFactory.Place(h1.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -16), new Vector2(300, 34));
            statsText = UIFactory.Label(left.transform, "", 20, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch(statsText.rectTransform, 20, 16, 56, 250);
            statsText.lineSpacing = 1.25f;
            var h2 = UIFactory.Label(left.transform, "장비", 24, TextAnchor.UpperLeft, UIFactory.Gold);
            UIFactory.Place(h2.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 240), new Vector2(300, 34));
            equipText = UIFactory.Label(left.transform, "", 20, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Place(equipText.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 200), new Vector2(370, 190));
            equipText.lineSpacing = 1.25f;

            // 가운데: 아이템 목록
            var mid = UIFactory.Rect("Items", body);
            mid.anchorMin = new Vector2(0.36f, 0); mid.anchorMax = new Vector2(0.72f, 1);
            mid.offsetMin = new Vector2(8, 0); mid.offsetMax = new Vector2(-8, 0);
            list = UIFactory.ScrollList(mid, out scroll, 5);
            UIFactory.Stretch((RectTransform)scroll.transform);

            // 우측: 상세
            var right = UIFactory.Panel(body, "Detail", UIFactory.PanelMid);
            var rr = right.rectTransform;
            rr.anchorMin = new Vector2(0.72f, 0); rr.anchorMax = new Vector2(1, 1);
            rr.offsetMin = new Vector2(8, 0); rr.offsetMax = Vector2.zero;
            detailText = UIFactory.Label(right.transform, "", 20, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch(detailText.rectTransform, 18, 18, 18, 100);
            detailText.lineSpacing = 1.2f;
            useBtn = UIFactory.Button(right.transform, "사용", UseSelected, new Color(0.25f, 0.55f, 0.32f), 24);
            UIFactory.Place((RectTransform)useBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(240, 60));

            root.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (GM.UI.Dialogue.IsOpen || GM.UI.Shop.IsOpen || GM.UI.Gamble.IsOpen) return;
            if (GM.UI.QuestLog.IsOpen) GM.UI.QuestLog.Close();
            ShowRoot();
            selected = null;
            Refresh();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }


        void Update()
        {
            // 열려 있는 동안 HP/MP 변화 반영 (가벼운 텍스트 갱신)
            if (IsOpen && Time.frameCount % 15 == 0) RefreshStats();
        }

        void Refresh()
        {
            RefreshStats();
            UIFactory.Clear(list);
            var inv = GM.Inv;
            if (inv.items.Count == 0)
            {
                var e = UIFactory.Label(list, "가방이 비어 있습니다.", 20, TextAnchor.MiddleCenter, UIFactory.TextDim);
                UIFactory.Size(e, -1, 80);
            }
            bool found = false;
            foreach (var s in inv.items) if (s.id == selected) found = true;
            if (!found) selected = inv.items.Count > 0 ? inv.items[0].id : null;

            foreach (var s in inv.items)
            {
                var d = s.Def;
                if (d == null) continue;
                string id = s.id;
                var btn = UIFactory.Button(list, "", () => { selected = id; Refresh(); }, id == selected ? new Color(0.3f, 0.4f, 0.62f) : UIFactory.PanelLight);
                UIFactory.Size(btn, -1, 56);
                UIFactory.ButtonLabel(btn).gameObject.SetActive(false);
                var icon = UIFactory.Panel(btn.transform, "Icon", d.color);
                UIFactory.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(36, 36));
                var n = UIFactory.Label(btn.transform, d.name, 20, TextAnchor.MiddleLeft, UIFactory.TextMain);
                UIFactory.Stretch(n.rectTransform, 58, 70, 0, 0);
                var c = UIFactory.Label(btn.transform, "x" + s.count, 20, TextAnchor.MiddleRight, UIFactory.Gold);
                UIFactory.Stretch(c.rectTransform, 200, 12, 0, 0);
            }
            ShowDetail();
        }

        void RefreshStats()
        {
            var s = GM.Stats; var inv = GM.Inv;
            var sb = new StringBuilder();
            sb.Append("레벨  ").Append(UIFactory.Col(s.level.ToString(), UIFactory.Gold)).Append('\n');
            sb.Append("경험치  ").Append(s.exp).Append(" / ").Append(s.ExpToNext).Append('\n');
            sb.Append("체력  ").Append(s.hp).Append(" / ").Append(s.MaxHP).Append('\n');
            sb.Append("마나  ").Append(s.mp).Append(" / ").Append(s.MaxMP).Append('\n');
            sb.Append("공격력  ").Append(UIFactory.Col(s.Atk.ToString(), UIFactory.Gold));
            if (inv.Weapon != null) sb.Append(UIFactory.Col("  (기본 " + s.baseAtk + " + 무기 " + inv.Weapon.atk + ")", UIFactory.TextDim));
            sb.Append('\n');
            sb.Append("방어력  ").Append(UIFactory.Col(s.Def.ToString(), UIFactory.Gold));
            if (inv.Armor != null) sb.Append(UIFactory.Col("  (기본 " + s.baseDef + " + 방어구 " + inv.Armor.def + ")", UIFactory.TextDim));
            sb.Append('\n');
            sb.Append("소지금  ").Append(UIFactory.Col(s.gold.ToString("N0") + " G", UIFactory.Gold)).Append('\n');
            sb.Append("처치 수  ").Append(s.kills);
            statsText.text = sb.ToString();

            equipText.text = "무기   " + (inv.Weapon != null ? UIFactory.Col(inv.Weapon.name, inv.Weapon.color) + UIFactory.Col("  공격력+" + inv.Weapon.atk, UIFactory.TextDim) : "-") + "\n" +
                             "방어구 " + (inv.Armor != null ? UIFactory.Col(inv.Armor.name, inv.Armor.color) + UIFactory.Col("  방어력+" + inv.Armor.def, UIFactory.TextDim) : "-");
        }

        void ShowDetail()
        {
            var d = ItemDB.Get(selected);
            if (d == null)
            {
                detailText.text = UIFactory.Col("아이템을 선택하세요.", UIFactory.TextDim);
                useBtn.gameObject.SetActive(false);
                return;
            }
            string stat = d.StatText;
            string cmp = "";
            if (d.type == ItemType.Weapon && GM.Inv.Weapon != null) cmp = Diff("공격력", d.atk - GM.Inv.Weapon.atk);
            if (d.type == ItemType.Armor && GM.Inv.Armor != null) cmp = Diff("방어력", d.def - GM.Inv.Armor.def);
            detailText.text = "<size=26><b>" + UIFactory.Col(d.name, UIFactory.Gold) + "</b></size>\n" +
                              UIFactory.Col("[" + d.TypeName + "]", UIFactory.TextDim) + "\n\n" +
                              (stat.Length > 0 ? UIFactory.Col(stat, UIFactory.Good) + "\n" : "") + cmp +
                              "\n" + d.desc + "\n\n" +
                              (d.sellable ? UIFactory.Col("판매가 " + d.SellPrice + " G", UIFactory.TextDim) : UIFactory.Col("판매 불가", UIFactory.TextDim));
            bool usable = d.type == ItemType.Consumable || d.type == ItemType.Weapon || d.type == ItemType.Armor;
            useBtn.gameObject.SetActive(usable);
            UIFactory.ButtonLabel(useBtn).text = d.type == ItemType.Consumable ? "사용" : "장착";
        }

        static string Diff(string stat, int diff)
        {
            if (diff > 0) return UIFactory.Col("장착 시 " + stat + " +" + diff + " ▲", UIFactory.Good) + "\n";
            if (diff < 0) return UIFactory.Col("장착 시 " + stat + " " + diff + " ▼", UIFactory.Bad) + "\n";
            return UIFactory.Col("현재 장비와 동일", UIFactory.TextDim) + "\n";
        }

        void UseSelected()
        {
            var d = ItemDB.Get(selected);
            if (d == null) return;
            if (d.type == ItemType.Weapon || d.type == ItemType.Armor)
            {
                GM.Inv.Equip(d.id);
                if (GM.Player != null) GM.Player.RefreshLook();
                SoundFX.Play(Sfx.Pickup);
                GM.UI.Hud.Toast(UIFactory.Col(d.name, UIFactory.Gold) + " 장착!");
            }
            else if (d.type == ItemType.Consumable)
            {
                bool closes = d.effect == "return";
                GM.UseItem(d.id);
                if (closes) { Close(); return; }
            }
            if (IsOpen) Refresh();
        }
    }

}
