using System.Linq;
using Neuralizer.Data;
using Neuralizer.Fuzzy;
using Neuralizer.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neuralizer.EditorTools {


    public static class Module2LevelSceneBuilder {

        public const string ScenePath = "Assets/Scenes/FuzzyLevel.unity";
        public const string SceneName = "FuzzyLevel";
        private const string MedalsPath = "Assets/ScriptableObjects/MedalsDB.asset";
        private const float BottomBarHeight = 150f;

        private static Sprite LoadSprite(string name) => Resources.Load<Sprite>("Sprites/Module2/" + name);

        private class BuiltRefs {
            public RectTransform canvasRoot;
            public Button backButton;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI narrativeText;
            public RectTransform casesContainer;
            public RectTransform viewTabsContainer;
            public RectTransform plotArea;
            public RawImage surfaceImage;
            public TextMeshProUGUI plotCaption;
            public TextMeshProUGUI controlsHint;
            public RectTransform controlsContent;
            public TextMeshProUGUI statusText;
            public Button evaluateButton;
            public GameObject resultPanel;
            public M2Popup resultPopup;
            public M2Pulse resultPulse;
            public TextMeshProUGUI resultTitle;
            public TextMeshProUGUI resultDetail;
            public Button retryButton;
            public Button menuButton;
        }

        [MenuItem("Tools/Neuralizer/Módulo 2/4. Escena de ejercicios (niveles)", false, 23)]
        public static void Build() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = OpenOrCreateScene();
            CleanupPrevious();

            EnsureCamera();
            EnsureEventSystem();

            BuiltRefs refs = new() { canvasRoot = BuildCanvas() };
            BuildBackground(refs.canvasRoot);
            BuildTopBar(refs);
            BuildMainColumn(refs);
            BuildBottomBar(refs);
            BuildResultPanel(refs);

            GameObject hostGO = new("FuzzyHost");
            hostGO.AddComponent<Module2LevelMarker>();
            FuzzyHost host = hostGO.AddComponent<FuzzyHost>();
            MedalsAsset medals = AssetDatabase.LoadAssetAtPath<MedalsAsset>(MedalsPath);
            if (medals) {
                SerializedObject hostSO = new(host);
                hostSO.FindProperty("medals").objectReferenceValue = medals;
                hostSO.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject controllerGO = new("LevelController");
            controllerGO.AddComponent<Module2LevelMarker>();
            FuzzyLevelController controller = controllerGO.AddComponent<FuzzyLevelController>();
            AssignReferences(controller, host, refs);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(refs.canvasRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            Debug.Log("[Module2LevelSceneBuilder] Escena de niveles construida y guardada en " + ScenePath);
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
            foreach (Module2LevelMarker marker in Object.FindObjectsByType<Module2LevelMarker>()) {
                Object.DestroyImmediate(marker.gameObject);
            }
        }

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
            Debug.Log("[Module2LevelSceneBuilder] Escena agregada a Build Settings.");
        }

        // -----------------------------------------------------------------------------------------
        // Canvas / fondo / barra superior
        // -----------------------------------------------------------------------------------------

        private static RectTransform BuildCanvas() {
            GameObject canvasGO = new("Canvas");
            canvasGO.AddComponent<Module2LevelMarker>();
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
            refs.titleText = EsportsUIKit.CreateTextFill(titleRT, "NIVEL", EsportsTheme.TextSize.Title, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center);

            RectTransform spacer = EsportsUIKit.CreateRect("Spacer", topBar);
            spacer.gameObject.AddComponent<LayoutElement>().preferredWidth = backSprite ? 44f : 100f;
        }

        // -----------------------------------------------------------------------------------------
        // Columna principal: contexto, casos, gráfica, controles
        // -----------------------------------------------------------------------------------------


        private static void BuildMainColumn(BuiltRefs refs) {
            RectTransform column = EsportsUIKit.CreateScrollArea(refs.canvasRoot, 0f);
            RectTransform scrollRoot = (RectTransform)column.parent.parent;
            scrollRoot.name = "PageScroll";
            EsportsUIKit.Stretch(scrollRoot, 0f, 0f, 92f, BottomBarHeight + 16f);
            VerticalLayoutGroup v = column.GetComponent<VerticalLayoutGroup>();
            v.spacing = EsportsTheme.Metrics.GapLarge;
            v.padding = new RectOffset(16, 16, 8, 24);

            RectTransform narrative = AutoHeightCard(column, "NarrativeCard", EsportsTheme.NeonBlend);
            refs.narrativeText = EsportsUIKit.CreateText(narrative, "", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary,
                FontStyles.Normal, TextAlignmentOptions.Left, 0f);

            refs.casesContainer = EsportsUIKit.CreateRect("CasesGrid", column);
            GridLayoutGroup grid = refs.casesContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(EsportsTheme.Metrics.Gap, EsportsTheme.Metrics.Gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.cellSize = new Vector2(216f, 170f);

            LayoutElement gridLE = refs.casesContainer.gameObject.AddComponent<LayoutElement>();
            gridLE.minWidth = 0f;
            gridLE.layoutPriority = 2;

            // Gráfica
            RectTransform graph = EsportsUIKit.CreateAccentCard(column, "GraphCard", EsportsTheme.NeonBlend, 2f);
            ((RectTransform)graph.parent.parent).gameObject.AddComponent<LayoutElement>().preferredHeight = 440f;
            VerticalLayoutGroup gv = graph.gameObject.AddComponent<VerticalLayoutGroup>();
            gv.spacing = 14f;
            gv.childControlWidth = true;
            gv.childControlHeight = true;
            gv.childForceExpandWidth = true;
            gv.childForceExpandHeight = false;

            refs.viewTabsContainer = EsportsUIKit.CreateRect("ViewTabs", graph);
            LayoutElement tabsLE = refs.viewTabsContainer.gameObject.AddComponent<LayoutElement>();
            tabsLE.preferredHeight = EsportsTheme.Metrics.TouchTarget;
            tabsLE.minHeight = EsportsTheme.Metrics.TouchTarget;
    
            tabsLE.flexibleHeight = 0f;
            tabsLE.layoutPriority = 2;
            HorizontalLayoutGroup th = refs.viewTabsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 8f;
            th.childAlignment = TextAnchor.MiddleLeft;
            th.childControlWidth = true;
            th.childControlHeight = true;
            th.childForceExpandWidth = false;
            th.childForceExpandHeight = false;

            RectTransform plotHolder = EsportsUIKit.CreateRect("PlotHolder", graph);
            plotHolder.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Image plotBg = EsportsUIKit.CreateImage("PlotBg", plotHolder, new Color(0f, 0f, 0f, 0.25f));
            EsportsUIKit.Stretch(plotBg.rectTransform);
            plotBg.raycastTarget = false;

 
            refs.plotArea = EsportsUIKit.CreateRect("PlotArea", plotHolder);
            EsportsUIKit.Stretch(refs.plotArea, 16f, 16f, 52f, 16f);
            refs.plotArea.pivot = Vector2.zero;

            Image axisX = EsportsUIKit.CreateImage("AxisX", refs.plotArea, new Color(1f, 1f, 1f, 0.25f));
            axisX.raycastTarget = false;
            axisX.rectTransform.anchorMin = Vector2.zero;
            axisX.rectTransform.anchorMax = new Vector2(1f, 0f);
            axisX.rectTransform.pivot = new Vector2(0.5f, 0f);
            axisX.rectTransform.sizeDelta = new Vector2(0f, 1f);
            axisX.rectTransform.anchoredPosition = Vector2.zero;

            GameObject surfaceGO = new("Surface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            surfaceGO.transform.SetParent(refs.plotArea, false);
            refs.surfaceImage = surfaceGO.GetComponent<RawImage>();
            refs.surfaceImage.raycastTarget = false;
            EsportsUIKit.Stretch(refs.surfaceImage.rectTransform);
            surfaceGO.SetActive(false);

            refs.plotCaption = EsportsUIKit.CreateText(graph, "", EsportsTheme.TextSize.Small, EsportsTheme.TextSecondary,
                FontStyles.Normal, TextAlignmentOptions.Center, 30f);


            RectTransform controls = AutoHeightCard(column, "ControlsCard", EsportsTheme.NeonBlend);
            refs.controlsHint = EsportsUIKit.CreateText(controls, "", EsportsTheme.TextSize.Body, EsportsTheme.TextSecondary,
                FontStyles.Italic, TextAlignmentOptions.Left, 0f);
            refs.controlsContent = EsportsUIKit.CreateRect("Controls", controls);
            VerticalLayoutGroup cv = refs.controlsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.spacing = EsportsTheme.Metrics.Gap;
            cv.childControlWidth = true;
            cv.childControlHeight = true;
            cv.childForceExpandWidth = true;
            cv.childForceExpandHeight = false;
        }


        internal static RectTransform AutoHeightCard(Transform parent, string name, Color accent) {
            RectTransform root = EsportsUIKit.CreateRect(name, parent);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = EsportsTheme.WithAlpha(EsportsTheme.BgPanel, 0.92f);
            bg.raycastTarget = false;
            if (EsportsSkin.HasSprites) {
                bg.sprite = EsportsSkin.ChamferFill;
                bg.type = Image.Type.Sliced;
            }
            VerticalLayoutGroup v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 16, 18);
            v.spacing = EsportsTheme.Metrics.Gap;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            if (EsportsSkin.HasSprites) {
                Image line = EsportsUIKit.CreateSpriteImage("Outline", root, EsportsSkin.ChamferLine, Image.Type.Sliced);
                EsportsUIKit.Stretch(line.rectTransform);
                line.color = EsportsTheme.WithAlpha(accent, 0.35f);
                line.raycastTarget = false;
                line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            else {
                EsportsUIKit.CreateOutline(root, new Color(accent.r, accent.g, accent.b, 0.35f), 1.5f, out Image[] bars);
                foreach (Image bar in bars) {
                    bar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                }
            }
            return root;
        }

        // -----------------------------------------------------------------------------------------
        // Barra inferior: estado en vivo + Evaluar
        // -----------------------------------------------------------------------------------------

        private static void BuildBottomBar(BuiltRefs refs) {
            RectTransform bar = EsportsUIKit.CreateRect("BottomBar", refs.canvasRoot);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, BottomBarHeight);
            bar.anchoredPosition = Vector2.zero;
   
            Image barBg = bar.gameObject.AddComponent<Image>();
            barBg.color = new Color(EsportsTheme.BgDeep.r, EsportsTheme.BgDeep.g, EsportsTheme.BgDeep.b, 0.94f);
            VerticalLayoutGroup v = bar.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 10, 16);
            v.spacing = 8f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            refs.statusText = EsportsUIKit.CreateTextFill(bar, "", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center);
            refs.statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;

  
            refs.evaluateButton = EsportsUIKit.CreateGradientButton(bar, "Evaluar", 64f);
        }

        // -----------------------------------------------------------------------------------------
        // Popup de resultado
        // -----------------------------------------------------------------------------------------

        private static void BuildResultPanel(BuiltRefs refs) {
            Image dim = EsportsUIKit.CreateImage("ResultPanel", refs.canvasRoot, EsportsTheme.WithAlpha(EsportsTheme.BgDeep, 0.78f));
            EsportsUIKit.Stretch(dim.rectTransform);
            refs.resultPanel = dim.gameObject;
            dim.gameObject.AddComponent<CanvasGroup>();
            refs.resultPopup = dim.gameObject.AddComponent<M2Popup>();

            // Halo detrás de la tarjeta
            if (EsportsSkin.HasSprites) {
                Image pulse = EsportsUIKit.CreateSpriteImage("SuccessPulse", dim.rectTransform, EsportsSkin.ChamferGlow, Image.Type.Sliced);
                pulse.raycastTarget = false;
                pulse.rectTransform.anchorMin = pulse.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                pulse.rectTransform.sizeDelta = new Vector2(680f, 520f);
                pulse.color = EsportsTheme.WithAlpha(EsportsTheme.NeonGreen, 0f);
                refs.resultPulse = pulse.gameObject.AddComponent<M2Pulse>();
            }

            RectTransform content = EsportsUIKit.CreateAccentCard(dim.rectTransform, "ResultCard", EsportsTheme.NeonBlend, 2f);
            RectTransform card = (RectTransform)content.parent.parent;
            refs.resultPopup.SetCard(card);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(620f, 460f);
            card.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = EsportsTheme.Metrics.GapLarge;
            v.padding = new RectOffset(20, 20, 20, 20);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            refs.resultTitle = EsportsUIKit.CreateText(content, "", EsportsTheme.TextSize.Display, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center, 56f);
  
            refs.resultDetail = EsportsUIKit.CreateText(content, "", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary,
                FontStyles.Normal, TextAlignmentOptions.Center, 0f);
            refs.resultDetail.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            refs.resultDetail.enableAutoSizing = true;
            refs.resultDetail.fontSizeMax = EsportsTheme.TextSize.Body;
            refs.resultDetail.fontSizeMin = EsportsTheme.TextSize.Small;

            RectTransform buttons = EsportsUIKit.CreateRect("Buttons", content);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = EsportsTheme.Metrics.TouchTarget;
            HorizontalLayoutGroup h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = EsportsTheme.Metrics.Gap;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;

            refs.retryButton = EsportsUIKit.CreateGhostButton(buttons, "Seguir ajustando", 260f, EsportsTheme.Metrics.TouchTarget);
            refs.menuButton = EsportsUIKit.CreateGhostButton(buttons, "Volver al menú", 260f, EsportsTheme.Metrics.TouchTarget);

            refs.resultPanel.SetActive(false);
        }

        // -----------------------------------------------------------------------------------------

        private static void AssignReferences(FuzzyLevelController controller, FuzzyHost host, BuiltRefs refs) {
            SerializedObject so = new(controller);
            so.FindProperty("host").objectReferenceValue = host;
            so.FindProperty("fallbackLevel").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<FuzzyScene>(Module2LevelAssetsBuilder.Folder + "/Tema 1 Distancia.asset");
            so.FindProperty("backButton").objectReferenceValue = refs.backButton;
            so.FindProperty("titleText").objectReferenceValue = refs.titleText;
            so.FindProperty("narrativeText").objectReferenceValue = refs.narrativeText;
            so.FindProperty("casesContainer").objectReferenceValue = refs.casesContainer;
            so.FindProperty("viewTabsContainer").objectReferenceValue = refs.viewTabsContainer;
            so.FindProperty("plotArea").objectReferenceValue = refs.plotArea;
            so.FindProperty("surfaceImage").objectReferenceValue = refs.surfaceImage;
            so.FindProperty("plotCaption").objectReferenceValue = refs.plotCaption;
            so.FindProperty("controlsHint").objectReferenceValue = refs.controlsHint;
            so.FindProperty("controlsContent").objectReferenceValue = refs.controlsContent;
            so.FindProperty("statusText").objectReferenceValue = refs.statusText;
            so.FindProperty("evaluateButton").objectReferenceValue = refs.evaluateButton;
            so.FindProperty("resultPanel").objectReferenceValue = refs.resultPanel;
            so.FindProperty("resultPopup").objectReferenceValue = refs.resultPopup;
            so.FindProperty("resultPulse").objectReferenceValue = refs.resultPulse;
            so.FindProperty("resultTitle").objectReferenceValue = refs.resultTitle;
            so.FindProperty("resultDetail").objectReferenceValue = refs.resultDetail;
            so.FindProperty("retryButton").objectReferenceValue = refs.retryButton;
            so.FindProperty("menuButton").objectReferenceValue = refs.menuButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
