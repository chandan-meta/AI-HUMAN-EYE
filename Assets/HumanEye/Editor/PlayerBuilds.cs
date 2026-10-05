using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HumanEye.EditorTools
{
    /// Player builds of the launch scene only. Batch entry points:
    ///   -executeMethod HumanEye.EditorTools.PlayerBuilds.Windows
    ///   -executeMethod HumanEye.EditorTools.PlayerBuilds.Android
    public static class PlayerBuilds
    {
        [MenuItem("Tools/Human Eye/Build Player/Windows", priority = 40)] public static void Windows() => Run(BuildTarget.StandaloneWindows64, "Builds/HumanEye/Windows/HumanEye.exe");
        [MenuItem("Tools/Human Eye/Build Player/Android (tablet)", priority = 41)] public static void Android() => Run(BuildTarget.Android, "Builds/HumanEye/Android/HumanEye.apk");
        public static void RebuildWindows() { Builder.Build(); Windows(); }

        static void Run(BuildTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // The existing _Project AssetAudit gate rejects non-development builds until production acceptance;
            // respect it and build Development players unless -heRelease is passed after that gate is lifted.
            var dev = !Environment.GetCommandLineArgs().Contains("-heRelease");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Builder.ScenePath }, locationPathName = path, target = target,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            });
            var s = report.summary;
            var line = $"{DateTime.UtcNow:u} {target} {s.result} size={s.totalSize / 1048576f:0.0}MB time={s.totalTime} errors={s.totalErrors} warnings={s.totalWarnings} -> {path}";
            File.AppendAllText("Logs/he_player_builds.txt", line + Environment.NewLine);
            Debug.Log("HUMAN_EYE_PLAYER " + line);
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
