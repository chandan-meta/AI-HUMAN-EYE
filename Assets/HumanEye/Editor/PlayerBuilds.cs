using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HumanEye.EditorTools
{
    /// Player builds of the launch scene only. Batch entry points:
    ///   -executeMethod HumanEye.EditorTools.PlayerBuilds.Windows
    ///   -executeMethod HumanEye.EditorTools.PlayerBuilds.Android
    ///   -executeMethod HumanEye.EditorTools.PlayerBuilds.WebGL [-heRelease]
    public static class PlayerBuilds
    {
        [MenuItem("Tools/Human Eye/Build Player/Windows", priority = 40)] public static void Windows() => Exit(Run(BuildTarget.StandaloneWindows64, "Builds/HumanEye/Windows/HumanEye.exe"));
        [MenuItem("Tools/Human Eye/Build Player/Android (tablet)", priority = 41)] public static void Android() => Exit(Run(BuildTarget.Android, "Builds/HumanEye/Android/HumanEye.apk"));
        [MenuItem("Tools/Human Eye/Build Player/WebGL", priority = 42)] public static void WebGL()
        {
            WebGLSettings();
            var aside = WebOnlyAssets();
            bool ok;
            try { ok = Run(BuildTarget.WebGL, "Builds/HumanEye/WebGL"); }
            finally { PutBack(aside); }   // the Editor and the desktop player keep their meshes and sound as they were
            Exit(ok);
        }
        public static void RebuildWindows() { Builder.Build(); Windows(); }

        /// The web player (16 October 2026): the Human Eye loading page (template HumanEyeLoader: the supplied artwork with a
        /// live bar), compressed files that the loader decompresses itself where the host sends no Content-Encoding header
        /// (GitHub Pages), hashed file names so a new deployment never mixes with cached files, and data caching in the browser.
        /// Smallest download (22 October 2026): Brotli instead of gzip, no Unity splash or logo (the loading page is the
        /// splash; the logo texture alone was 2.7 MB), IL2CPP code generated for size, and the web-only textures, meshes and
        /// sound below.
        static void WebGLSettings()
        {
            PlayerSettings.WebGL.template = "PROJECT:HumanEyeLoader";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.SplashScreen.show = false; PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            WebGLTextures();
        }

        // Textures that stay at 2048 on the web: read at full screen or explained up close.
        static readonly string[] WebHero = { "HumanSkin_Albedo", "Muscles_Part1_Albedo", "Muscles_Part2_Albedo", "Muscles_Part3_Albedo", "_Brain_Diff", "Hindbrain_Diff",
            "Hindbrain_Center_Diff", "Midbrain_Diff", "Rod_Albedo", "Cone_Albedo.", "Niko_BaseColor", "HUD Infographic", "Science Interface", "Starlit Galaxy" };

        /// Web-only texture overrides (the desktop player and the Editor keep theirs), for a quicker start: the lesson eye's maps
        /// stay at 2048 with the highest crunch quality (uncrunched until 22 October 2026: 22 MB of the build); the hero albedos
        /// stay at 2048 with high-quality crunch; every other map of 512 px or more (normals, masks, the lab, organs) is at most
        /// 1024 and crunched. UI sprites, small textures, HDR sources and baked lighting are left alone. (GitHub Pages' 100 MB
        /// file limit is met by splitting at publish, not here.)
        static void WebGLTextures()
        {
            int changed = 0, seen = 0;
            foreach (var path in AssetDatabase.GetDependencies(Builder.ScenePath, true))
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti || path.Contains("/Scenes/") || path.StartsWith("Packages/")) continue;
                if (ti.textureType is TextureImporterType.Sprite or TextureImporterType.GUI or TextureImporterType.Lightmap or TextureImporterType.Cookie) continue;
                ti.GetSourceTextureWidthAndHeight(out int w, out int h);
                if (Mathf.Min(Mathf.Max(w, h), ti.maxTextureSize) < 512) continue;   // small maps: glows, ramps, icons
                seen++;
                bool eye = path.Contains("/LocalEye/"), hero = Array.Exists(WebHero, k => Path.GetFileName(path).Contains(k));
                bool hdr = path.EndsWith(".exr", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".hdr", StringComparison.OrdinalIgnoreCase);
                int size = eye || hero ? Mathf.Min(2048, ti.maxTextureSize) : Mathf.Min(1024, ti.maxTextureSize);
                bool crunch = !hdr;
                int quality = eye ? 90 : hero ? 85 : ti.textureType == TextureImporterType.NormalMap ? 80 : 70;
                var web = ti.GetPlatformTextureSettings("WebGL");
                if (web.overridden && web.maxTextureSize == size && web.crunchedCompression == crunch && (!crunch || web.compressionQuality == quality)
                    && web.format == TextureImporterFormat.Automatic) continue;
                web.overridden = true; web.maxTextureSize = size; web.format = TextureImporterFormat.Automatic;
                web.textureCompression = TextureImporterCompression.Compressed;
                web.crunchedCompression = crunch; web.compressionQuality = crunch ? quality : 50;
                ti.SetPlatformTextureSettings(web); ti.SaveAndReimport(); changed++;
            }
            Debug.Log($"HUMAN_EYE_WEB_TEXTURES considered={seen} changed={changed}");
        }

        /// Smallest web download, kept to the web build (22 October 2026). Generated meshes are stored quantized (mesh
        /// compression Medium; the eye's own meshes Low, because the lesson explains them up close). Music and Niko's voice are
        /// encoded at quality 0.35 instead of 0.55 and 0.7: WebGL accepts no audio override, and below 0.35 its encoder saves
        /// nothing more. Both change the assets themselves, so their files are copied aside first and PutBack restores them
        /// byte for byte after the build.
        static string WebOnlyAssets()
        {
            var aside = FileUtil.GetUniqueTempPathInProject();
            var meshes = AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/HumanEye/Generated/Meshes" }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/HumanEye/Audio/Music", "Assets/HumanEye/Audio/VO" }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            foreach (var file in meshes.Concat(clips.Select(c => c + ".meta")))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(aside, file)));
                File.Copy(file, Path.Combine(aside, file));
            }
            foreach (var path in meshes)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                MeshUtility.SetMeshCompression(mesh, path.Contains("/LocalEye_") ? ModelImporterMeshCompression.Low : ModelImporterMeshCompression.Medium);
                EditorUtility.SetDirty(mesh);
            }
            AssetDatabase.SaveAssets();
            foreach (var path in clips)
            {
                var ai = (AudioImporter)AssetImporter.GetAtPath(path);
                var web = ai.defaultSampleSettings; web.quality = .35f; ai.defaultSampleSettings = web; ai.SaveAndReimport();
            }
            Debug.Log($"HUMAN_EYE_WEB_ASSETS meshes={meshes.Count} clips={clips.Count} aside={aside}");
            return aside;
        }

        static void PutBack(string aside)
        {
            foreach (var file in Directory.GetFiles(aside, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.GetRelativePath(aside, file), true);
            Directory.Delete(aside, true);
            AssetDatabase.Refresh();   // the restored files differ from what was imported, so they reimport
        }

        static void Exit(bool ok) { if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1); }

        static bool Run(BuildTarget target, string path)
        {
            if (target == BuildTarget.WebGL && Directory.Exists(path)) Directory.Delete(path, true);   // no stale files from an older build
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // The existing _Project AssetAudit gate rejects non-development builds until production acceptance; a production
            // build passes it only when explicitly requested with -heRelease (the published WebGL build).
            var dev = !Environment.GetCommandLineArgs().Contains("-heRelease");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Builder.ScenePath }, locationPathName = path, target = target,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            });
            var s = report.summary;
            if (s.result == BuildResult.Succeeded && target != BuildTarget.Android)
            {
                var folder = target == BuildTarget.WebGL ? path : Path.GetDirectoryName(path);
                File.Copy("Docs/HumanEye/THIRD_PARTY_NOTICES.md", Path.Combine(folder, "THIRD_PARTY_NOTICES.md"), true);
            }
            var line = $"{DateTime.UtcNow:u} {target} {s.result} {(dev ? "development" : "release")} size={s.totalSize / 1048576f:0.0}MB time={s.totalTime} errors={s.totalErrors} warnings={s.totalWarnings} -> {path}";
            File.AppendAllText("Logs/he_player_builds.txt", line + Environment.NewLine);
            Debug.Log("HUMAN_EYE_PLAYER " + line);
            return s.result == BuildResult.Succeeded;
        }
    }
}
