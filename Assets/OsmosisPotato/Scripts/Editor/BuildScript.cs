using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Osmosis.EditorTools
{
    /// Task 6.4: one-command Android build. Works from the menu or headless:
    ///   unity build . --target Android --execute-method Osmosis.EditorTools.BuildScript.Android            (dev APK)
    ///   ... -- -release            release APK        ... -- -release -aab     Play Store bundle
    public static class BuildScript
    {
        [MenuItem("Osmosis/Build Android (Development APK)")] static void MenuDev() => Run(dev: true, aab: false);
        [MenuItem("Osmosis/Build Android (Release AAB)")] static void MenuRelease() => Run(dev: false, aab: true);

        /// Entry point for -executeMethod; flags come from the command line.
        public static void Android()
        {
            var args = Environment.GetCommandLineArgs();
            bool release = Array.IndexOf(args, "-release") >= 0, aab = Array.IndexOf(args, "-aab") >= 0;
            var report = Run(dev: !release, aab: aab);
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static BuildPlayerOptions Options(bool dev, bool aab)
        {
            var scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++) scenes[i] = EditorBuildSettings.scenes[i].path;
            return new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                locationPathName = "Builds/osmosis-potato" + (dev ? "-dev" : "") + (aab ? ".aab" : ".apk"),
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
        }

        static BuildReport Run(bool dev, bool aab)
        {
            ProjectSetup.Apply();
            ProjectSetup.OptimizeAssets();
            EditorUserBuildSettings.buildAppBundle = aab;
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(Options(dev, aab));
            var s = report.summary;
            Debug.Log($"[Osmosis] Build {s.result}: {s.outputPath} {s.totalSize / 1048576f:0.0} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            return report;
        }
    }
}
