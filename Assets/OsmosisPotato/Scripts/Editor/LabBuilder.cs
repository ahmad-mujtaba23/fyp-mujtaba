using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static Osmosis.EditorTools.UIKitBuilder;

namespace Osmosis.EditorTools
{
    /// LabScene content (M4). 3D: fixed front camera, reference wall+blackboard backdrop, wooden bench,
    /// instruction text typed onto the blackboard. HUD: inventory shelf, top-left menu, stopwatch, Reset, feedback.
    /// Interactive parts are added task by task (4.3+), each together with its wiring and tests.
    public static class LabBuilder
    {
        const string MatDir = "Assets/OsmosisPotato/Art/Materials/";
        static readonly Color Wall = Hex("EEF0EF");

        public static void Build(Transform canvas, MenuActions actions)
        {
            var world = new GameObject("Lab").transform;
            Camera3D();
            Light();
            Backdrop(world);
            Bench(world);
            Board(world);
            Apparatus(world);
            Hud(canvas, actions);
            Flow();
        }

        // ---------------- 3D ----------------

        static void Camera3D()
        {
            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 0.75f, -1.9f), Quaternion.Euler(10f, 0f, 0f));
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            cam.backgroundColor = Wall;
            cam.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();   // touches on 3D apparatus
            cam.gameObject.AddComponent<CameraRig>();
        }

        static void Light()
        {
            var l = new GameObject("Directional Light").AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            l.intensity = 0.9f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.45f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.57f);
        }

        /// Reference wall-with-blackboard art (Environment/Sprites/front_15.png) on an unlit quad behind the bench.
        static void Backdrop(Transform world)
        {
            // Sized so the blackboard covers ~30% of screen height between the bench and the HUD shelf.
            var q = Primitive(PrimitiveType.Quad, "Backdrop", world, new Vector3(0f, 0.36f, 1.6f), new Vector3(2.87f, 1.57f, 1f));
            var tex = Tex("GlobalModules/Environment/Sprites/front_15.png", readable: true);
            q.GetComponent<Renderer>().sharedMaterial = Mat("Backdrop", "Universal Render Pipeline/Unlit", tex, Color.white);
            q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            q.GetComponent<Renderer>().receiveShadows = false;
            Camera.main.backgroundColor = tex.GetPixel(4, tex.height / 2);   // wall colour, so the quad edges vanish
            boardWorld = BoardRect(tex, q.transform);
        }

        static Rect boardWorld;

        /// Finds the dark chalkboard area in the wall art (pixels well below the wall's brightness) and returns it
        /// in world X/Y on the backdrop quad, so the instruction text sits exactly on the board.
        static Rect BoardRect(Texture2D tex, Transform quad)
        {
            int minX = tex.width, minY = tex.height, maxX = 0, maxY = 0;
            var px = tex.GetPixels32();
            for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
            {
                var c = px[y * tex.width + x];
                if (c.r + c.g + c.b > 3 * 90) continue;                  // not chalkboard-dark
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
            Vector3 s = quad.localScale, p = quad.position;
            float u0 = (float)minX / tex.width, u1 = (float)maxX / tex.width, v0 = (float)minY / tex.height, v1 = (float)maxY / tex.height;
            return Rect.MinMaxRect(p.x + (u0 - 0.5f) * s.x, p.y + (v0 - 0.5f) * s.y, p.x + (u1 - 0.5f) * s.x, p.y + (v1 - 0.5f) * s.y);
        }

        /// Bench: wooden top (y = 0 is the work surface) and a grey front panel, as in the reference mockup.
        static void Bench(Transform world)
        {
            var top = Primitive(PrimitiveType.Cube, "BenchTop", world, new Vector3(0f, -0.03f, 0.25f), new Vector3(3.4f, 0.06f, 1.3f));
            top.GetComponent<Renderer>().sharedMaterial = Mat("Wood", "Universal Render Pipeline/Lit",
                Tex("ART/wood texture-01.png", repeat: true, maxSize: 1024), Color.white);   // 1x: the texture has a visible seam when tiled
            var front = Primitive(PrimitiveType.Cube, "BenchFront", world, new Vector3(0f, -0.4f, -0.38f), new Vector3(3.4f, 0.7f, 0.04f));
            front.GetComponent<Renderer>().sharedMaterial = Mat("BenchFront", "Universal Render Pipeline/Lit", null, Hex("8A9096"));
        }

        /// Instruction text over the backdrop's blackboard; typed by InstructionBoard, pulses when done.
        static void Board(Transform world)
        {
            var go = new GameObject("BoardText");
            go.transform.SetParent(world, false);
            go.transform.position = new Vector3(boardWorld.center.x, boardWorld.center.y, 1.595f);
            var t = go.AddComponent<TextMeshPro>();
            ((RectTransform)go.transform).sizeDelta = boardWorld.size * 0.84f;        // chalk margin inside the frame
            t.text = "";
            t.color = new Color(0.96f, 0.96f, 0.93f);                                 // chalk white
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.1f;
            t.fontSizeMax = 1.0f;                                                      // readable on a phone, short steps don't balloon
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            var pulse = go.AddComponent<PulseScale>();
            pulse.enabled = false;
            var board = go.AddComponent<InstructionBoard>();
            Set(board, "text", t);
            Set(board, "pulse", pulse);
        }

        // ---------------- HUD ----------------

        static void Hud(Transform canvas, MenuActions actions)
        {
            Shelf(canvas);
            Stopwatch(canvas, actions);
            ConcentrationPanelUI(canvas);
            MeasureUI(canvas);
            FastForwardUI(canvas);
            Feedback(canvas, "FeedbackCorrect", "ART/successFeedback.png");
            Feedback(canvas, "FeedbackWrong", "ART/failureFeedback.png");

            var menu = SceneBuilder.Instance("UI/TopLeftMenu", canvas);
            ReadingsUI(canvas, menu);
            var study = SceneBuilder.Instance("UI/StudyWindow", canvas).GetComponent<PopupPanel>();
            var exit = SceneBuilder.Instance("UI/ConfirmDialog", canvas).GetComponent<PopupPanel>();
            UnityEventTools.AddPersistentListener(menu.transform.Find("HomeBtn").GetComponent<Button>().onClick, exit.Open);
            UnityEventTools.AddPersistentListener(menu.transform.Find("StudyBtn").GetComponent<Button>().onClick, study.Open);
        }

        /// Translucent white shelf along the top (reference main_ui mockup) with one compartment per apparatus.
        static void Shelf(Transform canvas)
        {
            var shelf = Img("Shelf", canvas, rounded, new Color(1f, 1f, 1f, 0.85f), 1100, 160);
            shelf.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;
            Anchor(shelf, new Vector2(0.5f, 1f), new Vector2(40f, -95f));
            (string name, string label, Sprite icon, Color tint)[] items =
            {
                ("PotatoSlot", "Potato strip", PotatoIcon(), Color.white),
                ("BeakerSlot", "Beaker", Spr("Sprites/beaker.png"), Color.white),
            };
            for (int i = 0; i < items.Length; i++)
            {
                var slot = Img(items[i].name, shelf.transform, rounded, new Color(1f, 1f, 1f, 0f), 170, 150);
                Anchor(slot, new Vector2(0f, 0.5f), new Vector2(100f + i * 180f, 0f));
                var icon = Img("Icon", slot.transform, items[i].icon, items[i].tint, 100, 100);
                icon.GetComponent<Image>().preserveAspect = true;
                Pos(icon, 0, 14);
                Pos(Label("Label", slot.transform, items[i].label, 24, Ink, 170, 34).gameObject, 0, -54);
                var tick = Img("Tick", slot.transform, Spr("ART/Correct.png"), Color.white, 40, 40);
                Pos(tick, 56, 48);
                tick.SetActive(false);
                var lockOverlay = Img("Lock", slot.transform, rounded, new Color(0.2f, 0.2f, 0.2f, 0.55f), 0, 0);
                Stretch(lockOverlay, 6);
                lockOverlay.GetComponent<Image>().raycastTarget = false;
                slot.GetComponent<Image>().raycastTarget = true;           // transparent, but catches the touch
                var inv = slot.AddComponent<InventorySlot>();
                Set(inv, "prefab", i == 0 ? stripPrefab : beakerPrefab);
                Set(inv, "lockOverlay", lockOverlay);
                Set(inv, "tick", tick);
            }
            for (int i = 1; i < 6; i++)                                     // compartment dividers
            {
                var div = Img("Divider", shelf.transform, null, new Color(0.8f, 0.8f, 0.8f, 1f), 3, 130);
                Anchor(div, new Vector2(0f, 0.5f), new Vector2(10f + i * 180f, 0f));
            }
        }

        /// Stopwatch (CountdownTimer) top-right with a radial fill, and the Reset pill under it.
        static void Stopwatch(Transform canvas, MenuActions actions)
        {
            var face = Img("Stopwatch", canvas, rounded, Hex("5B6B78"), 230, 230);
            face.GetComponent<Image>().pixelsPerUnitMultiplier = 0.4f;     // 48px corners -> full circle
            Anchor(face, new Vector2(1f, 1f), new Vector2(-150f, -150f));
            var dial = Img("Dial", face.transform, rounded, Color.white, 196, 196);
            dial.GetComponent<Image>().pixelsPerUnitMultiplier = 0.48f;
            var fill = Img("Fill", face.transform, Spr("ART/Clock/icons8-circle-96.png"), Green, 196, 196);
            var fi = fill.GetComponent<Image>();
            fi.type = Image.Type.Filled;
            fi.fillMethod = Image.FillMethod.Radial360;
            fi.fillOrigin = (int)Image.Origin360.Top;
            fi.fillClockwise = false;
            fi.color = new Color(Green.r, Green.g, Green.b, 0.35f);
            var label = Label("Time", face.transform, "30:00", 54, Ink, 200, 70);
            var timer = face.AddComponent<CountdownTimer>();
            Set(timer, "label", label);
            Set(timer, "radialFill", fi);

            var reset = Pill("ResetBtn", "Reset", Spr("ART/icons8-update-left-rotation-96 (1).png"), 230, 72, canvas);
            reset.GetComponent<Image>().pixelsPerUnitMultiplier = 3.2f;
            Anchor(reset, new Vector2(1f, 1f), new Vector2(-150f, -320f));
            UnityEventTools.AddPersistentListener(reset.GetComponent<Button>().onClick, actions.Again);
        }

        static void Feedback(Transform canvas, string name, string sprite)
        {
            var f = Img(name, canvas, Spr(sprite), Color.white, 260, 260);
            f.GetComponent<Image>().preserveAspect = true;
            f.GetComponent<Image>().raycastTarget = false;
            Pos(f, 0, 60);
            f.SetActive(false);
        }

        // ---------------- apparatus (4.3) ----------------

        // World scale lives with the strip at runtime (1 unit = 36 cm: real proportions, ruler read zoomed in).
        const float UnitsPerMm = PotatoStripController.UnitsPerMm;
        const float StripLengthMm = PotatoStripController.NominalLengthMm, StripThickMm = PotatoStripController.ThickMm;
        static Draggable stripPrefab, beakerPrefab;
        static readonly Vector3 RulerPos = new Vector3(-0.72f, 0.003f, -0.1f);
        const float RulerMargin = 0.025f;
        static readonly float RulerWidth = 25f * UnitsPerMm;                       // 25 mm wide school ruler
        static readonly float RulerLength = 150f * UnitsPerMm + 2f * RulerMargin;

        static void Apparatus(Transform world)
        {
            System.IO.Directory.CreateDirectory("Assets/OsmosisPotato/Prefabs/Lab");
            stripPrefab = StripPrefab();
            beakerPrefab = BeakerPrefab();
            Ruler(world);
            var vfx = new GameObject("Vfx").AddComponent<Vfx>();
            vfx.transform.SetParent(world, false);
            Set(vfx, "splashPrefab", SplashPrefab());
            Set(vfx, "flowPrefab", FlowPrefab());

            // strip lies on the ruler with its left end on the 0 mark (centre at 25 mm for a 50 mm strip)
            float zeroX = RulerPos.x - RulerLength / 2f + RulerMargin;
            Target("RulerTarget", world, ItemKind.PotatoStrip,
                new Vector3(zeroX + StripLengthMm / 2f * UnitsPerMm, RulerPos.y + 0.003f + StripThickMm / 2f * UnitsPerMm, RulerPos.z + RulerWidth * 0.3f),
                Quaternion.Euler(0, 0, 90), StripGhost);
            for (int i = 0; i < 3; i++)
                Target("BeakerTarget" + (i + 1), world, ItemKind.Beaker, new Vector3(0.05f + i * 0.45f, 0f, 0.05f), Quaternion.identity, BeakerGhost);
        }

        static Draggable StripPrefab()
        {
            var root = new GameObject("PotatoStrip");
            // Knife-cut bar, long axis = Y (brief: Y scale = length, X/Z = thickness).
            var mesh = Primitive(PrimitiveType.Cube, "Mesh", root.transform, Vector3.zero, StripScale);
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            var mat = Mat("Potato", "Universal Render Pipeline/Lit", null, Hex("F7EBBE"));   // pale yellow potato flesh
            mat.SetFloat("_Smoothness", 0.45f);                                              // freshly cut, moist
            mat.SetTexture("_BumpMap", WrinkleNormalMap());                                  // strength set per strip (0 = smooth)
            mat.SetFloat("_BumpScale", 0f);
            mat.EnableKeyword("_NORMALMAP");
            mesh.GetComponent<Renderer>().sharedMaterial = mat;
            var touch = root.AddComponent<BoxCollider>();                                 // finger-sized, not strip-sized
            touch.size = new Vector3(0.12f, 0.26f, 0.12f);
            var d = root.AddComponent<Draggable>();
            d.kind = ItemKind.PotatoStrip;
            var ctrl = root.AddComponent<PotatoStripController>();
            Set(ctrl, "mesh", mesh.transform);
            Set(ctrl, "meshRenderer", mesh.GetComponent<Renderer>());
            return SavePrefab(root, "PotatoStrip");
        }

        static Draggable BeakerPrefab()
        {
            var root = new GameObject("Beaker");
            var glass = Primitive(PrimitiveType.Cylinder, "Glass", root.transform, BeakerCenter, BeakerScale);
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            glass.GetComponent<Renderer>().sharedMaterial = Transparent("Glass", "Universal Render Pipeline/Lit", Hex("CFE8F5"), 0.28f);
            var liquid = Primitive(PrimitiveType.Cylinder, "Liquid", root.transform, new Vector3(0, 34f, 0) * UnitsPerMm, new Vector3(71f, 32f, 71f) * UnitsPerMm);   // ~64 mm deep
            Object.DestroyImmediate(liquid.GetComponent<Collider>());
            liquid.GetComponent<Renderer>().sharedMaterial = Transparent("Liquid", "Universal Render Pipeline/Lit", Hex("7EC8F0"), 0.55f);
            var touch = root.AddComponent<BoxCollider>();
            touch.center = BeakerCenter;
            touch.size = new Vector3(95f, 100f, 95f) * UnitsPerMm;
            var d = root.AddComponent<Draggable>();
            d.kind = ItemKind.Beaker;
            var inside = Target("StripTarget", root.transform, ItemKind.PotatoStrip, new Vector3(0, (3f + StripLengthMm / 2f) * UnitsPerMm, 0), Quaternion.identity, StripGhost);
            var bc = root.AddComponent<BeakerController>();
            Set(bc, "liquid", liquid.GetComponent<Renderer>());
            Set(bc, "stripTarget", inside);
            liquid.SetActive(true);                                                        // BeakerController.Awake hides it
            return SavePrefab(root, "Beaker");
        }

        /// Wrinkles running across the strip (ridges along its length), as a tangent-space normal map.
        static Texture2D WrinkleNormalMap()
        {
            const string path = "Assets/OsmosisPotato/Art/Materials/PotatoWrinkles.png";
            const int w = 64, h = 256;
            var heights = new float[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                heights[y * w + x] = Mathf.Sin(y * (2f * Mathf.PI * 6f / h) + 0.7f * Mathf.Sin(x * 0.1f))   // ~6 soft ridges
                                     * (0.7f + 0.3f * Mathf.PerlinNoise(x * 0.05f, y * 0.03f));
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = heights[y * w + (x + 1) % w] - heights[y * w + (x + w - 1) % w];
                float dy = heights[((y + 1) % h) * w + x] - heights[((y + h - 1) % h) * w + x];
                var n = new Vector3(-dx, -dy, 0.6f).normalized;
                px[y * w + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.NormalMap;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Vector3 StripScale => new Vector3(StripThickMm, StripLengthMm, StripThickMm) * UnitsPerMm;
        static void StripGhost(Transform t) => Ghost(PrimitiveType.Cube, t, Vector3.zero, StripScale * 1.15f);

        static void BeakerGhost(Transform t) =>
            Ghost(PrimitiveType.Cylinder, t, BeakerCenter, BeakerScale);

        // 250 ml beaker: 79 mm wide, 93 mm tall (cylinder primitive is 2 units tall at scale 1, so half-height in Y)
        static Vector3 BeakerScale => new Vector3(79f, 46.5f, 79f) * UnitsPerMm;
        static Vector3 BeakerCenter => new Vector3(0, 46.5f, 0) * UnitsPerMm;

        static void Ghost(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale)
        {
            var g = Primitive(type, "Ghost", parent, pos, scale);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = Transparent("Ghost", "Universal Render Pipeline/Unlit", Green, 0.55f);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.enabled = false;                                   // SnapTarget.Show() turns it on
        }

        static SnapTarget Target(string name, Transform parent, ItemKind kind, Vector3 pos, Quaternion rot, System.Action<Transform> ghost)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            ghost(go.transform);
            var t = go.AddComponent<SnapTarget>();
            t.accepts = kind;
            Set(t, "ghost", go.GetComponentInChildren<Renderer>());
            return t;
        }

        /// Flat ruler with a generated mm/cm tick texture and TMP cm numbers.
        static Transform rulerGroup;
        static float RulerZeroX => RulerPos.x - RulerLength / 2f + RulerMargin;

        static void Ruler(Transform world)
        {
            rulerGroup = new GameObject("RulerGroup").transform;                       // MeasurementTool slides this
            rulerGroup.SetParent(world, false);
            world = rulerGroup;
            var r = Primitive(PrimitiveType.Cube, "Ruler", world, RulerPos, new Vector3(RulerLength, 0.006f, RulerWidth));
            Object.DestroyImmediate(r.GetComponent<Collider>());
            r.GetComponent<Renderer>().sharedMaterial = Mat("Ruler", "Universal Render Pipeline/Unlit", RulerTexture(), Color.white);
            float zeroX = RulerZeroX;
            for (int cm = 0; cm <= 15; cm++)
            {
                var n = new GameObject("cm" + cm);
                n.transform.SetParent(world, false);
                n.transform.position = new Vector3(zeroX + cm * 10f * UnitsPerMm, RulerPos.y + 0.004f, RulerPos.z - RulerWidth * 0.03f);
                n.transform.rotation = Quaternion.Euler(90, 0, 0);
                var t = n.AddComponent<TextMeshPro>();
                ((RectTransform)n.transform).sizeDelta = new Vector2(0.05f, 0.03f);
                t.text = cm.ToString(); t.fontSize = 0.2f; t.color = Ink;
                t.alignment = TextAlignmentOptions.Center;
            }
        }

        static Texture2D RulerTexture()
        {
            const string path = "Assets/OsmosisPotato/Art/UI/RulerTicks.png";
            {
                const int w = 1440, h = 160;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color32[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(250, 246, 225, 255);   // pale yellow ruler
                float pxPerMm = w * (RulerLength - 2 * RulerMargin) / RulerLength / 150f;
                float zero = w * RulerMargin / RulerLength;
                for (int mm = 0; mm <= 150; mm++)
                {
                    int x = Mathf.RoundToInt(zero + mm * pxPerMm);
                    int len = mm % 10 == 0 ? 50 : mm % 5 == 0 ? 36 : 22;
                    for (int dx = -1; dx <= 1; dx++)
                    for (int y = h - len; y < h; y++)                                             // ticks hang from the far edge
                        if (x + dx >= 0 && x + dx < w) px[y * w + x + dx] = new Color32(30, 30, 30, 255);
                }
                tex.SetPixels32(px);
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.mipmapEnabled = true; ti.anisoLevel = 4; ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// Shelf icon: a brown potato with eyes and a pale cut strip in front, so it never reads as a wooden stick.
        static Sprite PotatoIcon()
        {
            const string path = "Assets/OsmosisPotato/Art/UI/PotatoIcon.png";
            const int n = 256;
            var px = new Color32[n * n];
            Color32 skin = new Color32(176, 124, 70, 255), skinDark = new Color32(120, 80, 42, 255),
                    flesh = new Color32(250, 238, 192, 255), edge = new Color32(214, 186, 112, 255);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // potato: tilted ellipse, upper-left, simple top-light shading
                float u = (x - 108f) / 92f, v = (y - 150f) / 70f;
                float ru = u * 0.94f + v * 0.34f, rv = -u * 0.34f + v * 0.94f;
                float d = ru * ru + rv * rv;
                if (d < 1f)
                {
                    float shade = Mathf.Lerp(1.08f, 0.78f, Mathf.Clamp01(0.5f - v * 0.5f + d * 0.3f));
                    px[y * n + x] = Scale(skin, shade);
                }
                // eyes
                foreach (var e in new[] { new Vector2(70, 170), new Vector2(120, 196), new Vector2(150, 140), new Vector2(92, 128) })
                    if ((new Vector2(x, y) - e).sqrMagnitude < 30f) px[y * n + x] = skinDark;
                // cut strip: upright bar, lower-right, with a darker outline
                if (x >= 150 && x <= 194 && y >= 22 && y <= 214)
                    px[y * n + x] = (x < 156 || x > 188 || y < 28 || y > 208) ? edge : flesh;
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color32 Scale(Color32 c, float k) =>
            new Color32((byte)Mathf.Min(255, c.r * k), (byte)Mathf.Min(255, c.g * k), (byte)Mathf.Min(255, c.b * k), c.a);

        static Material Transparent(string name, string shader, Color color, float alpha)
        {
            color.a = alpha;
            var m = Mat(name, shader, null, color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Draggable SavePrefab(GameObject root, string name)
        {
            var p = PrefabUtility.SaveAsPrefabAsset(root, "Assets/OsmosisPotato/Prefabs/Lab/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return p.GetComponent<Draggable>();
        }

        // ---------------- beaker (4.4) ----------------

        static ParticleSystem SplashPrefab()
        {
            var go = new GameObject("Splash");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = false; main.duration = 0.4f;
            main.startLifetime = 0.45f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.012f);
            main.gravityModifier = 0.6f; main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 30f; shape.radius = 0.02f;
            go.transform.rotation = Quaternion.Euler(-90, 0, 0);                            // spray upwards
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = ParticleMat();
            var p = PrefabUtility.SaveAsPrefabAsset(go, "Assets/OsmosisPotato/Prefabs/Lab/Splash.prefab");
            Object.DestroyImmediate(go);
            return p.GetComponent<ParticleSystem>();
        }

        /// Soft round dots: transparent particle material using the UI kit's rounded sprite as the dot texture.
        static Material ParticleMat()
        {
            var m = Transparent("Particle", "Universal Render Pipeline/Particles/Unlit", Color.white, 1f);
            m.SetTexture("_BaseMap", RoundedSprite().texture);
            return m;
        }

        /// Bottom-centre measuring card + numpad (task 4.7). Display box reopens the numpad; Record checks the value.
        static void MeasureUI(Transform canvas)
        {
            var card = Img("MeasurePanel", canvas, rounded, Green, 640, 250);
            Anchor(card, new Vector2(0.5f, 0f), new Vector2(-120f, 150f));
            Stretch(Img("Inner", card.transform, rounded, Color.white, 0, 0), 6);
            var title = Label("Title", card.transform, "Initial length of the strip", 36, Green, 600, 50);
            Pos(title.gameObject, 0, 85);

            var box = Img("Display", card.transform, rounded, Hex("B8C2C9"), 260, 92);
            Pos(box, -120, 10);
            Stretch(Img("Inner", box.transform, rounded, Color.white, 0, 0), 4);
            var display = Label("Value", box.transform, "", 56, Ink, 240, 88);
            Pos(Label("Unit", card.transform, "mm", 40, Ink, 80, 60).gameObject, 50, 10);
            Pos(Label("Hint", card.transform, "Type mm, or cm with a decimal (e.g. 4.9)", 22, Hex("6B7780"), 600, 30).gameObject, 0, -48);
            var record = SmallButton("RecordBtn", card.transform, "Record", 200, 10);
            ((RectTransform)record.transform).sizeDelta = new Vector2(170, 84);
            record.GetComponent<Image>().pixelsPerUnitMultiplier = 3.2f;
            var msg = Label("Message", card.transform, "", 24, Red, 600, 40);
            Pos(msg.gameObject, 0, -88);

            var numpad = SceneBuilder.Instance("UI/Numpad", canvas).GetComponent<Numpad>();
            UnityEventTools.AddObjectPersistentListener<TMP_Text>(box.AddComponent<SfxButton>().onClick, numpad.Open, display);

            var tool = new GameObject("Measurement").AddComponent<MeasurementTool>();
            Set(tool, "rulerGroup", rulerGroup);
            var so = new SerializedObject(tool);
            so.FindProperty("zeroX").floatValue = RulerZeroX;
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(tool, "panel", card);
            Set(tool, "title", title);
            Set(tool, "display", display);
            Set(tool, "message", msg);
            Set(tool, "numpad", numpad);
            UnityEventTools.AddPersistentListener(record.onClick, tool.Record);
            card.SetActive(false);
        }

        // ---------------- readings (M5) ----------------

        static readonly string[] Headers = { "Sugar\n(%)", "Initial\n(mm)", "Final\n(mm)", "Change\n(mm)", "Change\n(%)" };

        /// Readings window: table (left), graph on its own canvas (right), isotonic question (bottom); plus the result card.
        static void ReadingsUI(Transform canvas, GameObject menu)
        {
            var (root, card) = Popup("ReadingsWindow", 1700, 940);
            root.transform.SetParent(canvas, false);
            Stretch(root);
            var view = root.AddComponent<ReadingsView>();
            Set(view, "popup", root.GetComponent<PopupPanel>());

            var title = Label("Title", card.transform, "Readings", 60, Green, 500, 80);
            title.alignment = TextAlignmentOptions.Left;
            Pos(title.gameObject, -560, 405);
            var close = Img("CloseBtn", card.transform, Spr("Sprites/close.png"), Color.white, 80, 80);
            Anchor(close, new Vector2(1, 1), new Vector2(-60, -60));
            UnityEventTools.AddPersistentListener(close.AddComponent<SfxButton>().onClick, view.Close);

            // table
            float[] w = { 150, 150, 150, 160, 160 };
            float x0 = -800f;
            var cells = new Object[LabFlow.Strips * 5];
            var rowBgs = new Object[LabFlow.Strips];
            for (int row = -1; row < LabFlow.Strips; row++)
            {
                float y = row < 0 ? 300 : 205 - row * 90;
                var bg = Img(row < 0 ? "Header" : "Row" + (row + 1), card.transform, rounded, row < 0 ? Green : Color.white, 770, row < 0 ? 96 : 84);
                bg.GetComponent<Image>().pixelsPerUnitMultiplier = 4f;
                Pos(bg, x0 + 385, y);
                if (row >= 0) { var line = Img("Line", bg.transform, null, Hex("D5DADF"), 770, 2); Pos(line, 0, -42); rowBgs[row] = bg.GetComponent<Image>(); }
                float x = x0;
                for (int c = 0; c < 5; c++)
                {
                    var t = Label("C" + c, bg.transform, row < 0 ? Headers[c] : "", row < 0 ? 28 : 38, row < 0 ? Color.white : Ink, w[c], 84);
                    Pos(t.gameObject, x + w[c] / 2 - (x0 + 385), 0);
                    if (row >= 0) cells[row * 5 + c] = t;
                    x += w[c];
                }
            }
            Set(view, "cells", cells);
            Set(view, "rowBackgrounds", rowBgs);

            // graph (own canvas)
            var frame = Img("GraphFrame", card.transform, rounded, Hex("F4F6F7"), 800, 520);
            Pos(frame, 410, 105);
            var g = new GameObject("Graph", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(GraphicRaycaster));   // own canvas + raycaster so the slider inside gets touches
            g.transform.SetParent(frame.transform, false);
            ((RectTransform)g.transform).sizeDelta = new Vector2(640, 400);
            Pos(g, 40, 15);
            var graph = g.AddComponent<GraphView>();
            graph.raycastTarget = false;
            var yl = new Object[5];
            for (int i = 0; i < 5; i++)
            {
                var t = Label("Y" + i, frame.transform, "", 24, Ink, 70, 30);
                Pos(t.gameObject, 40 - 320 - 45, 15 + 200 - 8 - i * (392f / 4f));
                yl[i] = t;
            }
            Set(graph, "yLabels", yl);
            for (int x = 0; x <= 50; x += 10)
                Pos(Label("X" + x, frame.transform, x.ToString(), 24, Ink, 60, 30).gameObject, 40 - 320 + 8 + x / 50f * 624f, 15 - 200 - 22);
            Pos(Label("XTitle", frame.transform, "Sugar concentration (%)", 28, Green, 600, 36).gameObject, 40, -235);
            var yt = Label("YTitle", frame.transform, "Change in length (mm)", 28, Green, 400, 36);
            yt.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Pos(yt.gameObject, -375, 15);
            Set(view, "graph", graph);

            // isotonic question: drag a full-height line across the graph to where the data crosses zero
            var sliderGo = new GameObject("IsotonicSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(g.transform, false);
            Stretch(sliderGo);
            var hitArea = sliderGo.AddComponent<Image>();
            hitArea.color = new Color(1, 1, 1, 0);                                   // invisible, catches drags anywhere on the graph
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            Stretch(handleArea, GraphView.Pad);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hrt = (RectTransform)handle.transform;
            hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(0, 1); hrt.sizeDelta = new Vector2(44, 0);
            handle.GetComponent<Image>().color = new Color(1, 1, 1, 0);              // wide invisible grip
            var bar = Img("Line", handle.transform, null, Hex("4A6FA5"), 4, 0);
            var brt = (RectTransform)bar.transform; brt.anchorMin = new Vector2(0.5f, 0); brt.anchorMax = new Vector2(0.5f, 1); brt.sizeDelta = new Vector2(4, 0);
            var knob = Img("Knob", handle.transform, rounded, Hex("4A6FA5"), 40, 40);
            knob.GetComponent<Image>().pixelsPerUnitMultiplier = 2.4f;
            Anchor(knob, new Vector2(0.5f, 0f), new Vector2(0, -18));
            var slider = sliderGo.AddComponent<Slider>();
            slider.handleRect = hrt; slider.targetGraphic = knob.GetComponent<Image>();
            slider.minValue = 0; slider.maxValue = OsmosisSolver.MaxPercent; slider.wholeNumbers = false;
            slider.transition = Selectable.Transition.None;
            sliderGo.SetActive(false);

            var q = new GameObject("Question", typeof(RectTransform));
            q.transform.SetParent(card.transform, false);
            var qt = Label("Text", q.transform, "Drag the blue line on the graph to where it crosses zero.\nIsotonic point =", 34, Ink, 1100, 100);
            qt.alignment = TextAlignmentOptions.Right;
            Pos(qt.gameObject, -350, -320);
            var guessLabel = Label("Guess", q.transform, "25.0 %", 48, Hex("4A6FA5"), 220, 70);
            Pos(guessLabel.gameObject, 330, -340);
            var check = SmallButton("CheckBtn", q.transform, "Check", 560, -330);
            ((RectTransform)check.transform).sizeDelta = new Vector2(200, 90);
            check.GetComponent<Image>().pixelsPerUnitMultiplier = 3.2f;
            var msg = Label("Message", card.transform, "", 28, Red, 1500, 40);
            Pos(msg.gameObject, 0, -420);
            UnityEventTools.AddPersistentListener(check.onClick, view.CheckIsotonic);
            q.SetActive(false);
            Set(view, "question", q);
            Set(view, "guess", slider);
            Set(view, "guessLabel", guessLabel);
            Set(view, "message", msg);
            Set(view, "check", check);

            var result = SceneBuilder.Instance("UI/ResultCard", canvas);
            Set(view, "resultCard", result.GetComponent<PopupPanel>());
            Set(view, "resultBody", result.transform.Find("Card/Body").GetComponent<TMP_Text>());

            var readingsBtn = menu.transform.Find("ReadingsBtn");
            UnityEventTools.AddPersistentListener(readingsBtn.GetComponent<Button>().onClick, view.Open);
            Set(view, "badge", readingsBtn.GetComponent<ReadingsBadge>());
            var pulse = readingsBtn.gameObject.AddComponent<PulseScale>();
            pulse.enabled = false;
            Set(view, "buttonPulse", pulse);
        }

        /// Tiny looping particles for water molecules; colour/direction/shape set per use by Vfx.Flow.
        static ParticleSystem FlowPrefab()
        {
            var go = new GameObject("WaterFlow");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.duration = 1f;
            main.startLifetime = 0.8f; main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.01f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 40f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = vel.y = vel.z = new ParticleSystem.MinMaxCurve(0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            var p = PrefabUtility.SaveAsPrefabAsset(go, "Assets/OsmosisPotato/Prefabs/Lab/WaterFlow.prefab");
            Object.DestroyImmediate(go);
            return p.GetComponent<ParticleSystem>();
        }

        /// Fast Forward (reference fast_button.png) bottom-right, wired to a TimeLapseManager; hidden until a strip is soaking.
        static void FastForwardUI(Transform canvas)
        {
            var btn = Img("FastForwardBtn", canvas, Spr("ART/fast_button.png"), Color.white, 170, 170);
            btn.GetComponent<Image>().preserveAspect = true;
            Anchor(btn, new Vector2(1f, 0f), new Vector2(-130f, 140f));
            var label = Label("Text", btn.transform, "Fast Forward", 28, Ink, 220, 36);
            Pos(label.gameObject, 0, -95);
            var button = btn.AddComponent<SfxButton>();
            var tl = new GameObject("TimeLapse").AddComponent<TimeLapseManager>();
            Set(tl, "timer", Object.FindFirstObjectByType<CountdownTimer>());
            Set(tl, "fastForward", button);
            UnityEventTools.AddPersistentListener(button.onClick, tl.FastForward);
            btn.SetActive(false);
        }

        /// Bottom-left card: "Sugar solution" title, big % value, 0–50 % slider (5 % steps), Fill pill, warning line.
        static void ConcentrationPanelUI(Transform canvas)
        {
            var card = Img("ConcentrationPanel", canvas, rounded, Green, 600, 300);
            Anchor(card, new Vector2(0f, 0f), new Vector2(330f, 190f));
            Stretch(Img("Inner", card.transform, rounded, Color.white, 0, 0), 6);
            Pos(Label("Title", card.transform, "Sugar solution", 38, Green, 360, 50).gameObject, -90, 105);
            var value = Label("Value", card.transform, "0 %", 54, Ink, 200, 64);
            Pos(value.gameObject, 180, 105);

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(card.transform, false);
            ((RectTransform)sliderGo.transform).sizeDelta = new Vector2(500, 44);
            Pos(sliderGo, 0, 25);
            var bg = Img("Background", sliderGo.transform, rounded, Hex("DADFE3"), 0, 0);
            Stretch(bg); ((RectTransform)bg.transform).offsetMin = new Vector2(0, 12); ((RectTransform)bg.transform).offsetMax = new Vector2(0, -12);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            Stretch(fillArea); ((RectTransform)fillArea.transform).offsetMin = new Vector2(0, 12); ((RectTransform)fillArea.transform).offsetMax = new Vector2(0, -12);
            var fill = Img("Fill", fillArea.transform, rounded, Green, 0, 0);
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            Stretch(handleArea); ((RectTransform)handleArea.transform).offsetMin = new Vector2(22, 0); ((RectTransform)handleArea.transform).offsetMax = new Vector2(-22, 0);
            var handle = Img("Handle", handleArea.transform, rounded, DarkGreen, 56, 0);
            handle.GetComponent<Image>().pixelsPerUnitMultiplier = 0.85f;                   // round knob
            var hrt = (RectTransform)handle.transform; hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(0, 1); hrt.sizeDelta = new Vector2(56, 12);
            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = hrt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0; slider.maxValue = OsmosisSolver.MaxPercent / ConcentrationPanel.Step; slider.wholeNumbers = true;
            Pos(Label("Min", card.transform, "0 %", 24, Ink, 80, 30).gameObject, -250, -12);
            Pos(Label("Max", card.transform, "50 %", 24, Ink, 80, 30).gameObject, 250, -12);

            var fillBtn = SmallButton("FillBtn", card.transform, "Fill beaker", 0, -75);
            ((RectTransform)fillBtn.transform).sizeDelta = new Vector2(300, 76);
            Stretch(fillBtn.transform.Find("Text").gameObject);
            fillBtn.GetComponent<Image>().pixelsPerUnitMultiplier = 3.2f;
            var msg = Label("Message", card.transform, "", 24, Red, 560, 36);
            Pos(msg.gameObject, 0, -128);

            var panel = card.AddComponent<ConcentrationPanel>();
            Set(panel, "slider", slider);
            Set(panel, "valueLabel", value);
            Set(panel, "message", msg);
            UnityEventTools.AddPersistentListener(fillBtn.onClick, panel.Fill);
            card.SetActive(false);
        }

        // ---------------- flow (4.8) ----------------

        static void Flow()
        {
            var flow = new GameObject("LabFlow").AddComponent<LabFlow>();
            Set(flow, "board", Find<InstructionBoard>("BoardText"));
            Set(flow, "potatoSlot", Find<InventorySlot>("PotatoSlot"));
            Set(flow, "beakerSlot", Find<InventorySlot>("BeakerSlot"));
            Set(flow, "rulerTarget", Find<SnapTarget>("RulerTarget"));
            Set(flow, "beakerTargets", new Object[] { Find<SnapTarget>("BeakerTarget1"), Find<SnapTarget>("BeakerTarget2"), Find<SnapTarget>("BeakerTarget3") });
            Set(flow, "panel", Find<ConcentrationPanel>("ConcentrationPanel"));
            Set(flow, "timeLapse", Find<TimeLapseManager>("TimeLapse"));
            Set(flow, "measure", Object.FindFirstObjectByType<MeasurementTool>());
            Set(flow, "feedbackCorrect", Find<Transform>("FeedbackCorrect").gameObject);
            var readings = Object.FindFirstObjectByType<ReadingsView>(FindObjectsInactive.Include);
            Set(readings, "flow", flow);
            Set(flow, "readingsPulse", Find<PulseScale>("ReadingsBtn"));

            var m = Object.FindFirstObjectByType<MeasurementTool>();                     // HUD is built; finish wiring
            Set(m, "badge", Object.FindFirstObjectByType<ReadingsBadge>(FindObjectsInactive.Include));
            Set(m, "feedbackWrong", Find<Transform>("FeedbackWrong").gameObject);
        }

        static T Find<T>(string name) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == name) return c;
            throw new System.Exception("[LabBuilder] " + name + " not found");
        }

        // ---------------- helpers ----------------

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go;
        }

        static Texture2D Tex(string path, bool readable = false, bool repeat = false, int maxSize = 0)
        {
            string full = "Assets/OsmosisPotato/Reference/" + path;
            var ti = (TextureImporter)AssetImporter.GetAtPath(full);
            bool wrongType = ti.textureType == TextureImporterType.NormalMap;   // some reference art was imported as normal maps
            if (wrongType || (readable && !ti.isReadable) || (repeat && ti.wrapMode != TextureWrapMode.Repeat) || (maxSize > 0 && ti.maxTextureSize != maxSize))
            {
                if (wrongType) { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; }
                if (readable) ti.isReadable = true;
                if (repeat) ti.wrapMode = TextureWrapMode.Repeat;
                if (maxSize > 0) ti.maxTextureSize = maxSize;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(full);
        }

        /// Creates (or updates) a material asset so scenes reference saved materials, not scene-only instances.
        static Material Mat(string name, string shader, Texture2D tex, Color color, Vector2? tiling = null)
        {
            System.IO.Directory.CreateDirectory(MatDir);
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find(shader);
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            m.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
