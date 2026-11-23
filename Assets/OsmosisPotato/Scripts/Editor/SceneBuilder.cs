using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Osmosis.EditorTools.UIKitBuilder;

namespace Osmosis.EditorTools
{
    /// Builds MenuScene, GuidanceScene and a placeholder LabScene in the reference style (M2),
    /// and registers them in the build list. Re-runnable: overwrites the three scenes.
    public static class SceneBuilder
    {
        const string Dir = "Assets/OsmosisPotato/Scenes/";
        const string MenuArt = "ART/MainMenuPNG/";

        // ponytail: practical number/title are placeholders until the suite assigns this practical a number
        const string PracticalNo = "BIOLOGY PRACTICAL";
        const string PracticalTitle = "Investigate Osmosis In Potato Strips Using\nSugar Solutions Of Different Concentrations";

        [MenuItem("Osmosis/Build Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UIKitBuilder.Build();          // prefabs first (scenes instance them)
            Menu();
            Guidance();
            Lab();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Dir + Scenes.Menu + ".unity", true),
                new EditorBuildSettingsScene(Dir + Scenes.Guidance + ".unity", true),
                new EditorBuildSettingsScene(Dir + Scenes.Lab + ".unity", true),
            };
            EditorSceneManager.OpenScene(Dir + Scenes.Menu + ".unity");
            Debug.Log("[Osmosis] Scenes built: Menu, Guidance, Lab (placeholder). Build list updated.");
        }

        // ---------------- Menu ----------------

        static void Menu()
        {
            var canvas = NewScene(out var actions);
            Instance("SFX", null);
            GraphPaper(canvas);

            var footer = Img("Footer", canvas, Spr(MenuArt + "FooterCloud.png"), Color.white, 1920, 425);
            Anchor(footer, new Vector2(0.5f, 0), new Vector2(0, -70));   // only the hill tops show, like the reference

            Deco(canvas, MenuArt + "appratus.png", -700, -10, 330);
            Deco(canvas, MenuArt + "gasses.png", -760, 340, 120);
            Deco(canvas, MenuArt + "gasses.png", 640, -50, 170);
            Deco(canvas, MenuArt + "plus.png", 760, -330, 55);

            var no = Label("PracticalNo", canvas, PracticalNo, 64, Ink, 1000, 90);
            Pos(no.gameObject, 0, 410);
            var line = Img("UnderLine", canvas, Spr(MenuArt + "headling_line.png"), Color.white, 460, 8);
            Pos(line, 0, 360);
            var title = Label("PracticalName", canvas, PracticalTitle, 54, Ink, 1500, 170);
            Pos(title.gameObject, 0, 235);

            var plate = Img("MainMenuHead", canvas, Spr(MenuArt + "MainMenuCenter.png"), Color.white, 690, 257);
            plate.GetComponent<Image>().preserveAspect = true;
            Pos(plate, 0, 10);

            var study = Instance("UI/StudyWindow", canvas).GetComponent<PopupPanel>();
            var settings = SettingsPopup(canvas);

            string[] labels = { "PLAY", "STUDY", "SETTINGS", "QUIT" };
            string[] colors = { "yellow", "green", "red", "blue" };
            for (int i = 0; i < 4; i++)
            {
                var b = Img(labels[i] + "Btn", canvas, Spr(MenuArt + "button_" + colors[i] + ".png"), Color.white, 190, 190);
                Pos(b, -348 + i * 232, -215);
                Label("Text", b.transform, labels[i], i == 2 ? 30 : 38, i == 0 ? Ink : Color.white, 180, 60);
                var btn = b.AddComponent<SfxButton>();
                b.AddComponent<PulseScale>().enabled = i == 0;                       // invite the first tap
                if (i == 0) UnityEventTools.AddPersistentListener(btn.onClick, actions.Play);
                if (i == 1) UnityEventTools.AddPersistentListener(btn.onClick, study.Open);
                if (i == 2) UnityEventTools.AddPersistentListener(btn.onClick, settings.Open);
                if (i == 3) UnityEventTools.AddPersistentListener(btn.onClick, actions.Quit);
            }
            study.transform.SetAsLastSibling();
            settings.transform.SetAsLastSibling();
            Save(Scenes.Menu);
        }

