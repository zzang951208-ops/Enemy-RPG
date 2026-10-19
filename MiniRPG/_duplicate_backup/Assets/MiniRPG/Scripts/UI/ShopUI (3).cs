using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>상점 창: 구매 / 판매 탭</summary>
    public class ShopUI : UIWindow
    {
        RectTransform list, detail;
        Text titleText, goldText, detailName, detailBody, detailStat;
        Button buyTab, sellTab, actionBtn, actionAllBtn;
        ScrollRect scroll;
        string[] stock = new string[0];
        bool sellMode;
        string selected;

        GameManager GM { get { return GameManager.I; } }

        public void Build(RectTransform parent)
        {
            RectTransform body;
            root = UIFactory.Window(parent, "상점", new Vector2(1100, 700), Close, out body, out titleText);

            // 상단 탭 + 소지금
            var top = UIFactory.Rect("Top", body);
            top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1); top.anchoredPosition = Vector2.zero; top.sizeDelta = new Vector2(0, 52);
            buyTab = UIFactory.Button(top, "구매", () => SetMode(false), null, 22);
            UIFactory.Place((RectTransform)buyTab.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(150, 46));
            sellTab = UIFactory.Button(top, "판매", () => SetMode(true), null, 22);
            UIFactory.Place((RectTransform)sellTab.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(160, 0), new Vector2(150, 46));
            goldText = UIFactory.Label(top, "", 24, TextAnchor.MiddleRight, UIFactory.Gold);
            UIFactory.Stretch(goldText.rectTransform, 340, 6, 0, 0);

            // 좌측 목록
            var left = UIFactory.Rect("Left", body);
            left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0.58f, 1);
            left.offsetMin = new Vector2(0, 0); left.offsetMax = new Vector2(-8, -62);
            list = UIFactory.ScrollList(left, out scroll, 6);
            UIFactory.Stretch((RectTransform)scroll.transform);

            // 우측 상세
            var dp = UIFactory.Panel(body, "Detail", UIFactory.PanelMid);
            detail = dp.rectTransform;
            detail.anchorMin = new Vector2(0.58f, 0); detail.anchorMax = new Vector2(1, 1);
            detail.offsetMin = new Vector2(8, 0); detail.offsetMax = new Vector2(0, -62);
            detailName = UIFactory.Label(detail, "", 28, TextAnchor.UpperLeft, UIFactory.Gold);
            UIFactory.Stretch(detailName.rectTransform, 22, 22, 20, 0);
            detailName.rectTransform.anchorMin = new Vector2(0, 1);
            detailName.rectTransform.offsetMin = new Vector2(22, -60);
            detailName.fontStyle = FontStyle.Bold;
            detailStat = UIFactory.Label(detail, "", 21, TextAnchor.UpperLeft, UIFactory.Good);
            UIFactory.Stretch(detailStat.rectTransform, 22, 22, 72, 0);
            detailStat.rectTransform.anchorMin = new Vector2(0, 1);
            detailStat.rectTransform.offsetMin = new Vector2(22, -150);
            detailBody = UIFactory.Label(detail, "", 20, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch(detailBody.rectTransform, 22, 22, 160, 130);
            detailBody.lineSpacing = 1.2f;

            actionBtn = UIFactory.Button(detail, "구매", DoAction, new Color(0.25f, 0.55f, 0.32f), 24);
            UIFactory.Place((RectTransform)actionBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-95, 22), new Vector2(180, 60));
            actionAllBtn = UIFactory.Button(detail, "모두 판매", DoActionAll, new Color(0.55f, 0.42f, 0.2f), 22);
            UIFactory.Place((RectTransform)actionAllBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(95, 22), new Vector2(180, 60));

            root.gameObject.SetActive(false);
        }

        public void Open(string title, string[] ids, bool sell)
        {
            if (GM.UI.Dialogue.IsOpen) GM.UI.Dialogue.Close();
            titleText.text = title;
            stock = ids ?? new string[0];
            ShowRoot();
            SetMode(sell);
        }

        void SetMode(bool sell)
        {
            sellMode = sell;
            selected = null;
            buyTab.GetComponent<Image>().color = sell ? UIFactory.BtnNormal : UIFactory.Accent;
            sellTab.GetComponent<Image>().color = sell ? UIFactory.Accent : UIFactory.BtnNormal;
            Refresh();
        }

        void Refresh()
        {
            goldText.text = "소지금  " + GM.Stats.gold.ToString("N0") + " G";
            UIFactory.Clear(list);
            var ids = new List<string>();
            if (!sellMode) ids.AddRange(stock);
            else foreach (var s in GM.Inv.items) if (s.Def != null && s.Def.sellable) ids.Add(s.id);

            if (ids.Count == 0)
            {
                var empty = UIFactory.Label(list, sellMode ? "판매할 수 있는 물건이 없습니다." : "판매 중인 물건이 없습니다.", 20, TextAnchor.MiddleCenter, UIFactory.TextDim);
                UIFactory.Size(empty, -1, 80);
            }
            if (selected != null && !ids.Contains(selected)) selected = null;
            if (selected == null && ids.Count > 0) selected = ids[0];

            foreach (var id in ids) Row(id);
            ShowDetail();
        }

        void Row(string id)
        {
            var d = ItemDB.Get(id);
            if (d == null) return;
            bool sel = id == selected;
            string capturedId = id;
            var btn = UIFactory.Button(list, "", () => { selected = capturedId; Refresh(); }, sel ? new Color(0.3f, 0.4f, 0.62f) : UIFactory.PanelLight);
            UIFactory.Size(btn, -1, 62);
            UIFactory.ButtonLabel(btn).gameObject.SetActive(false);

            var icon = UIFactory.Panel(btn.transform, "Icon", d.color);
            UIFactory.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(42, 42));
            string owned = "";
            int cnt = GM.Inv.Count(id);
            if (GM.Inv.weaponId == id || GM.Inv.armorId == id) owned = "  " + UIFactory.Col("[장착중]", UIFactory.Good);
            else if (cnt > 0) owned = "  " + UIFactory.Col("보유 " + cnt, UIFactory.TextDim);
            var name = UIFactory.Label(btn.transform, d.name + owned, 21, TextAnchor.MiddleLeft, UIFactory.TextMain);
            UIFactory.Stretch(name.rectTransform, 64, 150, 0, 0);
            int price = sellMode ? d.SellPrice : d.price;
            bool afford = sellMode || GM.Stats.gold >= price;
            var pr = UIFactory.Label(btn.transform, price + " G", 21, TextAnchor.MiddleRight, afford ? UIFactory.Gold : UIFactory.Bad);
            UIFactory.Stretch(pr.rectTransform, 300, 14, 0, 0);
        }

        void ShowDetail()
        {
            var d = ItemDB.Get(selected);
            actionBtn.gameObject.SetActive(d != null);
            actionAllBtn.gameObject.SetActive(d != null && sellMode && GM.Inv.Count(selected) > 1);
            if (d == null) { detailName.text = ""; detailStat.text = ""; detailBody.text = ""; return; }

            detailName.text = d.name;
            string stat = d.StatText;
            // 현재 장비와 비교
            if (d.type == ItemType.Weapon && GM.Inv.Weapon != null)
                stat += "\n" + Compare("공격력", d.atk - GM.Inv.Weapon.atk);
            else if (d.type == ItemType.Armor && GM.Inv.Armor != null)
                stat += "\n" + Compare("방어력", d.def - GM.Inv.Armor.def);
            detailStat.text = stat;
            detailBody.text = UIFactory.Col("[" + d.TypeName + "]", UIFactory.TextDim) + "\n" + d.desc + "\n\n" +
                              (sellMode ? "판매가: " + UIFactory.Col(d.SellPrice + " G", UIFactory.Gold) + "  (보유 " + GM.Inv.Count(selected) + "개)"
                                        : "가격: " + UIFactory.Col(d.price + " G", UIFactory.Gold));
            UIFactory.ButtonLabel(actionBtn).text = sellMode ? "1개 판매" : "구매";
            UIFactory.ButtonLabel(actionAllBtn).text = "모두 판매";
            actionBtn.interactable = sellMode || GM.Stats.gold >= d.price;
        }

        static string Compare(string stat, int diff)
        {
            if (diff > 0) return UIFactory.Col("현재 장비보다 " + stat + " +" + diff + " ▲", UIFactory.Good);
            if (diff < 0) return UIFactory.Col("현재 장비보다 " + stat + " " + diff + " ▼", UIFactory.Bad);
            return UIFactory.Col("현재 장비와 동일", UIFactory.TextDim);
        }

        void DoAction()
        {
            var d = ItemDB.Get(selected);
            if (d == null) return;
            if (sellMode) Sell(d, 1);
            else Buy(d);
            Refresh();
        }

        void DoActionAll()
        {
            var d = ItemDB.Get(selected);
            if (d == null || !sellMode) return;
            Sell(d, GM.Inv.Count(d.id));
            Refresh();
        }

        void Buy(ItemDef d)
        {
            if (!GM.Stats.Spend(d.price))
            {
                SoundFX.Play(Sfx.Lose, 0.5f);
                GM.UI.Hud.Toast(UIFactory.Col("골드가 부족합니다", UIFactory.Bad));
                return;
            }
            GM.Inv.Add(d.id);
            SoundFX.Play(Sfx.Coin);
            // 더 좋은 장비면 자동 장착
            bool better = (d.type == ItemType.Weapon && (GM.Inv.Weapon == null || d.atk > GM.Inv.Weapon.atk)) ||
                          (d.type == ItemType.Armor && (GM.Inv.Armor == null || d.def > GM.Inv.Armor.def));
            if (better)
            {
                GM.Inv.Equip(d.id);
                if (GM.Player != null) GM.Player.RefreshLook();
                GM.UI.Hud.Toast(UIFactory.Col(d.name, UIFactory.Gold) + " 구매 후 장착했습니다!");
            }
            else GM.UI.Hud.Toast(UIFactory.Col(d.name, UIFactory.Gold) + " 을(를) 구매했습니다.");
        }

        void Sell(ItemDef d, int n)
        {
            if (n <= 0 || !d.sellable) return;
            if (!GM.Inv.Remove(d.id, n)) return;
            int total = d.SellPrice * n;
            GM.Stats.AddGold(total);
            SoundFX.Play(Sfx.Coin);
            GM.UI.Hud.Toast(d.name + " x" + n + " 판매  " + UIFactory.Col("+" + total + " G", UIFactory.Gold));
        }
    }
}
