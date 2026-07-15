using System.Linq;
using UnityEditor;

namespace EL4S.EditorTools
{
    // Scratch build entry point for local batchmode verification runs.
    public static class CIBuild
    {
        public static void BuildStandaloneOSX()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/macOS/EL4S-manual.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            });

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
