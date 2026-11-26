using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace First180.Practice.Editor
{
    public static class PracticeBenchmarkBuild
    {
        [MenuItem("FIRST180 Practice/Diagnostics/Build Windows frame benchmark")]
        public static void Build()
        {
            string folder = Path.Combine(Path.GetTempPath(), "FIRST180-PracticeBenchmark");
            BuildWindows(Path.Combine(folder,"FIRST180-Benchmark.exe"), true);
        }

        [MenuItem("FIRST180 Practice/Build Windows practice player")]
        public static void BuildRelease()
        {
            var repository = Directory.GetParent(Application.dataPath).Parent.FullName;
            BuildWindows(Path.Combine(repository,"output","Windows","FIRST180.exe"), false);
        }

        static void BuildWindows(string outputPath, bool diagnostic)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before building the Windows player.");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
                target = BuildTarget.StandaloneWindows64,
                locationPathName = outputPath,
                options = BuildOptions.None,
                extraScriptingDefines = diagnostic ? new[] {"FIRST180_FRAME_BENCHMARK"} : Array.Empty<string>()
            });
            if (build.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows player build " + build.summary.result + " (" + build.summary.totalErrors + " errors).");
            string marker = diagnostic ? "FIRST180_FRAME_BENCHMARK_BUILT=" : "FIRST180_WINDOWS_RELEASE_BUILT=";
            Debug.Log(marker + build.summary.outputPath);
        }
    }
}
