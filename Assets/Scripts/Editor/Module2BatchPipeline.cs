using System;
using System.IO;
using System.Reflection;
using Neuralizer.Data;
using Neuralizer.Fuzzy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neuralizer.EditorTools {

    /// <summary>
    /// Regenera todo el Módulo 2 de una vez (recursos, niveles, escenas de ejercicios y menú) y saca
    /// capturas PNG a 540×960 (la resolución del build WebGL) de cada pantalla, para revisar el diseño
    /// sin abrir cada escena a mano. Pensado para correrse también en modo batch:
    /// <c>Unity -batchmode -quit -projectPath … -executeMethod Neuralizer.EditorTools.Module2BatchPipeline.Run</c>
    /// (variable de entorno <c>M2_CAPTURE_DIR</c> opcional para la carpeta de salida).
    /// </summary>
    /// <remarks>
    /// Las capturas se hacen en modo edición: la escena de niveles se "arranca" invocando por
    /// reflexión el Start/LateUpdate de <see cref="FuzzyLevelController"/> sobre un nivel, y el menú se
    /// pone en estado "Módulo 2 seleccionado". Las escenas se abren para capturar SIN guardar esos
    /// cambios temporales.
    /// </remarks>
    public static class Module2BatchPipeline {

        private const int Width = 540;
        private const int Height = 960;

        [MenuItem("Tools/Neuralizer/Módulo 2/Regenerar todo + capturas", false, 1)]
        public static void Run() {
            string outDir = Environment.GetEnvironmentVariable("M2_CAPTURE_DIR");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.GetFullPath("Temp/Module2Captures");
            Directory.CreateDirectory(outDir);

            try {
                Module2AssetGenerator.Generate();
                Module2LevelAssetsBuilder.Build();
                BuildHealerHud();
                Module2DamageDemoBuilder.Build();
                Module2ComparisonDemoBuilder.Build();
                Module2LevelSceneBuilder.Build();
                BuildMenu();
                CaptureAll(outDir);
                Debug.Log("[Module2BatchPipeline] OK — capturas en " + outDir);
            }
            catch (Exception e) {
                Debug.LogError("[Module2BatchPipeline] FALLÓ: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        /// <summary>Solo capturas (sin reconstruir nada).</summary>
        [MenuItem("Tools/Neuralizer/Módulo 2/Solo capturas", false, 2)]
        public static void CaptureOnly() {
            string outDir = Environment.GetEnvironmentVariable("M2_CAPTURE_DIR");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.GetFullPath("Temp/Module2Captures");
            Directory.CreateDirectory(outDir);
            CaptureAll(outDir);
            Debug.Log("[Module2BatchPipeline] Capturas en " + outDir);
        }

        private static void BuildHealerHud() {
            // El builder del HUD trabaja sobre la escena abierta y no la guarda solo.
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/FuzzyDemo.unity", OpenSceneMode.Single);
            Module2HudBuilder.BuildHud();
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildMenu() {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            Module2MenuBuilder.Build();
            EditorSceneManager.SaveScene(scene);
        }

        // -----------------------------------------------------------------------------------------

        private static void CaptureAll(string outDir) {
            CaptureMenu(outDir);
            CaptureScene("Assets/Scenes/FuzzyDemo.unity", Path.Combine(outDir, "ej1_healer.png"));
            CaptureScene("Assets/Scenes/FuzzyDamageDemo.unity", Path.Combine(outDir, "ej2_dano.png"));
            CaptureScene("Assets/Scenes/FuzzyComparisonDemo.unity", Path.Combine(outDir, "ej3_triage.png"));

            string[] guids = AssetDatabase.FindAssets("t:FuzzyScene", new[] { Module2LevelAssetsBuilder.Folder });
            string[] paths = Array.ConvertAll(guids, g => AssetDatabase.GUIDToAssetPath(g));
            Array.Sort(paths, StringComparer.Ordinal);
            for (int i = 0; i < paths.Length; i++) {
                CaptureLevel(paths[i], Path.Combine(outDir, $"nivel{i + 1}.png"), false);
            }
            if (paths.Length > 0) {
                CaptureLevel(paths[^1], Path.Combine(outDir, "nivel_resultado.png"), true);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void CaptureMenu(string outDir) {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            ModuleTabController tabs = UnityEngine.Object.FindAnyObjectByType<ModuleTabController>(FindObjectsInactive.Include);
            if (!tabs) {
                Debug.LogWarning("[Module2BatchPipeline] MainScene sin ModuleTabController: no se captura el menú.");
                return;
            }

            // Mismo estado que deja el botón "Start" (ver MainMenuReturnHandler), con el Módulo 2 activo.
            Transform wrapper = tabs.transform.parent;
            Transform canvas = wrapper.parent.parent;
            Transform start = canvas.Find("Start Button");
            if (start) start.gameObject.SetActive(false);
            foreach (string n in new[] { "App Bar", "Content Panel", "Version Label" }) {
                Transform t = wrapper.Find(n);
                if (t) t.gameObject.SetActive(true);
            }
            tabs.gameObject.SetActive(true);
            Capture capture = BeginCapture();
            tabs.ShowModule2();
            RebuildLayouts();
            // En modo edición no corre LateUpdate: se ubica el indicador de pestaña a mano.
            foreach (UI.M2TabIndicator indicator in UnityEngine.Object.FindObjectsByType<UI.M2TabIndicator>()) {
                Invoke(indicator, "LateUpdate");
            }
            // Estado "como en el juego": sin gizmos 3D del Módulo 1, sin la etiqueta de versión (se
            // rellena en runtime) y con la barra de progreso en su valor final (en edición no corre Update).
            foreach (AxisGizmo gizmo in UnityEngine.Object.FindObjectsByType<AxisGizmo>()) {
                gizmo.gameObject.SetActive(false);
            }
            Transform version = wrapper.Find("Version Label");
            if (version) version.gameObject.SetActive(false);
            foreach (UI.Module2MenuView view in UnityEngine.Object.FindObjectsByType<UI.Module2MenuView>()) {
                Invoke(view, "OnEnable");
                FieldInfo target = typeof(UI.Module2MenuView).GetField("fillTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo fill = typeof(UI.Module2MenuView).GetField("progressFill", BindingFlags.Instance | BindingFlags.NonPublic);
                if (target != null && fill?.GetValue(view) is Image img) img.fillAmount = (float)target.GetValue(view);
            }
            RebuildLayouts();
            EndCapture(capture, Path.Combine(outDir, "menu_modulo2.png"));
        }

        private static void CaptureScene(string path, string file) {
            if (!File.Exists(path)) return;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Render(file);
        }

        private static void CaptureLevel(string levelPath, string file, bool showResult) {
            EditorSceneManager.OpenScene(Module2LevelSceneBuilder.ScenePath, OpenSceneMode.Single);
            // Cargar el nivel DESPUÉS de abrir la escena: abrirla descarga los assets sin uso, y un nivel
            // cargado antes llegaba "destruido" (el controlador caía al nivel de respaldo).
            FuzzyScene level = AssetDatabase.LoadAssetAtPath<FuzzyScene>(levelPath);
            FuzzyLevelController controller = UnityEngine.Object.FindAnyObjectByType<FuzzyLevelController>();
            FuzzyHost host = UnityEngine.Object.FindAnyObjectByType<FuzzyHost>();
            if (!controller || !host) return;

            // La cámara de 540×960 va PRIMERO: el controlador mide anchos (grilla de casos, gráfica)
            // en Start/LateUpdate, y debe medir con el tamaño final, no con la pantalla del modo batch.
            Capture capture = BeginCapture();

            Invoke(host, "Awake");
            FuzzyLevelSelection.Pending = level;
            Invoke(controller, "Start");
            Debug.Log($"[Module2BatchPipeline] Captura de nivel '{level.name}' → título '{GetTitle(controller)}'");

            // Varias pasadas de layout + LateUpdate: la primera mide, las siguientes dibujan con esa medida.
            for (int pass = 0; pass < 4; pass++) {
                RebuildLayouts();
                Invoke(controller, "LateUpdate");
                // En modo edición no corre el LateUpdate del indicador de pestaña: se ubica a mano.
                foreach (UI.M2TabIndicator indicator in UnityEngine.Object.FindObjectsByType<UI.M2TabIndicator>()) {
                    Invoke(indicator, "LateUpdate");
                }
            }

            if (showResult) {
                // Se muestra el popup directo (no host.SubmitResult): calificar escribiría el progreso
                // del nivel en los PlayerPrefs del editor.
                Invoke(controller, "ShowResult", false);
                // En modo edición no corre Update: se deja el popup en su estado final de la animación.
                GameObject panel = GameObject.Find("ResultPanel");
                if (panel) {
                    CanvasGroup g = panel.GetComponent<CanvasGroup>();
                    if (g) g.alpha = 1f;
                    Transform card = panel.transform.Find("ResultCard");
                    if (card) card.localScale = Vector3.one;
                }
            }
            EndCapture(capture, file);
        }

        private static string GetTitle(FuzzyLevelController controller) {
            FieldInfo f = typeof(FuzzyLevelController).GetField("titleText", BindingFlags.Instance | BindingFlags.NonPublic);
            return f?.GetValue(controller) is TMPro.TMP_Text t ? t.text : "?";
        }

        private static void Invoke(object target, string method, params object[] args) {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            m?.Invoke(target, args.Length > 0 ? args : null);
        }

        /// <summary>
        /// Renderiza todos los Canvas de la escena abierta a un PNG de 540×960: los pasa
        /// temporalmente a ScreenSpaceCamera con una cámara apuntando a un RenderTexture.
        /// </summary>
        private static void Render(string file) => EndCapture(BeginCapture(), file);

        private class Capture {
            public RenderTexture rt;
            public Camera cam;
        }

        /// <summary>Pasa los Canvas raíz a ScreenSpaceCamera con una cámara de 540×960 sobre un RenderTexture.</summary>
        private static Capture BeginCapture() {
            RenderTexture rt = new(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            rt.Create();
            GameObject camGO = new("CaptureCamera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.targetTexture = rt;
            cam.cullingMask = 0; // se completa abajo con las capas de los Canvas (sin objetos 3D sueltos)

            foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>()) {
                if (!c.isRootCanvas) continue;
                cam.cullingMask |= 1 << c.gameObject.layer;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 10f;
            }
            RebuildLayouts();
            return new Capture { rt = rt, cam = cam };
        }

        private static void RebuildLayouts() {
            for (int i = 0; i < 2; i++) {
                Canvas.ForceUpdateCanvases();
                foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>()) {
                    if (c.isRootCanvas) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
                }
            }
        }

        private static void EndCapture(Capture capture, string file) {
            RebuildLayouts();
            capture.cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = capture.rt;
            Texture2D tex = new(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(file, tex.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(capture.cam.gameObject);
            capture.rt.Release();
            UnityEngine.Object.DestroyImmediate(capture.rt);
        }
    }
}
