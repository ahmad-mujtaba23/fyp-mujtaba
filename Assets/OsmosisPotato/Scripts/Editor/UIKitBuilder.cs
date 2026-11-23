using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Osmosis.EditorTools
{
    /// Builds the shared UI prefabs (task 1.6) in the reference style. Re-runnable: overwrites the prefabs.
    /// Sized for a 1920x1080 CanvasScaler reference resolution.
    public static class UIKitBuilder
    {
        const string Out = "Assets/OsmosisPotato/Prefabs/UI/";
        const string Ref = "Assets/OsmosisPotato/Reference/";
        const string RoundedPath = "Assets/OsmosisPotato/Art/UI/Rounded.png";

        public static readonly Color Green = Hex("4FB28A"), DarkGreen = Hex("2E7D5B"),
            Dim = new Color(0f, 0f, 0f, 0.6f), Red = Hex("E53935"), Ink = Hex("1E1E1E");

        internal static Sprite rounded;
        const float ButtonCorner = 3.2f;   // 48px sprite corners / 3.2 = 15px, matches reference pill buttons

        [MenuItem("Osmosis/Build UI Kit Prefabs")]
        public static void Build()
        {
            rounded = RoundedSprite();
            Directory.CreateDirectory(Out);
            PillButton();
            ConfirmDialog();
            StudyWindowPrefab();
            TopLeftMenu();
            ResultCard();
            NumpadPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[Osmosis] UI kit prefabs built in " + Out);
        }

        // ---------- prefabs ----------

        internal static void PillButton() => Save(Pill("PillButton", "Label", null, 320, 84), "PillButton");

        /// Green pill with a white icon cap on the left and a dark divider (reference "Camera"/"Reset" buttons).
        internal static GameObject Pill(string name, string label, Sprite icon, float w, float h, Transform parent = null)
        {
            var root = Img(name, parent, rounded, Green, w, h);
            root.GetComponent<Image>().pixelsPerUnitMultiplier = ButtonCorner;
            root.AddComponent<SfxButton>().targetGraphic = root.GetComponent<Image>();
            var cap = Img("IconCap", root.transform, rounded, Color.white, h, h);
            cap.GetComponent<Image>().pixelsPerUnitMultiplier = ButtonCorner;
            Anchor(cap, new Vector2(0, 0.5f), new Vector2(h / 2, 0));
            var ic = Img("Icon", cap.transform, icon, Green, h * 0.6f, h * 0.6f);
            ic.GetComponent<Image>().enabled = icon;
            ic.GetComponent<Image>().preserveAspect = true;
            var div = Img("Divider", root.transform, null, DarkGreen, 12, h);
            Anchor(div, new Vector2(0, 0.5f), new Vector2(h + 6, 0));
            var t = Label("Label", root.transform, label, 36, Color.white, w - h - 24, h);
            Anchor(t.gameObject, new Vector2(0, 0.5f), new Vector2(h + 12 + (w - h - 24) / 2, 0));
            return root;
        }

        /// Exit-to-menu dialog (reference ExitMenu.png): white card, green border, green title, YES/NO.
        internal static void ConfirmDialog()
        {
            var (root, card) = Popup("ConfirmDialog", 900, 520);
            var actions = root.AddComponent<MenuActions>();
            var t = Label("Title", card.transform, "Exit to Main Menu", 64, Green, 820, 90);
            Pos(t.gameObject, 0, 170);
            Pos(Img("Underline", card.transform, null, Ink, 560, 4), 0, 115);
            var m = Label("Message", card.transform,
                "Are you sure you want to quit the experiment and return to the main menu?", 40, Ink, 760, 160);
            Pos(m.gameObject, 0, 10);
            var yes = SmallButton("YesBtn", card.transform, "YES", -130, -170);
            var no = SmallButton("NoBtn", card.transform, "NO", 130, -170);
            UnityEventTools.AddPersistentListener(yes.onClick, actions.ToMenu);
            UnityEventTools.AddPersistentListener(no.onClick, root.GetComponent<PopupPanel>().Close);
            Save(root, "ConfirmDialog");
        }

        /// Study window: yellow STUDY header (reference sprite), 3 baked-text tabs, scrolling pages, close X.
        // Study window content (task 2.4). Concept / Procedure / Observations.
        static readonly string[] StudyText =
        {
            "<b>Osmosis</b> is the movement of water molecules from a region of <b>higher water concentration</b> (dilute solution) " +
            "to a region of <b>lower water concentration</b> (concentrated solution) through a <b>partially permeable membrane</b>.\n\n" +
            "The cell membrane of a potato cell lets small water molecules pass but holds back larger sugar molecules.\n\n" +
            "<b>Hypotonic solution</b> (more dilute than cell sap): water moves <b>into</b> the cells. The cells swell and become " +
            "<b>turgid</b>, and the potato strip gets <b>longer</b>.\n\n" +
            "<b>Isotonic solution</b> (same concentration as cell sap): no net movement of water, so <b>no change</b> in length.\n\n" +
            "<b>Hypertonic solution</b> (more concentrated than cell sap): water moves <b>out</b> of the cells. The cells become " +
            "<b>flaccid / plasmolysed</b>, and the strip gets <b>shorter</b>.\n\n" +
            "The concentration at which the change in length is zero is the <b>isotonic point</b>. It tells us the concentration of the potato cell sap.",

            "1. Take a potato strip from the inventory and measure its <b>initial length</b> with the ruler.\n\n" +
            "2. Take a beaker and set the concentration of the sugar solution with the slider.\n\n" +
            "3. Drag the potato strip and drop it into the beaker.\n\n" +
            "4. Press <b>Fast Forward</b> to let 30 minutes pass.\n\n" +
            "5. Take the strip out and measure its <b>final length</b>. Record the reading.\n\n" +
            "6. Repeat with other concentrations (for example 0%, 20% and 40%).\n\n" +
            "7. Open <b>Readings</b>, plot change in length against concentration, and find where the line crosses zero: the <b>isotonic point</b>.",

            "For each strip, record:\n\n" +
            "<b>Concentration of solution (%)</b>\n<b>Initial length (mm)</b>\n<b>Final length (mm)</b>\n" +
            "<b>Change in length (mm)</b> = final length - initial length\n\n" +
            "A <b>positive</b> change means the strip gained water (hypotonic solution).\n" +
            "A <b>negative</b> change means the strip lost water (hypertonic solution).\n\n" +
            "Plot <b>change in length</b> (y-axis) against <b>concentration</b> (x-axis). " +
            "The concentration where the line crosses <b>zero</b> is the isotonic point.",
        };

        internal static void StudyWindowPrefab()
        {
            var (root, card) = Popup("StudyWindow", 1600, 900);
            var header = Img("Header", card.transform, Spr("Sprites/Study/Top.png"), Color.white, 1600, 173);
            Anchor(header, new Vector2(0.5f, 1), new Vector2(0, -86));
            var close = Img("CloseBtn", card.transform, Spr("Sprites/close.png"), Color.white, 80, 80);
            Anchor(close, new Vector2(1, 1), new Vector2(-60, -86));
            var closeBtn = close.AddComponent<SfxButton>();
            UnityEventTools.AddPersistentListener(closeBtn.onClick, root.GetComponent<PopupPanel>().Close);

            string[] names = { "Concept", "Procedure", "Observations" };
            Sprite[] open = { Spr("Sprites/Study/ConOpen.png"), Spr("Sprites/Study/pROoPEN.png"), Spr("Sprites/Study/ObsOpen.png") };
            Sprite[] closed = { Spr("Sprites/Study/ConClose.png"), Spr("Sprites/Study/pROClose.png"), Spr("Sprites/Study/ObsClose.png") };
            var tabs = new Image[3];
            var pages = new ScrollRect[3];
            var study = root.AddComponent<StudyWindow>();
            for (int i = 0; i < 3; i++)
            {
                var tab = Img(names[i] + "Tab", card.transform, closed[i], Color.white, 330, 90);
                Anchor(tab, new Vector2(0.5f, 1), new Vector2(-170 + i * 340, -86));
                tabs[i] = tab.GetComponent<Image>();
                var btn = tab.AddComponent<SfxButton>();
                UnityEventTools.AddIntPersistentListener(btn.onClick, study.Select, i);
                pages[i] = Page(names[i] + "Page", card.transform, StudyText[i]);
                pages[i].gameObject.SetActive(i == 0);
            }
            Set(study, "pages", pages);
            Set(study, "tabs", tabs);
            Set(study, "tabOpen", open);
            Set(study, "tabClosed", closed);
            Save(root, "StudyWindow");
        }

        /// Top-left utilities: Home / Study / Readings icon buttons on a white rounded panel, red count badge on Readings.
        internal static void TopLeftMenu()
        {
            var root = Img("TopLeftMenu", null, Spr("Sprites/LeftTopMenu/topleftBg.png"), Color.white, 420, 150);
            Anchor(root, new Vector2(0, 1), new Vector2(230, -95));
            string[] names = { "Home", "Study", "Readings" };
            string[] icons = { "home", "study", "reading" };
            GameObject readings = null;
            for (int i = 0; i < 3; i++)
            {
                var b = Img(names[i] + "Btn", root.transform, Spr("Sprites/LeftTopMenu/" + icons[i] + ".png"), Color.white, 90, 90);
                b.GetComponent<Image>().preserveAspect = true;
                Pos(b, -130 + i * 130, 18);
                b.AddComponent<SfxButton>();
                Pos(Label("Text", b.transform, names[i], 26, Ink, 140, 34).gameObject, 0, -65);
                readings = b;
            }
            var badgeRoot = readings.AddComponent<ReadingsBadge>();
            var badge = Img("Badge", readings.transform, rounded, Red, 44, 44);
            badge.GetComponent<Image>().pixelsPerUnitMultiplier = 2.2f;   // 48px corners -> full circle at 44px
            Pos(badge, 40, 40);
            var count = Label("Count", badge.transform, "1", 26, Color.white, 44, 44);
            Set(badgeRoot, "badge", badge);
            Set(badgeRoot, "countText", count);
            Save(root, "TopLeftMenu");
        }

        /// Result card: reference "Well Done" yellow cloud card, value lines, Once Again / Quit buttons.
        internal static void ResultCard()
        {
            var (root, card) = Popup("ResultCard", 1300, 885, Spr("Sprites/Success/successbg.png"));
            var actions = root.AddComponent<MenuActions>();
            var body = Label("Body", card.transform,
                "Initial length: -- mm\nFinal length: -- mm\nChange in length: -- mm\nObservation: --",
                44, DarkGreen, 1060, 300);
            body.alignment = TextAlignmentOptions.Left;
            body.enableAutoSizing = true; body.fontSizeMin = 20; body.fontSizeMax = 40;
            Pos(body.gameObject, 0, -105);              // below the card's baked-in "completed practical!" line
            var again = Img("AgainBtn", card.transform, Spr("Sprites/Success/successagain.png"), Color.white, 360, 126);
            var quit = Img("QuitBtn", card.transform, Spr("Sprites/Success/successquit.png"), Color.white, 360, 126);
            Pos(again, -220, -320);
            Pos(quit, 220, -320);
            UnityEventTools.AddPersistentListener(again.AddComponent<SfxButton>().onClick, actions.Again);
            UnityEventTools.AddPersistentListener(quit.AddComponent<SfxButton>().onClick, actions.ToMenu);
            Save(root, "ResultCard");
        }

        /// Reference numpad: grey keys on numpad_bg, rounded corner keys, green backspace, red X close.
        /// Root stays active (holds the Numpad component); only Panel toggles.
        internal static void NumpadPrefab()
        {
            const string art = "GlobalModules/UI Modules/Numpad/Art/";
            var root = new GameObject("Numpad", typeof(RectTransform));
            Stretch(root);
            var pad = root.AddComponent<Numpad>();
            var panel = Img("Panel", root.transform, Spr(art + "numpad_bg.png"), Color.white, 440, 520);
            Anchor(panel, new Vector2(1, 0.5f), new Vector2(-260, -130));   // clear of the stopwatch + Reset
            Set(pad, "panel", panel);

            string[] keys = { "7", "8", "9", "4", "5", "6", "1", "2", "3", ".", "0", "<" };
            for (int i = 0; i < keys.Length; i++)
            {
                int row = i / 3, col = i % 3;
                string sprite = i == 0 ? "top_left" : i == 2 ? "top_right" : i == 9 ? "bottom_left"
                              : i == 11 ? "bottom_right_green" : "regular";
                var k = Img("Key" + keys[i], panel.transform, Spr(art + sprite + ".png"), Color.white, 110, 110);
                Pos(k, -120 + col * 120, 170 - row * 115);
                Label("Text", k.transform, keys[i], 48, Color.white, 110, 110);
                UnityEventTools.AddStringPersistentListener(k.AddComponent<SfxButton>().onClick, pad.Press, keys[i]);
            }
            var close = Img("CloseBtn", panel.transform, Spr(art + "remove_button.png"), Color.white, 64, 64);
            Anchor(close, new Vector2(1, 1), new Vector2(-14, -14));
            UnityEventTools.AddPersistentListener(close.AddComponent<SfxButton>().onClick, pad.Close);
            panel.SetActive(false);
            Save(root, "Numpad");
        }

        // ---------- building blocks ----------

        /// Full-screen dim + centred card with PopupPanel (spring in/out). Card = green border + white inset unless a sprite is given.
        internal static (GameObject root, GameObject card) Popup(string name, float w, float h, Sprite cardSprite = null)
        {
            var root = Img(name, null, null, Dim, 0, 0);
            Stretch(root);
            root.AddComponent<CanvasGroup>();
            var pop = root.AddComponent<PopupPanel>();
            GameObject card;
            if (cardSprite)
            {
                card = Img("Card", root.transform, cardSprite, Color.white, w, h);
            }
            else
            {
                card = Img("Card", root.transform, rounded, Green, w, h);
                var inner = Img("Inner", card.transform, rounded, Color.white, 0, 0);
                Stretch(inner, 8);
            }
            Set(pop, "card", card.GetComponent<RectTransform>());
            root.SetActive(false);   // hidden until PopupPanel.Open()
            return (root, card);
        }

        internal static Button SmallButton(string name, Transform parent, string text, float x, float y)
        {
            var b = Img(name, parent, rounded, Green, 200, 84);
            b.GetComponent<Image>().pixelsPerUnitMultiplier = ButtonCorner;
            Pos(b, x, y);
            Label("Text", b.transform, text, 40, Color.white, 200, 84);
            return b.AddComponent<SfxButton>();
        }

        internal static ScrollRect Page(string name, Transform parent, string text)
        {
            var view = Img(name, parent, null, new Color(1, 1, 1, 0), 1500, 620);
            Anchor(view, new Vector2(0.5f, 0), new Vector2(0, 340));
            view.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(view.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 1200);   // ponytail: fixed height, add ContentSizeFitter if pages outgrow it
            var t = Label("Text", content.transform, text, 36, Ink, 0, 0);
            t.alignment = TextAlignmentOptions.TopLeft;
            Stretch(t.gameObject, 20);
            var sr = view.AddComponent<ScrollRect>();
            sr.content = crt; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped;
            return sr;
        }

        internal static GameObject Img(string name, Transform parent, Sprite sprite, Color color, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            if (parent) go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite == rounded && sprite) img.type = Image.Type.Sliced;
            ((RectTransform)go.transform).sizeDelta = new Vector2(w, h);
            return go;
        }

        internal static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(w, h);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        internal static void Pos(GameObject go, float x, float y) => ((RectTransform)go.transform).anchoredPosition = new Vector2(x, y);

        internal static void Anchor(GameObject go, Vector2 anchor, Vector2 pos)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
        }

        internal static void Stretch(GameObject go, float inset = 0)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Set(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Save(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go, Out + name + ".prefab");
            Object.DestroyImmediate(go);
        }

        internal static Sprite Spr(string path)
        {
            var full = Ref + path;
            if (AssetImporter.GetAtPath(full) is TextureImporter ti && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(full);
            if (!s) Debug.LogWarning("[Osmosis] missing sprite " + full);
            return s;
        }

        /// One white 9-sliced rounded rect, tinted per use: pills, cards, borders, badges.
        internal static Sprite RoundedSprite()
        {
            if (!File.Exists(RoundedPath))
            {
                const int size = 128, r = 48;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, size - r), cy = Mathf.Clamp(y + 0.5f, r, size - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(RoundedPath));
                File.WriteAllBytes(RoundedPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(RoundedPath);
            }
            var ti = (TextureImporter)AssetImporter.GetAtPath(RoundedPath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;   // project default is Multiple, which ignores spriteBorder
            ti.spriteBorder = new Vector4(48, 48, 48, 48);
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath);
        }

        internal static Color Hex(string h) => ColorUtility.TryParseHtmlString("#" + h, out var c) ? c : Color.magenta;
    }
}
