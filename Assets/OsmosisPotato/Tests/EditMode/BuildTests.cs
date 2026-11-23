using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using Osmosis.EditorTools;

namespace Osmosis.Tests
{
    /// M6: build configuration, mobile asset settings, canvas/LayoutGroup rules.
    public class BuildTests
    {
        [Test]
        public void BuildOptionsCoverTheThreeScenes()
        {
            var dev = BuildScript.Options(dev: true, aab: false);
            Assert.AreEqual(BuildTarget.Android, dev.target);
            CollectionAssert.AreEqual(new[] { "Assets/OsmosisPotato/Scenes/" + Scenes.Menu + ".unity", "Assets/OsmosisPotato/Scenes/" + Scenes.Guidance + ".unity", "Assets/OsmosisPotato/Scenes/" + Scenes.Lab + ".unity" }, dev.scenes);
            Assert.IsTrue(dev.options.HasFlag(BuildOptions.Development));
            StringAssert.EndsWith("-dev.apk", dev.locationPathName);
            var rel = BuildScript.Options(dev: false, aab: true);
            Assert.IsFalse(rel.options.HasFlag(BuildOptions.Development));
            StringAssert.EndsWith(".aab", rel.locationPathName);
        }

        [Test]
        public void PlayerSettingsMatchTheBrief()
        {
            var a = NamedBuildTarget.Android;
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(a));
            Assert.AreEqual(ManagedStrippingLevel.High, PlayerSettings.GetManagedStrippingLevel(a));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
            Assert.IsTrue(PlayerSettings.gcIncremental);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait, "landscape only");
            var rp = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            Assert.IsFalse(rp.supportsHDR, "no HDR");
        }

        [Test]
        public void AudioAndTexturesHaveAndroidOverrides()
        {
            foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/OsmosisPotato/Reference/Audio" }))
            {
                var ai = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g));
                Assert.IsTrue(ai.ContainsSampleSettingsOverride("Android"), ai.assetPath);
                Assert.AreEqual(AudioCompressionFormat.Vorbis, ai.GetOverrideSampleSettings("Android").compressionFormat);
            }
            var music = (AudioImporter)AssetImporter.GetAtPath("Assets/OsmosisPotato/Reference/Audio/Jeremy Blake - Powerup! NO COPYRIGHT 8-bit Music.mp3");
            Assert.AreEqual(AudioClipLoadType.Streaming, music.GetOverrideSampleSettings("Android").loadType, "music streams, not loaded whole");
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/OsmosisPotato/Reference", "Assets/OsmosisPotato/Art" }))
            {
                var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as TextureImporter;
                if (!ti) continue;
                var s = ti.GetPlatformTextureSettings("Android");
                Assert.IsTrue(s.overridden && s.maxTextureSize <= 2048, ti.assetPath);
            }
            var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
            Assert.IsNull(renderer.postProcessData, "unused post-processing stripped");
        }

        [TestCase(Scenes.Menu), TestCase(Scenes.Guidance), TestCase(Scenes.Lab)]
        public void NoLayoutGroupsAndGraphHasOwnCanvas(string scene)
        {
            var s = EditorSceneManager.OpenScene("Assets/OsmosisPotato/Scenes/" + scene + ".unity", OpenSceneMode.Additive);
            try
            {
                var all = s.GetRootGameObjects();
                Assert.IsEmpty(all.SelectMany(g => g.GetComponentsInChildren<LayoutGroup>(true)), "brief: no LayoutGroup on animated panels (we use none)");
                foreach (var gv in all.SelectMany(g => g.GetComponentsInChildren<GraphView>(true)))
                {
                    var c = gv.GetComponent<Canvas>();
                    Assert.IsNotNull(c, "animated graph on its own canvas");
                    Assert.IsNotNull(gv.GetComponent<GraphicRaycaster>());
                }
            }
            finally { EditorSceneManager.CloseScene(s, true); }
        }
    }
}
