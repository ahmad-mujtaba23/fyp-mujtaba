using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Osmosis.Tests
{
    /// End-to-end scene flow (task 2.5): Menu -> Guidance -> Lab -> Home -> Menu, plus Again and Settings.
    public class SceneFlowTests
    {
        static IEnumerator WaitForScene(string name, float timeout = 5f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(name, SceneManager.GetActiveScene().name, "scene did not load in time");
            yield return new WaitForSecondsRealtime(0.5f);          // let the fade-in finish
        }

        static void Click(string button)
        {
            var go = GameObject.Find(button);
            Assert.IsNotNull(go, button + " not found (inactive or missing)");
            go.GetComponent<Button>().onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator FullLoopMenuGuidanceLabMenu()
        {
            SceneManager.LoadScene(Scenes.Menu);
            yield return WaitForScene(Scenes.Menu);
            Sfx.Stop(Sfx.Music);

            Click("PLAYBtn");
            yield return WaitForScene(Scenes.Guidance);
            Assert.IsTrue(Sfx.IsPlaying(Sfx.Music), "menu music starts on Play");

            var next = GameObject.Find("NextBtn").GetComponent<Button>();
            for (int slide = 0; slide < 4; slide++)                 // read each slide: Next unlocks after a delay
            {
                Assert.IsNull(GameObject.Find("GotItBtn"), "Got it only on the last slide");
                float end = Time.realtimeSinceStartup + 5f;
                while (!next.interactable && Time.realtimeSinceStartup < end) yield return null;
                Assert.IsTrue(next.interactable, "Next never unlocked");
                next.onClick.Invoke();
            }
            Click("GotItBtn");
            yield return WaitForScene(Scenes.Lab);
            Assert.IsFalse(Sfx.IsPlaying(Sfx.Music), "music stops in the lab");

            Click("HomeBtn");                                       // opens exit dialog
            yield return new WaitForSecondsRealtime(0.4f);
            Click("YesBtn");
            yield return WaitForScene(Scenes.Menu);

            var fade = GameObject.Find("[SceneLoader]").GetComponent<CanvasGroup>();
            Assert.AreEqual(0f, fade.alpha, 0.01f, "fade finished");
            Assert.IsFalse(fade.blocksRaycasts, "fade no longer blocks input");
        }

        [UnityTest]
        public IEnumerator LabTypesFirstStepInsideTheBlackboard()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return WaitForScene(Scenes.Lab);
            var t = GameObject.Find("BoardText").GetComponent<TMPro.TMP_Text>();
            yield return new WaitForSeconds(3.5f);                    // typing finishes
            StringAssert.Contains("Take a potato strip", t.text);
            Assert.AreEqual(t.textInfo.characterCount, t.maxVisibleCharacters, "fully typed");

            // every glyph must lie inside the board text box (which is fitted inside the chalkboard frame)
            var rt = (RectTransform)t.transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var b = t.textBounds;                                      // local space
            Vector3 min = t.transform.TransformPoint(b.min), max = t.transform.TransformPoint(b.max);
            Assert.GreaterOrEqual(min.x, corners[0].x - 1e-3f); Assert.LessOrEqual(max.x, corners[2].x + 1e-3f);
            Assert.GreaterOrEqual(min.y, corners[0].y - 1e-3f); Assert.LessOrEqual(max.y, corners[2].y + 1e-3f);
            Assert.AreEqual((corners[0].y + corners[2].y) / 2f, (min.y + max.y) / 2f, 0.02f, "vertically centred on the board");

            // and the text box is in front of the backdrop's dark chalkboard: sample the backdrop texture under it
            var backdrop = GameObject.Find("Backdrop");
            var tex = (Texture2D)backdrop.GetComponent<Renderer>().sharedMaterial.mainTexture;
            Vector3 local = backdrop.transform.InverseTransformPoint(rt.position);
            var c = tex.GetPixelBilinear(local.x + 0.5f, local.y + 0.5f);
            Assert.Less(c.r + c.g + c.b, 0.9f, "text centre is over the dark board, not the wall");
        }

        [UnityTest]
        public IEnumerator AgainReloadsTheLabNotTheMenu()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return WaitForScene(Scenes.Lab);
            var before = SceneManager.GetActiveScene().handle;
            Object.FindFirstObjectByType<MenuActions>().Again();
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(Scenes.Lab, SceneManager.GetActiveScene().name);
            Assert.AreNotEqual(before, SceneManager.GetActiveScene().handle, "lab was reloaded");
        }

        [UnityTest]
        public IEnumerator SettingsMuteMusicAndPersist()
        {
            bool music = Sfx.MusicOn, fx = Sfx.EffectsOn;
            try
            {
                SceneManager.LoadScene(Scenes.Menu);
                yield return WaitForScene(Scenes.Menu);
                Click("SETTINGSBtn");
                yield return new WaitForSecondsRealtime(0.4f);
                Sfx.MusicOn = true;
                Click("MusicBtn");
                Assert.IsFalse(Sfx.MusicOn);
                Assert.IsTrue(Sfx.IsMuted(Sfx.Music));
                Assert.AreEqual("Music: OFF", GameObject.Find("MusicBtn").GetComponentInChildren<TMPro.TMP_Text>().text);
                Assert.AreEqual(0, PlayerPrefs.GetInt("osmosis.music_on"), "persisted");
                Click("MusicBtn");
                Assert.IsFalse(Sfx.IsMuted(Sfx.Music));
            }
            finally { Sfx.MusicOn = music; Sfx.EffectsOn = fx; }
        }
    }
}
