using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Osmosis.Tests
{
    /// Catches broken prefab wiring (missing scripts, unset references, unhooked buttons)
    /// after UIKitBuilder re-runs or someone edits a prefab by hand.
    public class UIKitPrefabTests
    {
        static GameObject Load(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/" + name + ".prefab");
            Assert.IsNotNull(go, name + " prefab missing");
            return go;
        }

        static void AssertRefsSet(Component c)
        {
            var it = new SerializedObject(c).GetIterator();
            while (it.NextVisible(true))
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.name != "m_Script")
                    Assert.IsTrue(it.objectReferenceValue != null || it.objectReferenceInstanceIDValue == 0,
                        $"{c.GetType().Name}.{it.propertyPath} points at a missing object");
        }

        [TestCase("UI/PillButton"), TestCase("UI/ConfirmDialog"), TestCase("UI/StudyWindow"),
         TestCase("UI/TopLeftMenu"), TestCase("UI/ResultCard"), TestCase("UI/Numpad"), TestCase("SFX")]
        public void NoMissingScriptsOrReferences(string name)
        {
            var go = Load(name);
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),
                    $"missing script on {t.name}");
            foreach (var c in go.GetComponentsInChildren<MonoBehaviour>(true)) AssertRefsSet(c);
        }

        [Test]
        public void SfxHasEveryNamedSoundWithAClip()
        {
            var sfx = Load("SFX").GetComponent<Sfx>();
            foreach (var n in new[] { Sfx.Click, Sfx.Inventory, Sfx.Snap, Sfx.Correct, Sfx.Wrong, Sfx.Typing,
                                      Sfx.Notification, Sfx.ClockTicking, Sfx.Pour, Sfx.Music })
            {
                var s = System.Array.Find(sfx.sounds, x => x.name == n);
                Assert.IsNotNull(s, n + " not in SFX prefab");
                Assert.IsNotNull(s.clip, n + " has no clip");
            }
        }

        [TestCase("UI/ConfirmDialog", "YesBtn", "ToMenu")]
        [TestCase("UI/ConfirmDialog", "NoBtn", "Close")]
        [TestCase("UI/ResultCard", "AgainBtn", "Again")]
        [TestCase("UI/ResultCard", "QuitBtn", "ToMenu")]
        [TestCase("UI/StudyWindow", "CloseBtn", "Close")]
        [TestCase("UI/StudyWindow", "ProcedureTab", "Select")]
        public void ButtonIsWired(string prefab, string button, string method)
        {
            Button b = null;
            foreach (var x in Load(prefab).GetComponentsInChildren<Button>(true)) if (x.name == button) b = x;
            Assert.IsNotNull(b, button + " not found");
            Assert.IsInstanceOf<SfxButton>(b, "buttons must be SfxButton (click sound)");
            Assert.AreEqual(1, b.onClick.GetPersistentEventCount());
            Assert.AreEqual(method, b.onClick.GetPersistentMethodName(0));
            Assert.IsNotNull(b.onClick.GetPersistentTarget(0));
        }

        [TestCase("UI/ConfirmDialog"), TestCase("UI/StudyWindow"), TestCase("UI/ResultCard")]
        public void PopupsStartHidden(string name) => Assert.IsFalse(Load(name).activeSelf);
    }
}