        static PopupPanel SettingsPopup(Transform canvas)
        {
            var (root, card) = Popup("SettingsPanel", 760, 520);
            root.transform.SetParent(canvas, false);
            Stretch(root);
            var view = root.AddComponent<SettingsView>();
            Pos(Label("Title", card.transform, "Settings", 64, Green, 700, 90).gameObject, 0, 170);
            var music = SmallButton("MusicBtn", card.transform, "Music: ON", 0, 40);
            var sound = SmallButton("SoundBtn", card.transform, "Sound: ON", 0, -80);
            ((RectTransform)music.transform).sizeDelta = ((RectTransform)sound.transform).sizeDelta = new Vector2(420, 90);
            UnityEventTools.AddPersistentListener(music.onClick, view.ToggleMusic);
            UnityEventTools.AddPersistentListener(sound.onClick, view.ToggleSound);
            Set(view, "musicLabel", music.GetComponentInChildren<TMP_Text>());
            Set(view, "soundLabel", sound.GetComponentInChildren<TMP_Text>());
            var close = Img("CloseBtn", card.transform, Spr("Sprites/close.png"), Color.white, 80, 80);
            Anchor(close, new Vector2(1, 1), new Vector2(-55, -55));
            UnityEventTools.AddPersistentListener(close.AddComponent<SfxButton>().onClick, root.GetComponent<PopupPanel>().Close);
            return root.GetComponent<PopupPanel>();
        }

        // ---------------- Guidance ----------------

        struct Box { public string head, body; public Box(string h, string b) { head = h; body = b; } }

        static readonly (string lead, Box[] boxes)[] Slides =
        {
            ("Water moves by OSMOSIS:", new[] {
                new Box("Osmosis", "Water molecules move from a <b>dilute</b> solution to a <b>concentrated</b> solution."),
                new Box("Membrane", "They pass through a <b>partially permeable membrane</b> that holds back sugar."),
                new Box("Cells", "Every potato cell is wrapped in such a membrane, so water can move in or out.") }),
            ("The solution decides which way water moves:", new[] {
                new Box("Hypotonic", "Dilute solution.\nWater moves <b>IN</b>.\nCells become <b>turgid</b>."),
                new Box("Isotonic", "Same as cell sap.\n<b>No net</b> movement.\nNo change."),
                new Box("Hypertonic", "Concentrated solution.\nWater moves <b>OUT</b>.\nCells become <b>flaccid</b>.") }),
            ("What happens to a potato strip?", new[] {
                new Box("In water", "The strip absorbs water and gets <b>longer</b> and firmer."),
                new Box("In strong sugar", "The strip loses water and gets <b>shorter</b> and softer."),
                new Box("Measure it!", "The <b>change in length</b> shows how much water moved.") }),
            ("In this practical you will:", new[] {
                new Box("1. Measure", "Measure the <b>initial length</b> of a potato strip with the ruler."),
                new Box("2. Soak", "Put it in a sugar solution and <b>fast forward 30 minutes</b>."),
                new Box("3. Record", "Measure the <b>final length</b> and record the change. Repeat for other concentrations.") }),
            ("Finding the ISOTONIC POINT:", new[] {
                new Box("Plot", "Plot change in length against concentration."),
                new Box("Zero", "Find where the line crosses <b>zero change</b>."),
                new Box("Meaning", "That concentration equals the concentration of the <b>potato cell sap</b>.") }),
        };

        static void Guidance()
        {
            var canvas = NewScene(out var actions);
            GraphPaper(canvas);

            var slides = new GameObject[Slides.Length];
            for (int i = 0; i < Slides.Length; i++) { slides[i] = Slide(canvas, i); slides[i].SetActive(i == 0); }

            var back = NavButton(canvas, "BackBtn", "Sprites/Guidance/BACK.png", -820);
            var next = NavButton(canvas, "NextBtn", "Sprites/Guidance/NEXT.png", 820);
            var gotIt = SmallButton("GotItBtn", canvas, "Got it!", 0, -470);
            ((RectTransform)gotIt.transform).sizeDelta = new Vector2(320, 96);
            gotIt.gameObject.AddComponent<PulseScale>();
            gotIt.gameObject.SetActive(false);   // SlideshowView shows it on the last slide
            UnityEventTools.AddPersistentListener(gotIt.onClick, actions.StartLab);

            var exit = Instance("UI/ConfirmDialog", canvas).GetComponent<PopupPanel>();
            HomeButton(canvas, exit);

            var show = canvas.gameObject.AddComponent<SlideshowView>();
            Set(show, "slides", slides);
            Set(show, "back", back);
            Set(show, "next", next);
            Set(show, "gotIt", gotIt.gameObject);
            UnityEventTools.AddPersistentListener(back.onClick, show.Back);
            UnityEventTools.AddPersistentListener(next.onClick, show.Next);
            exit.transform.SetAsLastSibling();
            Save(Scenes.Guidance);
        }

