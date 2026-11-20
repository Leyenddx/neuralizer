using System.Linq;
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

 
    public static class Module2Tema1ExampleBuilder {

        public const string ScenePath = "Assets/Scenes/Tema1Ejemplo.unity";
        public const string SceneName = "Tema1Ejemplo";

        private static readonly string[] StateNames = { "Critical", "Caution", "Fine" };
        private static readonly Color[] StateColors = { EsportsTheme.NeonRed, EsportsTheme.Accent, EsportsTheme.NeonGreen };

        private class BuiltRefs {
            public RectTransform canvasRoot;
            public Button backButton;
            public Image[] heroRing;
            public Image[] glowLayers;
            public TextMeshProUGUI stateText;
            public TextMeshProUGUI[] degreeLabels = new TextMeshProUGUI[3];
            public Image[] degreeBars = new Image[3];
            public Button[] logicButtons;
            public M2TabIndicator logicIndicator;
            public Button[] shapeButtons;
            public M2TabIndicator shapeIndicator;
            public CanvasGroup shapeGroup;
            public RectTransform plotArea;
            public TextMeshProUGUI plotCaption;
            public Slider healthSlider;
            public TextMeshProUGUI healthLabel;
            public TextMeshProUGUI explanationText;
        }

        [MenuItem("Tools/Neuralizer/Módulo 2/3. Escena Tema 1 · Ejemplo (Salud)", false, 22)]
        public static void Build() {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = OpenOrCreateScene();
            foreach (Module2LevelMarker marker in Object.FindObjectsByType<Module2LevelMarker>()) {
                Object.DestroyImmediate(marker.gameObject);
            }
            EnsureCamera();
            EnsureEventSystem();

            BuiltRefs refs = new() { canvasRoot = BuildCanvas() };
            EsportsUIKit.CreateAmbientBackground(refs.canvasRoot);
            BuildTopBar(refs);
            BuildColumn(refs);

            GameObject controllerGO = new("Tema1ExampleController");
            controllerGO.AddComponent<Module2LevelMarker>();
            AssignReferences(controllerGO.AddComponent<FuzzyHealthExample>(), refs);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(refs.canvasRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings(ScenePath);
            Debug.Log("[Module2Tema1ExampleBuilder] Escena construida y guardada en " + ScenePath);
        }

        // -----------------------------------------------------------------------------------------

        private static Scene OpenOrCreateScene() {
            if (System.IO.File.Exists(ScenePath)) return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return scene;
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

      
        public static void RegisterInBuildSettings(string scenePath) {
            var scenes = EditorBuildSettings.scenes.Where(s => System.IO.File.Exists(s.path)).ToList();
            if (scenes.All(s => s.path != scenePath)) scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

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

            Sprite backSprite = Resources.Load<Sprite>("Sprites/Module2/Module2_ButtonBack");
            refs.backButton = backSprite
                ? EsportsUIKit.CreateSpriteIconButton(topBar, backSprite, 44f, 64f, out _)
                : EsportsUIKit.CreateGhostButton(topBar, "‹ Volver", 100f, 64f);

            RectTransform titleRT = EsportsUIKit.CreateRect("Title", topBar);
            titleRT.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            EsportsUIKit.CreateTextFill(titleRT, "Estado de salud", EsportsTheme.TextSize.Title, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Center);

            RectTransform spacer = EsportsUIKit.CreateRect("Spacer", topBar);
            spacer.gameObject.AddComponent<LayoutElement>().preferredWidth = backSprite ? 44f : 100f;
        }

        // -----------------------------------------------------------------------------------------

        private static void BuildColumn(BuiltRefs refs) {
            RectTransform column = EsportsUIKit.CreateRect("MainColumn", refs.canvasRoot);
            EsportsUIKit.Stretch(column, 16f, 16f, 92f, 16f);
            VerticalLayoutGroup v = column.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            BuildHeroRow(column, refs);
            refs.logicButtons = BuildSegmented(column, "Lógica", new[] { "Difusa", "Clásica" }, out refs.logicIndicator, out _);
            refs.shapeButtons = BuildSegmented(column, "Forma", new[] { "Triangular", "Trapezoidal" }, out refs.shapeIndicator, out refs.shapeGroup);
            BuildGraph(column, refs);
            BuildBottom(column, refs);
        }

  
        private static void FixedRow(RectTransform row, float height) {
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            le.layoutPriority = 2;
        }

        private static void BuildHeroRow(RectTransform column, BuiltRefs refs) {
            RectTransform row = EsportsUIKit.CreateRect("HeroRow", column);
            FixedRow(row, 300f);
            HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

       
            RectTransform area = EsportsUIKit.CreateRect("HeroArea", row);
            LayoutElement areaLE = area.gameObject.AddComponent<LayoutElement>();
            areaLE.preferredWidth = 250f;
            areaLE.minWidth = 250f;

            RectTransform glow = EsportsUIKit.CreateGlowDot(area, EsportsTheme.NeonGreen, 180f, 90f);
            glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0.5f);
            glow.anchoredPosition = Vector2.zero;
            refs.glowLayers = glow.GetComponentsInChildren<Image>();
            foreach (Image layer in refs.glowLayers) layer.raycastTarget = false;

            Sprite hero = Resources.Load<Sprite>("Sprites/Module2/Heroes/Hero_Reinhardt");
            Image portrait = hero
                ? EsportsUIKit.CreateSpriteImage("Portrait", area, hero)
                : EsportsUIKit.CreateImage("Portrait", area, EsportsTheme.BgPanelLight);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            EsportsUIKit.Stretch(portrait.rectTransform, 22f, 22f, 22f, 22f);

            RectTransform ringRT = EsportsUIKit.CreateRect("Ring", area);
            EsportsUIKit.Stretch(ringRT, 4f, 4f, 4f, 4f);
            EsportsUIKit.CreateOutline(ringRT, EsportsTheme.NeonGreen, 5f, out refs.heroRing);

         
            RectTransform card = Module2LevelSceneBuilder.AutoHeightCard(row, "ReadoutCard", EsportsTheme.NeonBlend);
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup cv = card.GetComponent<VerticalLayoutGroup>();
            cv.spacing = 6f;
            cv.childAlignment = TextAnchor.MiddleLeft;

            EsportsUIKit.CreateOverline(card, "Estado dominante", EsportsTheme.TextSecondary);
            refs.stateText = EsportsUIKit.CreateText(card, "Fine", EsportsTheme.TextSize.Title, EsportsTheme.NeonGreen,
                FontStyles.Bold, TextAlignmentOptions.Left, 0f);
            refs.stateText.textWrappingMode = TextWrappingModes.NoWrap;

            for (int s = 0; s < 3; s++) {
                refs.degreeLabels[s] = EsportsUIKit.CreateText(card, $"μ {StateNames[s]} = 0.00", EsportsTheme.TextSize.Small,
                    StateColors[s], FontStyles.Normal, TextAlignmentOptions.Left, 0f);
                EsportsSkin.ApplyFont(refs.degreeLabels[s], TypeRole.Mono);
                refs.degreeLabels[s].fontStyle = FontStyles.Bold;
                refs.degreeLabels[s].textWrappingMode = TextWrappingModes.NoWrap;

                RectTransform bar = EsportsUIKit.CreateRect("Bar_" + StateNames[s], card);
                LayoutElement barLE = bar.gameObject.AddComponent<LayoutElement>();
                barLE.preferredHeight = 10f;
                barLE.minHeight = 10f;
                Image track = EsportsUIKit.CreateImage("Track", bar, EsportsTheme.WithAlpha(EsportsTheme.TextPrimary, 0.12f));
                EsportsUIKit.Stretch(track.rectTransform);
                track.raycastTarget = false;
                Image fill = EsportsUIKit.CreateImage("Fill", bar, StateColors[s]);
                EsportsUIKit.Stretch(fill.rectTransform);
                fill.raycastTarget = false;
                fill.sprite = EsportsSkin.Solid;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = 0f;
                refs.degreeBars[s] = fill;
            }
        }

     
        private static Button[] BuildSegmented(RectTransform column, string caption, string[] options,
            out M2TabIndicator tabIndicator, out CanvasGroup group) {
            RectTransform row = EsportsUIKit.CreateRect("Segmented_" + caption, column);
            FixedRow(row, EsportsTheme.Metrics.TouchTarget);
            group = row.gameObject.AddComponent<CanvasGroup>();
            HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            TextMeshProUGUI label = EsportsUIKit.CreateOverline(row, caption, EsportsTheme.TextSecondary, EsportsTheme.TextSize.Small);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            LayoutElement labelLE = label.gameObject.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 110f;
            labelLE.minWidth = 110f;

            RectTransform tabs = EsportsUIKit.CreateRect("Tabs", row);
            tabs.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            HorizontalLayoutGroup th = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 8f;
            th.childControlWidth = true;
            th.childControlHeight = true;
            th.childForceExpandWidth = true;
            th.childForceExpandHeight = true;

            Image indicator = EsportsUIKit.CreateImage("Indicator", tabs, EsportsTheme.WithAlpha(EsportsTheme.NeonBlue, 0.3f));
            if (EsportsSkin.HasSprites) {
                indicator.sprite = EsportsSkin.ChamferFill;
                indicator.type = Image.Type.Sliced;
                indicator.pixelsPerUnitMultiplier = 1.6f;
                Image line = EsportsUIKit.CreateSpriteImage("Line", indicator.rectTransform, EsportsSkin.ChamferLine, Image.Type.Sliced);
                line.pixelsPerUnitMultiplier = 1.6f;
                line.color = EsportsTheme.NeonBlue;
                line.raycastTarget = false;
                EsportsUIKit.Stretch(line.rectTransform);
            }
            indicator.raycastTarget = false;
            indicator.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            tabIndicator = tabs.gameObject.AddComponent<M2TabIndicator>();
            tabIndicator.SetIndicator(indicator.rectTransform);

            Button[] buttons = new Button[options.Length];
            for (int i = 0; i < options.Length; i++) {
                buttons[i] = EsportsUIKit.CreateGhostButton(tabs, options[i], 200f, EsportsTheme.Metrics.TouchTarget);
            }
            return buttons;
        }

        private static void BuildGraph(RectTransform column, BuiltRefs refs) {
            RectTransform graph = EsportsUIKit.CreateAccentCard(column, "GraphCard", EsportsTheme.NeonBlend, 2f);
            LayoutElement graphLE = ((RectTransform)graph.parent.parent).gameObject.AddComponent<LayoutElement>();
            graphLE.flexibleHeight = 1f;
            graphLE.minHeight = 240f;
            VerticalLayoutGroup gv = graph.gameObject.AddComponent<VerticalLayoutGroup>();
            gv.spacing = 8f;
            gv.childControlWidth = true;
            gv.childControlHeight = true;
            gv.childForceExpandWidth = true;
            gv.childForceExpandHeight = false;

            RectTransform plotHolder = EsportsUIKit.CreateRect("PlotHolder", graph);
            plotHolder.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Image plotBg = EsportsUIKit.CreateImage("PlotBg", plotHolder, new Color(0f, 0f, 0f, 0.25f));
            EsportsUIKit.Stretch(plotBg.rectTransform);
            plotBg.raycastTarget = false;

            refs.plotArea = EsportsUIKit.CreateRect("PlotArea", plotHolder);
            EsportsUIKit.Stretch(refs.plotArea, 16f, 16f, 16f, 34f);
            refs.plotArea.pivot = Vector2.zero;

            Image axisX = EsportsUIKit.CreateImage("AxisX", refs.plotArea, new Color(1f, 1f, 1f, 0.25f));
            axisX.raycastTarget = false;
            axisX.rectTransform.anchorMin = Vector2.zero;
            axisX.rectTransform.anchorMax = new Vector2(1f, 0f);
            axisX.rectTransform.pivot = new Vector2(0.5f, 0f);
            axisX.rectTransform.sizeDelta = new Vector2(0f, 1f);
            axisX.rectTransform.anchoredPosition = Vector2.zero;

      
            foreach ((float t, string text, TextAlignmentOptions align) in new[] {
                         (0f, "0", TextAlignmentOptions.TopLeft), (0.5f, "50", TextAlignmentOptions.Top),
                         (1f, "100", TextAlignmentOptions.TopRight) }) {
                TextMeshProUGUI tick = EsportsUIKit.CreateText(refs.plotArea, text, EsportsTheme.TextSize.Caption,
                    EsportsTheme.TextSecondary, FontStyles.Normal, align, 0f);
                EsportsSkin.ApplyFont(tick, TypeRole.Mono);
                RectTransform rt = tick.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(t, 0f);
                rt.pivot = new Vector2(t, 1f);
                rt.sizeDelta = new Vector2(80f, 24f);
                rt.anchoredPosition = new Vector2(0f, -6f);
            }

            refs.plotCaption = EsportsUIKit.CreateText(graph, "", EsportsTheme.TextSize.Small, EsportsTheme.TextSecondary,
                FontStyles.Normal, TextAlignmentOptions.Center, 26f);
        }

        private static void BuildBottom(RectTransform column, BuiltRefs refs) {
            RectTransform card = Module2LevelSceneBuilder.AutoHeightCard(column, "ControlCard", EsportsTheme.NeonBlend);

            refs.explanationText = EsportsUIKit.CreateText(card, "", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary,
                FontStyles.Normal, TextAlignmentOptions.Left, 84f);

            refs.healthLabel = EsportsUIKit.CreateText(card, "Salud del aliado: 30", EsportsTheme.TextSize.Subtitle,
                EsportsTheme.NeonBlue, FontStyles.Bold, TextAlignmentOptions.Left, 0f);

            RectTransform sliderHolder = EsportsUIKit.CreateRect("SliderHolder", card);
            sliderHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = EsportsTheme.Metrics.SliderHeight;
            refs.healthSlider = EsportsUIKit.CreateGradientSlider(sliderHolder, 0f, 100f, 30f);
        }

        // -----------------------------------------------------------------------------------------

        private static void AssignReferences(FuzzyHealthExample example, BuiltRefs refs) {
            SerializedObject so = new(example);
            so.FindProperty("backButton").objectReferenceValue = refs.backButton;
            SetArray(so.FindProperty("heroRing"), refs.heroRing);
            SetArray(so.FindProperty("glowLayers"), refs.glowLayers);
            so.FindProperty("stateText").objectReferenceValue = refs.stateText;
            SetArray(so.FindProperty("degreeLabels"), refs.degreeLabels);
            SetArray(so.FindProperty("degreeBars"), refs.degreeBars);
            SetArray(so.FindProperty("logicButtons"), refs.logicButtons);
            so.FindProperty("logicIndicator").objectReferenceValue = refs.logicIndicator;
            SetArray(so.FindProperty("shapeButtons"), refs.shapeButtons);
            so.FindProperty("shapeIndicator").objectReferenceValue = refs.shapeIndicator;
            so.FindProperty("shapeGroup").objectReferenceValue = refs.shapeGroup;
            so.FindProperty("plotArea").objectReferenceValue = refs.plotArea;
            so.FindProperty("plotCaption").objectReferenceValue = refs.plotCaption;
            so.FindProperty("healthSlider").objectReferenceValue = refs.healthSlider;
            so.FindProperty("healthLabel").objectReferenceValue = refs.healthLabel;
            so.FindProperty("explanationText").objectReferenceValue = refs.explanationText;
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
