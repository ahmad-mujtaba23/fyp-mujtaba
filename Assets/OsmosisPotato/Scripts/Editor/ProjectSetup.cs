using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Osmosis.EditorTools
{
    /// Applies the brief's mobile settings in one click. Safe to re-run.
    public static class ProjectSetup
    {
        [MenuItem("Osmosis/Apply Mobile Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Tajrubahgah";            // matches the reference suite
            PlayerSettings.productName = "Osmosis in Potato";
            // ponytail: placeholder bundle id, replace with the store id before publishing
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.tajrubahgah.osmosispotato");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.gcIncremental = true;

            // Landscape lab bench, both landscape directions
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            var mobile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (mobile)
            {
                mobile.supportsHDR = false;
                EditorUtility.SetDirty(mobile);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Osmosis] Mobile settings applied: IL2CPP/ARM64, stripping High, incremental GC, landscape, HDR off.");
        }

        const string Android = "Android";
        static readonly string[] AssetRoots = { "Assets/OsmosisPotato/Reference", "Assets/OsmosisPotato/Art" };

        /// Task 6.3: Android import overrides so the APK carries only what it needs. Safe to re-run.
        [MenuItem("Osmosis/Optimize Assets For Mobile")]
        public static void OptimizeAssets()
        {
            int tex = 0, audio = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", AssetRoots))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                bool sprite = ti.textureType == TextureImporterType.Sprite;
                // backdrops and the zoomed-in ruler keep detail; UI art is never shown larger than ~1024 px
                string file = System.IO.Path.GetFileName(path);
                int max = path.Contains("/Environment/") || file == "bg.png" || file == "RulerTicks.png" ? 2048 : 1024;
                var s = ti.GetPlatformTextureSettings(Android);
                if (s.overridden && s.maxTextureSize == max && s.format == TextureImporterFormat.ASTC_6x6 && (!sprite || !ti.mipmapEnabled)) continue;
                s.overridden = true;
                s.maxTextureSize = max;
                s.format = TextureImporterFormat.ASTC_6x6;
                ti.SetPlatformTextureSettings(s);
                if (sprite) ti.mipmapEnabled = false;                                   // UI never minifies much
                ti.SaveAndReimport();
                tex++;
            }
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", AssetRoots))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ai = (AudioImporter)AssetImporter.GetAtPath(path);
                bool music = path.Contains("Powerup") || path.Contains("bensound");
                var s = ai.GetOverrideSampleSettings(Android);
                var want = new AudioImporterSampleSettings
                {
                    loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad,
                    compressionFormat = AudioCompressionFormat.Vorbis,
                    quality = music ? 0.45f : 0.6f,
                    sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate,
                    preloadAudioData = !music,
                };
                if (ai.ContainsSampleSettingsOverride(Android) && s.loadType == want.loadType && Mathf.Approximately(s.quality, want.quality)
                    && ai.forceToMono == !music) continue;
                ai.SetOverrideSampleSettings(Android, want);
                ai.forceToMono = !music;                                                 // SFX: mono is half the size, no audible loss
                ai.SaveAndReimport();
                audio++;
            }
            // URP post-processing is unused (no Volume in our scenes); dropping its data removes ~3.5 MB of film-grain/SMAA textures.
            // Both template renderers ship in the build (one per quality level), so strip both.
            foreach (var r in new[] { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" })
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(r);
                if (renderer && renderer.postProcessData) { renderer.postProcessData = null; EditorUtility.SetDirty(renderer); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Osmosis] Optimized for Android: {tex} textures, {audio} audio clips, post-processing data removed.");
        }
    }
}
