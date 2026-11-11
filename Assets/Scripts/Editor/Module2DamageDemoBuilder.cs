using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Neuralizer.Fuzzy;
using Neuralizer.UI;

namespace Neuralizer.EditorTools {

    /// <summary>
    /// Arma (o rearma desde cero) la escena completa del Ejercicio 2 del Módulo 2 ("Lectura visual
    /// del daño") — Canvas, retrato de personaje, aro de urgencia, resplandor, slider sin números y
    /// panel de revelado con la curva real — como GameObjects guardados en
    /// <c>Assets/Scenes/FuzzyDamageDemo.unity</c>, y deja las referencias de
    /// <see cref="FuzzyDamageDemo"/> ya conectadas. Mismo patrón que
    /// <see cref="Module2HudBuilder"/>/<c>FuzzyDemo.unity</c>, aplicado a una escena nueva.
    /// </summary>
    /// <remarks>
    /// Correr <c>Tools → Neuralizer → Configurar Sprites del Módulo 2</c> ANTES de esta herramienta
    /// al menos una vez, para que los retratos en <c>Resources/Sprites/Module2/Heroes/</c> ya estén
    /// importados como Sprite (si no, el retrato cae de vuelta a un cuadro de color sólido).
    /// Idempotente: correrla de nuevo borra el Canvas/EventSystem de una corrida anterior (marcados
    /// con <see cref="Module2DamageDemoMarker"/>) y arma todo de nuevo.
    /// </remarks>
    public static class Module2DamageDemoBuilder {

        private const string ScenePath = "Assets/Scenes/FuzzyDamageDemo.unity";
        private const string HeroSpriteFolder = "Sprites/Module2/Heroes/";
        private static Sprite LoadSprite(string name) => Resources.Load<Sprite>("Sprites/Module2/" + name);
        private static Sprite LoadHeroSprite(string name) => Resources.Load<Sprite>(HeroSpriteFolder + name);

        private class BuiltRefs {
            public RectTransform canvasRoot;
            public Button backButton;
            public Image heroImage;
            public Image[] heroRing;
            public Image[] glowLayers;
            public Slider damageSlider;
            public Button revealButton;
            public RectTransform curvePanel;
            public RectTransform curveArea;
            public UIPolyline curveLine;
            public UIFilledCurve curveFill;
            public RectTransform curveMarker;
            public TextMeshProUGUI formulaText;
            public TextMeshProUGUI valueReadout;
        }