        /// "Do You Know?" card in the reference guide1.png style: yellow card, green-headed white boxes.
        static GameObject Slide(Transform canvas, int index)
        {
            var (lead, boxes) = Slides[index];
            var card = Img("Slide" + (index + 1), canvas, rounded, Hex("FFD60A"), 1560, 820);
            Pos(card, 0, 20);
            var title = Label("Title", card.transform, "Do You Know?", 84, Color.white, 760, 130);
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;
            title.alignment = TextAlignmentOptions.Left;
            Pos(title.gameObject, -360, 320);
            Pos(Label("Lead", card.transform, lead, 46, Ink, 780, 120).gameObject, 330, 320);

            for (int b = 0; b < boxes.Length; b++)
            {
                var box = Img("Box" + (b + 1), card.transform, rounded, DarkGreen, 460, 520);
                Pos(box, -500 + b * 500, -60);
                var inner = Img("Inner", box.transform, rounded, Color.white, 0, 0);
                Stretch(inner, 5);
                var head = Img("Head", box.transform, rounded, Green, 460, 120);
                Anchor(head, new Vector2(0.5f, 1), new Vector2(0, -60));
                Label("Text", head.transform, boxes[b].head, 44, Color.white, 440, 110);
                var body = Label("Body", box.transform, boxes[b].body, 38, Hex("1B3A8C"), 410, 360);
                body.fontStyle = FontStyles.Normal;
                Pos(body.gameObject, 0, -60);
            }
            return card;
        }

        static Button NavButton(Transform canvas, string name, string sprite, float x)
        {
            var go = Img(name, canvas, Spr(sprite), Color.white, 150, 150);
            Pos(go, x, 20);
            return go.AddComponent<SfxButton>();
        }

        static void HomeButton(Transform canvas, PopupPanel exit)
        {
            var home = Img("HomeBtn", canvas, Spr("Sprites/LeftTopMenu/home.png"), Color.white, 110, 110);
            Anchor(home, new Vector2(0, 1), new Vector2(90, -80));
            UnityEventTools.AddPersistentListener(home.AddComponent<SfxButton>().onClick, exit.Open);
        }

        // ---------------- Lab (see LabBuilder) ----------------

        static void Lab()
        {
            var canvas = NewScene(out var actions);
            LabBuilder.Build(canvas, actions);
            Save(Scenes.Lab);
        }

        // ---------------- shared ----------------

        /// Empty scene + white camera + EventSystem (Input System module) + 1920x1080 scaled canvas with MenuActions.
        internal static Transform NewScene(out MenuActions actions)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            actions = go.AddComponent<MenuActions>();
            if (!rounded) rounded = RoundedSprite();
            return go.transform;
        }

        static void GraphPaper(Transform canvas)
        {
            var bg = Img("Background", canvas, Spr(MenuArt + "bg.png"), Color.white, 0, 0);
            Stretch(bg);
            bg.GetComponent<Image>().raycastTarget = false;
        }

        static void Deco(Transform canvas, string sprite, float x, float y, float size)
        {
            var d = Img("Deco", canvas, Spr(sprite), Color.white, size, size);
            d.GetComponent<Image>().preserveAspect = true;
            d.GetComponent<Image>().raycastTarget = false;
            Pos(d, x, y);
        }

        internal static GameObject Instance(string prefab, Transform parent)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/" + prefab + ".prefab");
            return (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
        }

        internal static void Save(string scene) =>
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Dir + scene + ".unity");
    }
}
