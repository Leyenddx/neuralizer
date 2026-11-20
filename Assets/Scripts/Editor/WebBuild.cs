using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Neuralizer.EditorTools {

    /// <summary>
    /// Genera la build WebGL en la carpeta <c>public/</c> de la raíz del proyecto, que es la que publica
    /// la GitHub Action <c>.github/workflows/deploy-pages.yml</c>. Usa las escenas activas de Build
    /// Settings y la configuración WebGL del proyecto (plantilla PS2L, Brotli con Decompression
    /// Fallback, que es lo que necesita GitHub Pages porque no deja configurar encabezados).
    /// </summary>
    /// <remarks>
    /// También corre en modo batch (con el editor cerrado):
    /// <c>Unity.exe -batchmode -quit -projectPath … -executeMethod Neuralizer.EditorTools.WebBuild.Build</c>
    /// </remarks>
    public static class WebBuild {

        public const string OutputFolder = "public";

        [MenuItem("Tools/Neuralizer/Build Web (carpeta public)", false, 100)]
        public static void Build() {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && File.Exists(s.path))
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0) {
                Fail("No hay escenas activas en Build Settings.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            string output = Path.GetFullPath(OutputFolder);
            Debug.Log($"[WebBuild] Generando build WebGL en {output} con {scenes.Length} escenas:\n  " + string.Join("\n  ", scenes));

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded) {
                Fail($"La build terminó con resultado {summary.result} ({summary.totalErrors} errores). Revisa la consola.");
                return;
            }
            Debug.Log($"[WebBuild] OK — {summary.totalSize / (1024f * 1024f):0.0} MB en {summary.totalTime.TotalMinutes:0.0} min → {output}");
        }

        private static void Fail(string message) {
            Debug.LogError("[WebBuild] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
