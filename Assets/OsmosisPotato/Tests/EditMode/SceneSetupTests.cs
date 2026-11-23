using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Osmosis.Tests
{
    /// Static checks on the built scenes: build order, input module, no missing scripts, every button wired.
    public class SceneSetupTests
    {
        static readonly string[] Order = { Scenes.Menu, Scenes.Guidance, Scenes.Lab };

        static Scene Open(string name) =>
            EditorSceneManager.OpenScene("Assets/OsmosisPotato/Scenes/" + name + ".unity", OpenSceneMode.Additive);

        static T[] All<T>(Scene s) where T : Component =>
            s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        static Button Btn(Scene s, string name) => All<Button>(s).Single(b => b.name == name);

        [Test]
        public void BuildListIsMenuGuidanceLab()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(3, scenes.Length);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(scenes[i].enabled);
                StringAssert.EndsWith(Order[i] + ".unity", scenes[i].path);
            }
        }

        [TestCase(Scenes.Menu), TestCase(Scenes.Guidance), TestCase(Scenes.Lab)]
        public void SceneIsSoundAndWired(string name)
        {
            var s = Open(name);
            try
            {
                var es = All<EventSystem>(s);
                Assert.AreEqual(1, es.Length, "one EventSystem");
                Assert.IsNotNull(es[0].GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(), "new Input System UI module");
                Assert.IsNull(es[0].GetComponent<StandaloneInputModule>(), "no legacy input module");

                foreach (var t in All<Transform>(s))
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), "missing script on " + t.name);

                foreach (var b in All<Button>(s))
                {
                    Assert.Greater(b.onClick.GetPersistentEventCount(), 0, b.name + " does nothing");
                    Assert.IsInstanceOf<SfxButton>(b, b.name + " should be SfxButton (click sound)");
                }

                var scaler = All<CanvasScaler>(s).First();
                Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution);
            }
            finally { EditorSceneManager.CloseScene(s, true); }
        }

        [Test]
        public void MenuButtonsDoTheRightThing()
        {
            var s = Open(Scenes.Menu);
            try
            {
                Assert.AreEqual("Play", Btn(s, "PLAYBtn").onClick.GetPersistentMethodName(0));
                Assert.AreEqual("Open", Btn(s, "STUDYBtn").onClick.GetPersistentMethodName(0));
                Assert.AreEqual("Open", Btn(s, "SETTINGSBtn").onClick.GetPersistentMethodName(0));
                Assert.AreEqual("Quit", Btn(s, "QUITBtn").onClick.GetPersistentMethodName(0));
                Assert.AreEqual(1, All<Sfx>(s).Length, "SFX bank lives in the Menu scene");
            }
            finally { EditorSceneManager.CloseScene(s, true); }
        }

        [Test]
        public void LabEnvironmentAndHud()
        {
            var s = Open(Scenes.Lab);
            try
            {
                var cam = All<Camera>(s).Single();
                Assert.IsFalse(cam.orthographic, "3D lab");
                Assert.AreEqual("MainCamera", cam.tag);

                foreach (var r in All<Renderer>(s))
                    Assert.IsFalse(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(r.sharedMaterial)),
                        r.name + " uses a scene-only material");

                var board = All<InstructionBoard>(s).Single();
                AssertNoNullRefs(board);
                var timer = All<CountdownTimer>(s).Single();
                AssertNoNullRefs(timer);
                Assert.AreEqual("Again", Btn(s, "ResetBtn").onClick.GetPersistentMethodName(0));

                foreach (var slot in new[] { "PotatoSlot", "BeakerSlot" })
                {
                    var t = All<RectTransform>(s).Single(x => x.name == slot);
                    Assert.IsNotNull(t.Find("Icon")); Assert.IsNotNull(t.Find("Lock")); Assert.IsNotNull(t.Find("Tick"));
                    Assert.IsFalse(t.Find("Tick").gameObject.activeSelf, "tick hidden until placed");
                }
                foreach (var f in new[] { "FeedbackCorrect", "FeedbackWrong" })
                    Assert.IsFalse(All<RectTransform>(s).Single(x => x.name == f).gameObject.activeSelf, f + " hidden");
            }
            finally { EditorSceneManager.CloseScene(s, true); }
        }

        static void AssertNoNullRefs(Component c)
        {
            var it = new SerializedObject(c).GetIterator();
            while (it.NextVisible(true))
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.name != "m_Script")
                    Assert.IsNotNull(it.objectReferenceValue, c.GetType().Name + "." + it.propertyPath + " unset");
        }

        [Test]
        public void GuidanceHasFiveSlidesAndGotItStartsLab()
        {
            var s = Open(Scenes.Guidance);
            try
            {
                var show = All<SlideshowView>(s).Single();
                var slides = (GameObject[])typeof(SlideshowView).GetField("slides",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(show);
                Assert.AreEqual(5, slides.Length);
                Assert.IsTrue(slides.All(x => x != null));
                Assert.AreEqual("StartLab", Btn(s, "GotItBtn").onClick.GetPersistentMethodName(0));
                Assert.AreEqual("Open", Btn(s, "HomeBtn").onClick.GetPersistentMethodName(0));
            }
            finally { EditorSceneManager.CloseScene(s, true); }
        }
    }
}