        [MenuItem("Tools/Neuralizer/Construir Ejercicio 2 (Lectura de Daño)")]
        public static void Build() {
            Scene scene = OpenOrCreateScene();

            CleanupPrevious();

            BuiltRefs refs = new();
            EnsureCamera();
            EnsureEventSystem();
            refs.canvasRoot = BuildCanvas();

            BuildBackground(refs.canvasRoot);
            BuildTopBar(refs);
            BuildHeroArea(refs);
            BuildControlArea(refs);
            BuildRevealPanel(refs);

            GameObject controllerGO = new("DamageDemoController");
            controllerGO.AddComponent<Module2DamageDemoMarker>();
            FuzzyDamageDemo demo = controllerGO.AddComponent<FuzzyDamageDemo>();
            AssignReferences(demo, refs);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(refs.canvasRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();

            Debug.Log("[Module2DamageDemoBuilder] Ejercicio 2 (Lectura de Daño) construido y guardado en " + ScenePath);
        }

        // -----------------------------------------------------------------------------------------
        // Escena / limpieza
        // -----------------------------------------------------------------------------------------

        private static Scene OpenOrCreateScene() {
            if (System.IO.File.Exists(ScenePath)) {
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return scene;
        }

        private static void CleanupPrevious() {
            foreach (Module2DamageDemoMarker marker in Object.FindObjectsByType<Module2DamageDemoMarker>()) {
                Undo.DestroyObjectImmediate(marker.gameObject);
            }
            // Las corridas anteriores de esta misma herramienta dejaban el Canvas/EventSystem sueltos
            // (antes de que existiera este marcador) — una pasada defensiva por si hay que reconstruir
            // sobre una escena armada con una versión vieja de este script.
            foreach (Canvas c in Object.FindObjectsByType<Canvas>()) {
                if (c.gameObject.name == "Canvas") Object.DestroyImmediate(c.gameObject);
            }
        }

        /// <summary>
        /// La pantalla es Canvas ScreenSpaceOverlay puro (no necesita cámara para dibujar UI), pero sin
        /// NINGUNA cámara en la escena el Game view de Unity muestra el aviso "No cameras rendering"
        /// tapando todo — mismo motivo por el que FuzzyDemo.unity trae su propia Main Camera. Clear
        /// Flags en SolidColor (no Skybox) porque no hay nada 3D que mostrar detrás del Canvas.
        /// </summary>
        private static void EnsureCamera() {
            if (Object.FindAnyObjectByType<Camera>()) return;
            GameObject camGO = new("Main Camera", typeof(Camera));
            camGO.tag = "MainCamera";
            Camera cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = EsportsTheme.BgDeep;
            cam.orthographic = true;
        }

        private static void EnsureEventSystem() {
            if (Object.FindAnyObjectByType<EventSystem>()) return;
            GameObject es = new("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private static void RegisterInBuildSettings() {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(s => s.path == ScenePath)) return;

            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
            Debug.Log("[Module2DamageDemoBuilder] Escena agregada a Build Settings.");
        }

        // -----------------------------------------------------------------------------------------
        // Canvas / fondo / barra superior (mismo patrón que Module2HudBuilder)
        // -----------------------------------------------------------------------------------------

        private static RectTransform BuildCanvas() {
            GameObject canvasGO = new("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            return canvasGO.GetComponent<RectTransform>();
        }

        private static void BuildBackground(RectTransform canvasRoot) {
            Sprite backgroundSprite = LoadSprite("Module2_Background");
            if (backgroundSprite) {
                Image bgImg = EsportsUIKit.CreateSpriteImage("Background", canvasRoot, backgroundSprite);
                EsportsUIKit.Stretch(bgImg.rectTransform);
            }
            else {
                EsportsUIKit.Stretch(EsportsUIKit.CreateImage("Background", canvasRoot, EsportsTheme.BgDeep).rectTransform);
            }
        }

        private static void BuildTopBar(BuiltRefs refs) {
            RectTransform topBar = EsportsUIKit.CreateRect("TopBar", refs.canvasRoot);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(-24f, 64f);
            topBar.anchoredPosition = new Vector2(0f, -16f);

            HorizontalLayoutGroup layout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            Sprite backSprite = LoadSprite("Module2_ButtonBack");
            refs.backButton = backSprite
                ? EsportsUIKit.CreateSpriteIconButton(topBar, backSprite, 44f, 64f, out _)
                : EsportsUIKit.CreateGhostButton(topBar, "‹ Volver", 100f, 64f);

            RectTransform titleRT = EsportsUIKit.CreateRect("Title", topBar);
            titleRT.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            EsportsUIKit.CreateTextFill(titleRT, "LECTURA VISUAL DEL DAÑO", 16f, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center);
        }

        // -----------------------------------------------------------------------------------------
        // Personaje: retrato + aro de urgencia + resplandor
        // -----------------------------------------------------------------------------------------

        private static void BuildHeroArea(BuiltRefs refs) {
            RectTransform area = EsportsUIKit.CreateRect("HeroArea", refs.canvasRoot);
            area.anchorMin = new Vector2(0.5f, 1f);
            area.anchorMax = new Vector2(0.5f, 1f);
            area.pivot = new Vector2(0.5f, 1f);
            area.sizeDelta = new Vector2(360f, 480f);
            area.anchoredPosition = new Vector2(0f, -110f);

            // Resplandor detrás de todo — más grande que el retrato para que el halo se note alrededor.
            RectTransform glow = EsportsUIKit.CreateGlowDot(area, EsportsTheme.NeonGreen, 280f, 120f);
            glow.anchorMin = new Vector2(0.5f, 0.5f);
            glow.anchorMax = new Vector2(0.5f, 0.5f);
            glow.anchoredPosition = Vector2.zero;
            refs.glowLayers = glow.GetComponentsInChildren<Image>();

            Sprite heroSprite = LoadHeroSprite("Hero_Soldier76");
            RectTransform portrait = EsportsUIKit.CreateRect("Portrait", area);
            EsportsUIKit.Stretch(portrait, 20f, 20f, 20f, 60f);
            if (heroSprite) {
                refs.heroImage = EsportsUIKit.CreateSpriteImage("Image", portrait, heroSprite);
                refs.heroImage.preserveAspect = true;
                EsportsUIKit.Stretch(refs.heroImage.rectTransform);
            }
            else {
                refs.heroImage = EsportsUIKit.CreateImage("Image", portrait, EsportsTheme.BgPanelLight);
                EsportsUIKit.Stretch(refs.heroImage.rectTransform);
            }

            // Aro de urgencia alrededor del retrato (4 barras independientes) — arranca verde, el color
            // real lo fija FuzzyDamageDemo.ApplyUrgencyColor en Start().
            RectTransform ringRT = EsportsUIKit.CreateRect("Ring", area);
            EsportsUIKit.Stretch(ringRT, -14f, -14f, -14f, -14f);
            EsportsUIKit.CreateOutline(ringRT, EsportsTheme.NeonGreen, 5f, out Image[] ring);
            refs.heroRing = ring;
        }

        // -----------------------------------------------------------------------------------------
        // Control: slider sin número visible + botón de revelado
        // -----------------------------------------------------------------------------------------

        private static void BuildControlArea(BuiltRefs refs) {
            RectTransform container = EsportsUIKit.CreateAccentCard(refs.canvasRoot, "ControlCard", EsportsTheme.NeonBlend, 2f, 6f);
            RectTransform root = (RectTransform)container.parent.parent;
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(-22f, 140f);
            root.anchoredPosition = new Vector2(0f, 20f);

            VerticalLayoutGroup v = container.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.MiddleCenter;

            EsportsUIKit.CreateText(container, "Deslizá para ver cómo cambia el estado — sin números, solo mirá.",
                13f, EsportsTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, 32f);

            RectTransform sliderHolder = EsportsUIKit.CreateRect("SliderHolder", container);
            sliderHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            refs.damageSlider = EsportsUIKit.CreateGradientSlider(sliderHolder, 0f, 100f, 0f);

            refs.revealButton = EsportsUIKit.CreateGhostButton(container, "Revelar la curva real", 240f, 36f);
        }

        // -----------------------------------------------------------------------------------------
        // Panel de revelado: la curva μ_Crítico(x) real, oculto hasta que el usuario lo pide
        // -----------------------------------------------------------------------------------------

        private static void BuildRevealPanel(BuiltRefs refs) {
            RectTransform content = EsportsUIKit.CreateAccentCard(refs.canvasRoot, "RevealPanel", EsportsTheme.NeonRed, 2f, 6f);
            refs.curvePanel = (RectTransform)content.parent.parent;
            refs.curvePanel.anchorMin = new Vector2(0.5f, 0.5f);
            refs.curvePanel.anchorMax = new Vector2(0.5f, 0.5f);
            refs.curvePanel.pivot = new Vector2(0.5f, 0.5f);
            refs.curvePanel.sizeDelta = new Vector2(560f, 420f);
            refs.curvePanel.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.padding = new RectOffset(10, 10, 10, 10);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;

            EsportsUIKit.CreateText(content, "μ_Crítico(x) — grado de pertenencia", 16f, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center, 24f);

            RectTransform plotHolder = EsportsUIKit.CreateRect("PlotHolder", content);
            plotHolder.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            refs.curveArea = EsportsUIKit.CreateRect("PlotArea", plotHolder);
            EsportsUIKit.Stretch(refs.curveArea, 12f, 12f, 8f, 8f);
            refs.curveArea.pivot = Vector2.zero;
            refs.curveArea.anchorMin = Vector2.zero;
            refs.curveArea.anchorMax = Vector2.one;

            EsportsUIKit.CreateImage("Axis", refs.curveArea, new Color(1f, 1f, 1f, 0.15f));

            refs.curveFill = EsportsUIKit.CreateFilledCurve(refs.curveArea, new Color(EsportsTheme.NeonRed.r, EsportsTheme.NeonRed.g, EsportsTheme.NeonRed.b, 0.25f));
            refs.curveLine = EsportsUIKit.CreatePolyline(refs.curveArea, EsportsTheme.NeonRed, 3f);
            refs.curveMarker = EsportsUIKit.CreateGlowDot(refs.curveArea, EsportsTheme.CurveIntersection, 14f, 16f);

            refs.formulaText = EsportsUIKit.CreateText(content, "", 14f, EsportsTheme.TextSecondary,
                FontStyles.Normal, TextAlignmentOptions.Center, 20f);
            refs.valueReadout = EsportsUIKit.CreateText(content, "0%", 28f, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center, 36f);

            Button closeButton = EsportsUIKit.CreateGhostButton(content, "Cerrar", 140f, 36f);
            closeButton.onClick.AddListener(() => refs.curvePanel.gameObject.SetActive(false));
        }

        // -----------------------------------------------------------------------------------------

        private static void AssignReferences(FuzzyDamageDemo demo, BuiltRefs refs) {
            SerializedObject so = new(demo);
            so.FindProperty("canvasRoot").objectReferenceValue = refs.canvasRoot;
            so.FindProperty("backButton").objectReferenceValue = refs.backButton;
            so.FindProperty("heroImage").objectReferenceValue = refs.heroImage;
            SetArray(so.FindProperty("heroRing"), refs.heroRing);
            SetArray(so.FindProperty("glowLayers"), refs.glowLayers);
            so.FindProperty("damageSlider").objectReferenceValue = refs.damageSlider;
            so.FindProperty("revealButton").objectReferenceValue = refs.revealButton;
            so.FindProperty("curvePanel").objectReferenceValue = refs.curvePanel;
            so.FindProperty("curveArea").objectReferenceValue = refs.curveArea;
            so.FindProperty("curveLine").objectReferenceValue = refs.curveLine;
            so.FindProperty("curveFill").objectReferenceValue = refs.curveFill;
            so.FindProperty("curveMarker").objectReferenceValue = refs.curveMarker;
            so.FindProperty("formulaText").objectReferenceValue = refs.formulaText;
            so.FindProperty("valueReadout").objectReferenceValue = refs.valueReadout;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedProperty arrayProp, Object[] values) {
            arrayProp.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
