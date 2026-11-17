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
    /// Arma (o rearma desde cero) la escena completa del Ejercicio 3 del Módulo 2 ("¿A quién curás
    /// primero?") — Canvas, dos tarjetas de personaje (Reinhardt a la izquierda, Reaper a la derecha),
    /// cada una con retrato + aro de urgencia + resplandor + badge de resultado, más el área de
    /// instrucción/resultado/puntaje y el botón de siguiente ronda — como GameObjects guardados en
    /// <c>Assets/Scenes/FuzzyComparisonDemo.unity</c>, y deja las referencias de
    /// <see cref="FuzzyComparisonDemo"/> ya conectadas. Mismo patrón que
    /// <see cref="Module2DamageDemoBuilder"/>, aplicado a una pantalla de dos personajes.
    /// </summary>
    /// <remarks>
    /// Correr <c>Tools → Neuralizer → Módulo 2 → Configurar sprites del mockup</c> ANTES de esta herramienta
    /// al menos una vez, para que los retratos en <c>Resources/Sprites/Module2/Heroes/</c> ya estén
    /// importados como Sprite. Idempotente: correrla de nuevo borra el Canvas/EventSystem de una
    /// corrida anterior (marcados con <see cref="Module2ComparisonDemoMarker"/>) y arma todo de nuevo.
    /// </remarks>
    public static class Module2ComparisonDemoBuilder {

        private const string ScenePath = "Assets/Scenes/FuzzyComparisonDemo.unity";
        private const string HeroSpriteFolder = "Sprites/Module2/Heroes/";
        private static Sprite LoadHeroSprite(string name) => Resources.Load<Sprite>(HeroSpriteFolder + name);
        private static Sprite LoadSprite(string name) => Resources.Load<Sprite>("Sprites/Module2/" + name);

        private class HeroCardRefs {
            public Button button;
            public Image[] ring;
            public Image[] glow;
            public TextMeshProUGUI badge;
        }

        private class BuiltRefs {
            public RectTransform canvasRoot;
            public Button backButton;
            public HeroCardRefs left;
            public HeroCardRefs right;
            public TextMeshProUGUI instructionText;
            public TextMeshProUGUI resultText;
            public Button nextRoundButton;
            public TextMeshProUGUI scoreText;
        }

        [MenuItem("Tools/Neuralizer/Módulo 2/5. Escena Ejercicio 3 (Triage)", false, 24)]
        public static void Build() {
            Scene scene = OpenOrCreateScene();

            CleanupPrevious();

            BuiltRefs refs = new();
            EnsureCamera();
            EnsureEventSystem();
            refs.canvasRoot = BuildCanvas();

            BuildBackground(refs.canvasRoot);
            BuildTopBar(refs);
            BuildHeroRow(refs);
            BuildResultArea(refs);

            GameObject controllerGO = new("ComparisonDemoController");
            controllerGO.AddComponent<Module2ComparisonDemoMarker>();
            FuzzyComparisonDemo demo = controllerGO.AddComponent<FuzzyComparisonDemo>();
            AssignReferences(demo, refs);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(refs.canvasRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();

            Debug.Log("[Module2ComparisonDemoBuilder] Ejercicio 3 (Comparación) construido y guardado en " + ScenePath);
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
            foreach (Module2ComparisonDemoMarker marker in Object.FindObjectsByType<Module2ComparisonDemoMarker>()) {
                Undo.DestroyObjectImmediate(marker.gameObject);
            }
            foreach (Canvas c in Object.FindObjectsByType<Canvas>()) {
                if (c.gameObject.name == "Canvas") Object.DestroyImmediate(c.gameObject);
            }
        }

        /// <summary>Misma razón que en Module2DamageDemoBuilder: sin cámara el Game view tapa todo con
        /// el aviso "No cameras rendering" aunque el Canvas sea ScreenSpaceOverlay.</summary>
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
            Debug.Log("[Module2ComparisonDemoBuilder] Escena agregada a Build Settings.");
        }

        // -----------------------------------------------------------------------------------------
        // Canvas / fondo / barra superior
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
            // Fondo ambiental común del sistema "Tactical Neon" (MODULO2_DISENO.md). Si todavía no se
            // generaron los recursos visuales, cae solo a un degradado azul noche plano.
            EsportsUIKit.CreateAmbientBackground(canvasRoot);
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
            EsportsUIKit.CreateTextFill(titleRT, "¿A QUIÉN CURAS PRIMERO?", EsportsTheme.TextSize.Subtitle, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center);
        }

        // -----------------------------------------------------------------------------------------
        // Fila de dos personajes: retrato + aro de urgencia + resplandor + badge, cada uno clickeable
        // -----------------------------------------------------------------------------------------

        private static void BuildHeroRow(BuiltRefs refs) {
            RectTransform row = EsportsUIKit.CreateRect("HeroRow", refs.canvasRoot);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(-20f, 460f);
            row.anchoredPosition = new Vector2(0f, -96f);

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            refs.left = BuildHeroCard(row, "HeroCard_Left", "Hero_Reinhardt");
            refs.right = BuildHeroCard(row, "HeroCard_Right", "Hero_Reaper");
        }

        private static HeroCardRefs BuildHeroCard(RectTransform parent, string name, string heroResourceName) {
            RectTransform card = EsportsUIKit.CreateRect(name, parent);

            RectTransform portraitArea = EsportsUIKit.CreateRect("PortraitArea", card);
            portraitArea.anchorMin = new Vector2(0.5f, 1f);
            portraitArea.anchorMax = new Vector2(0.5f, 1f);
            portraitArea.pivot = new Vector2(0.5f, 1f);
            portraitArea.sizeDelta = new Vector2(280f, 380f);
            portraitArea.anchoredPosition = new Vector2(0f, -40f);

            // Resplandor detrás de todo — arranca verde, el color real lo fija
            // FuzzyComparisonDemo.ApplyUrgencyColor en cada Update().
            RectTransform glow = EsportsUIKit.CreateGlowDot(portraitArea, EsportsTheme.NeonGreen, 220f, 100f);
            glow.anchorMin = new Vector2(0.5f, 0.5f);
            glow.anchorMax = new Vector2(0.5f, 0.5f);
            glow.anchoredPosition = Vector2.zero;
            Image[] glowLayers = glow.GetComponentsInChildren<Image>();

            Sprite heroSprite = LoadHeroSprite(heroResourceName);
            RectTransform portrait = EsportsUIKit.CreateRect("Portrait", portraitArea);
            EsportsUIKit.Stretch(portrait, 16f, 16f, 16f, 50f);
            Image heroImage;
            if (heroSprite) {
                heroImage = EsportsUIKit.CreateSpriteImage("Image", portrait, heroSprite);
                heroImage.preserveAspect = true;
                EsportsUIKit.Stretch(heroImage.rectTransform);
            }
            else {
                heroImage = EsportsUIKit.CreateImage("Image", portrait, EsportsTheme.BgPanelLight);
                EsportsUIKit.Stretch(heroImage.rectTransform);
            }
            heroImage.raycastTarget = false;

            // Aro de urgencia (4 barras independientes) alrededor del retrato.
            RectTransform ringRT = EsportsUIKit.CreateRect("Ring", portraitArea);
            EsportsUIKit.Stretch(ringRT, -12f, -12f, -12f, -12f);
            EsportsUIKit.CreateOutline(ringRT, EsportsTheme.NeonGreen, 5f, out Image[] ring);
            foreach (Image bar in ring) bar.raycastTarget = false;

            // Badge "PEOR" — oculto (texto vacío) hasta que FuzzyComparisonDemo revela el resultado.
            TextMeshProUGUI badge = EsportsUIKit.CreateTextFill(
                EsportsUIKit.CreateRect("BadgeHolder", portraitArea), "", EsportsTheme.TextSize.Subtitle,
                EsportsTheme.NeonRed, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform badgeHolderRT = (RectTransform)badge.transform.parent;
            badgeHolderRT.anchorMin = new Vector2(0.5f, 1f);
            badgeHolderRT.anchorMax = new Vector2(0.5f, 1f);
            badgeHolderRT.pivot = new Vector2(0.5f, 1f);
            badgeHolderRT.sizeDelta = new Vector2(200f, 36f);
            badgeHolderRT.anchoredPosition = new Vector2(0f, 10f);

            // Overlay transparente invisible, clickeable en toda el área de la tarjeta — así el jugador
            // "toca al personaje" en vez de buscar un botón separado, igual que tocar un aliado dañado
            // en el minimapa de un shooter de equipo.
            RectTransform overlay = EsportsUIKit.CreateRect("ClickOverlay", portraitArea);
            EsportsUIKit.Stretch(overlay);
            Image overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0f);
            Button button = overlay.gameObject.AddComponent<Button>();
            button.targetGraphic = overlayImg;

            return new HeroCardRefs { button = button, ring = ring, glow = glowLayers, badge = badge };
        }

        // -----------------------------------------------------------------------------------------
        // Instrucción / resultado / puntaje / siguiente ronda
        // -----------------------------------------------------------------------------------------

        private static void BuildResultArea(BuiltRefs refs) {
            RectTransform container = EsportsUIKit.CreateAccentCard(refs.canvasRoot, "ResultCard", EsportsTheme.NeonBlend, 2f, 6f);
            RectTransform root = (RectTransform)container.parent.parent;
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(-22f, 260f);
            root.anchoredPosition = new Vector2(0f, 20f);

            VerticalLayoutGroup v = container.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.MiddleCenter;

            refs.instructionText = EsportsUIKit.CreateText(container, "¿A quién curas primero? Elige sin pensarlo: mira el contorno.",
                EsportsTheme.TextSize.Body, EsportsTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, 0f);
            refs.resultText = EsportsUIKit.CreateText(container, "", EsportsTheme.TextSize.Subtitle, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center, 0f);
            refs.scoreText = EsportsUIKit.CreateText(container, "Aciertos: 0/0", EsportsTheme.TextSize.Small, EsportsTheme.TextSecondary,
                FontStyles.Normal, TextAlignmentOptions.Center, 0f);

            refs.nextRoundButton = EsportsUIKit.CreateGhostButton(container, "Siguiente ronda", 260f, EsportsTheme.Metrics.TouchTarget);
            refs.nextRoundButton.gameObject.SetActive(false);
        }

        // -----------------------------------------------------------------------------------------

        private static void AssignReferences(FuzzyComparisonDemo demo, BuiltRefs refs) {
            SerializedObject so = new(demo);
            so.FindProperty("canvasRoot").objectReferenceValue = refs.canvasRoot;
            so.FindProperty("backButton").objectReferenceValue = refs.backButton;

            so.FindProperty("leftButton").objectReferenceValue = refs.left.button;
            SetArray(so.FindProperty("leftRing"), refs.left.ring);
            SetArray(so.FindProperty("leftGlow"), refs.left.glow);
            so.FindProperty("leftBadge").objectReferenceValue = refs.left.badge;

            so.FindProperty("rightButton").objectReferenceValue = refs.right.button;
            SetArray(so.FindProperty("rightRing"), refs.right.ring);
            SetArray(so.FindProperty("rightGlow"), refs.right.glow);
            so.FindProperty("rightBadge").objectReferenceValue = refs.right.badge;

            so.FindProperty("instructionText").objectReferenceValue = refs.instructionText;
            so.FindProperty("resultText").objectReferenceValue = refs.resultText;
            so.FindProperty("nextRoundButton").objectReferenceValue = refs.nextRoundButton;
            so.FindProperty("scoreText").objectReferenceValue = refs.scoreText;
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
