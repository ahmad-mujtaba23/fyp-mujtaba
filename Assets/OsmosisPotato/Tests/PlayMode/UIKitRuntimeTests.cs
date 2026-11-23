using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Osmosis.Tests
{
    /// Runtime behaviour of the M1 UI kit. Runs in the Editor's play mode (needs AssetDatabase for prefabs).
    public class UIKitRuntimeTests
    {
        Canvas canvas;

        [SetUp]
        public void SetUp()
        {
            canvas = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            EnsureSfx();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(canvas.gameObject);

        // ---------- helpers ----------

        static void EnsureSfx()
        {
            if (Object.FindFirstObjectByType<Sfx>()) return;
            Object.Instantiate(Prefab("SFX"));
        }

        static GameObject Prefab(string name)
        {
#if UNITY_EDITOR
            var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/" + name + ".prefab");
            Assert.IsNotNull(p, name);
            return p;
#else
            Assert.Ignore("editor-only test"); return null;
#endif
        }

        GameObject Spawn(string name)
        {
            var go = Object.Instantiate(Prefab(name), canvas.transform);
            return go;
        }

        static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        TextMeshProUGUI Text(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(800, 200);
            return go.AddComponent<TextMeshProUGUI>();
        }

        // ---------- core ----------

        [Test]
        public void BootSets60Fps() => Assert.AreEqual(60, Application.targetFrameRate);

        [UnityTest]
        public IEnumerator SfxPlaysStopsAndIgnoresUnknownNames()
        {
            yield return null;
            Sfx.Play(Sfx.Click);
            Assert.IsTrue(Sfx.IsPlaying(Sfx.Click));
            Sfx.Stop(Sfx.Click);
            Assert.IsFalse(Sfx.IsPlaying(Sfx.Click));
            Assert.DoesNotThrow(() => Sfx.Play("no_such_sound"));
        }

        [UnityTest]
        public IEnumerator SecondSfxDestroysItself()
        {
            var extra = Object.Instantiate(Prefab("SFX"));
            yield return null;
            Assert.IsTrue(extra == null, "duplicate SFX should self-destroy");
            Assert.AreEqual(1, Object.FindObjectsByType<Sfx>(FindObjectsSortMode.None).Length);
        }

        // ---------- popups ----------

        [UnityTest]
        public IEnumerator PopupSpringsOpenAndClosesHidden()
        {
            var dialog = Spawn("UI/ConfirmDialog");
            var pop = dialog.GetComponent<PopupPanel>();
            var group = dialog.GetComponent<CanvasGroup>();
            var card = dialog.transform.Find("Card");

            pop.Open();
            Assert.IsTrue(dialog.activeSelf);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(1f, group.alpha, 0.01f);
            Assert.AreEqual(1f, card.localScale.x, 0.01f);

            pop.Close();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(dialog.activeSelf);
        }

        [UnityTest]
        public IEnumerator ConfirmDialogNoButtonClosesWithClickSound()
        {
            var dialog = Spawn("UI/ConfirmDialog");
            dialog.GetComponent<PopupPanel>().Open();
            yield return new WaitForSecondsRealtime(0.4f);
            var no = dialog.transform.Find("Card/NoBtn").GetComponent<SfxButton>();
            no.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
            Assert.IsTrue(Sfx.IsPlaying(Sfx.Click), "SfxButton should play click");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(dialog.activeSelf);
        }

        [UnityTest]
        public IEnumerator StudyWindowTabsSwitchPagesAndSprites()
        {
            var win = Spawn("UI/StudyWindow");
            win.SetActive(true);                     // OnEnable -> Select(0)
            yield return null;
            var study = win.GetComponent<StudyWindow>();
            var pages = (ScrollRect[])typeof(StudyWindow).GetField("pages", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(study);
            var tabs = (Image[])typeof(StudyWindow).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(study);
            var open = (Sprite[])typeof(StudyWindow).GetField("tabOpen", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(study);

            Assert.IsTrue(pages[0].gameObject.activeSelf);
            Assert.IsFalse(pages[1].gameObject.activeSelf);
            study.Select(2);
            Assert.IsFalse(pages[0].gameObject.activeSelf);
            Assert.IsTrue(pages[2].gameObject.activeSelf);
            Assert.AreEqual(open[2], tabs[2].sprite);
            Assert.AreNotEqual(open[0], tabs[0].sprite);
        }

        [UnityTest]
        public IEnumerator NumpadKeysTypeIntoDisplayAndClose()
        {
            var pad = Spawn("UI/Numpad").GetComponent<Numpad>();
            var panel = pad.transform.Find("Panel").gameObject;
            var display = Text("Display");
            display.text = "";
            Assert.IsFalse(panel.activeSelf, "starts closed");

            pad.Open(display);
            Assert.IsTrue(panel.activeSelf);
            void Tap(string key) => panel.transform.Find("Key" + key).GetComponent<Button>().onClick.Invoke();
            Tap("5"); Tap("2"); Tap("."); Tap("5"); Tap("<"); Tap("7");
            Assert.AreEqual("52.7", display.text);
            Assert.AreEqual("52.7", pad.Value);

            panel.transform.Find("CloseBtn").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(panel.activeSelf);
            yield return null;
        }

        // ---------- lab widgets ----------

        [UnityTest]
        public IEnumerator CountdownRunsSimulatedTimeAndFinishesOnce()
        {
            var t = Text("Timer");
            var timer = t.gameObject.AddComponent<CountdownTimer>();
            Set(timer, "label", t);
            int finished = 0;
            timer.Finished += () => finished++;

            timer.Run(30 * 60, 0.5f);
            Assert.AreEqual("30:00", t.text);
            Assert.IsTrue(Sfx.IsPlaying(Sfx.ClockTicking));
            yield return new WaitForSeconds(0.25f);
            Assert.That(timer.Progress, Is.InRange(0.2f, 0.8f));
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1, finished);
            Assert.IsFalse(timer.Running);
            Assert.AreEqual(1f, timer.Progress);
            Assert.AreEqual("00:00", t.text);
            Assert.IsFalse(Sfx.IsPlaying(Sfx.ClockTicking));
        }

        [UnityTest]
        public IEnumerator InstructionBoardTypesWholeTextThenStopsTyping()
        {
            var t = Text("Board");
            var board = t.gameObject.AddComponent<InstructionBoard>();
            Set(board, "text", t);
            Set(board, "secondsPerChar", 0.01f);
            Set(board, "pulseSeconds", 0f);

            board.Show("First step");
            yield return null;
            Assert.Less(t.maxVisibleCharacters, 10);
            Assert.IsTrue(Sfx.IsPlaying(Sfx.Typing));

            board.Show("1. Take a potato strip.");   // restart mid-typing
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual("1. Take a potato strip.", t.text);
            Assert.AreEqual(t.textInfo.characterCount, t.maxVisibleCharacters);
            Assert.IsFalse(Sfx.IsPlaying(Sfx.Typing));
        }

        [UnityTest]
        public IEnumerator ReadingsBadgeCountsAndGlideIncrements()
        {
            var menu = Spawn("UI/TopLeftMenu");
            var badge = menu.GetComponentInChildren<ReadingsBadge>();
            var badgeGo = badge.transform.Find("Badge").gameObject;
            var count = badgeGo.GetComponentInChildren<TMP_Text>();
            yield return null;
            Assert.IsFalse(badgeGo.activeSelf, "badge hidden at 0");

            badge.Increment();
            badge.Increment();
            Assert.IsTrue(badgeGo.activeSelf);
            Assert.AreEqual("2", count.text);

            var label = Text("Reading");
            label.text = "Length: 50 mm";
            int before = canvas.transform.childCount;
            badge.Glide(label.rectTransform);
            Assert.AreEqual(before + 1, canvas.transform.childCount, "ghost label spawned");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual("3", count.text);
            Assert.AreEqual(before, canvas.transform.childCount, "ghost label destroyed");

            badge.Clear();
            Assert.IsFalse(badgeGo.activeSelf);
        }

        [UnityTest]
        public IEnumerator SlideshowGatesNextAndShowsGotItOnLast()
        {
            var root = new GameObject("Slides", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            root.SetActive(false);                     // wire before Start runs
            var show = root.AddComponent<SlideshowView>();
            var slides = new GameObject[3];
            for (int i = 0; i < 3; i++) { slides[i] = new GameObject("S" + i); slides[i].transform.SetParent(root.transform); }
            Button Btn(string n) { var b = new GameObject(n, typeof(Image), typeof(Button)); b.transform.SetParent(root.transform); return b.GetComponent<Button>(); }
            var back = Btn("Back"); var next = Btn("Next");
            var gotIt = new GameObject("GotIt"); gotIt.transform.SetParent(root.transform);
            Set(show, "slides", slides); Set(show, "back", back); Set(show, "next", next);
            Set(show, "gotIt", gotIt); Set(show, "readSeconds", 0.2f);
            root.SetActive(true);
            yield return null;

            Assert.IsTrue(slides[0].activeSelf); Assert.IsFalse(slides[1].activeSelf);
            Assert.IsFalse(back.interactable, "no Back on first slide");
            Assert.IsFalse(next.interactable, "Next locked while reading");
            Assert.IsFalse(gotIt.activeSelf);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(next.interactable);

            show.Next(); show.Next();
            Assert.IsTrue(slides[2].activeSelf);
            Assert.IsTrue(gotIt.activeSelf, "Got it on last slide");
            Assert.IsFalse(next.gameObject.activeSelf);
            show.Back();
            Assert.IsTrue(slides[1].activeSelf);
            Assert.IsTrue(back.interactable);
            Object.Destroy(root);
        }
    }
}
