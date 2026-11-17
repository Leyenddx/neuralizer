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
    /// Arma (o rearma desde cero) toda la jerarquía visual de la pantalla de ejercicio del Módulo 2
    /// — Canvas, barra superior, gráfica, tarjeta inferior, popup de resultado — como GameObjects
    /// reales guardados en la escena, y deja las referencias de <see cref="FuzzyHealerDemo"/> ya
    /// conectadas. Correrlo es lo que reemplaza al viejo "la UI se construye en código en Start()":
    /// ahora se construye UNA VEZ acá, en el editor, y desde ese momento es una jerarquía Unity normal
    /// — se puede mover, redimensionar y retocar a mano, con el resultado visible en la escena/Game
    /// view SIN apretar Play.
    /// </summary>
    /// <remarks>
    /// Las medidas usadas acá son las mismas, medidas directo del mockup, que tenía el viejo código de
    /// construcción en tiempo de ejecución — ver GUIA_AJUSTE_TAMANOS.md para el detalle de qué medía
    /// cada una. Los 5 valores que además afectan lógica en RUNTIME (pantalla completa, escala de la
    /// gráfica) viven como <c>public const</c> en <see cref="FuzzyHealerDemo"/> y se leen de ahí
    /// (<see cref="FuzzyHealerDemo.GraphSideMargin"/> y compañía) para que este builder y el
    /// comportamiento en juego nunca queden desincronizados. El resto de las medidas (específicas de
    /// la construcción inicial, no de lógica) viven solo acá como constantes locales — una vez armada
    /// la jerarquía, esos valores se ajustan directo en el Inspector de cada objeto, no hay necesidad
    /// de que sigan existiendo como código.
    ///
    /// Es seguro correr esta herramienta más de una vez: primero borra cualquier Canvas/EventSystem
    /// generado por una corrida anterior (identificados por el componente marcador
    /// <see cref="Module2HudMarker"/>) y arma todo de nuevo — útil para volver a la base "de fábrica"
    /// después de desordenar el layout a propósito. Objetos que el usuario haya agregado a mano FUERA
    /// de esta jerarquía (o cuyo GameObject raíz no tenga el marcador) no se tocan.
    /// </remarks>
    public static class Module2HudBuilder {

        // -----------------------------------------------------------------------------------------
        // Medidas de construcción (solo se usan acá, una vez, para dejar la jerarquía en su posición
        // inicial medida del mockup — después de correr el builder se ajustan a mano en el Inspector).
        // -----------------------------------------------------------------------------------------

        private const float TopBarMargin = 16f;
        private const float TopBarHeight = 64f;
        private const float TopBarSideMargin = 12f;
        private const float TopBarInnerSpacing = 10f;

        private const float GraphTopGap = 95f;

        private const float PlotLeftMargin = 26f;
        private const float PlotRightMargin = 10f;
        private const float PlotTopMargin = 16f;
        private const float PlotBottomMargin = 40f;

        private const float BottomSheetSideMargin = 11f;
        private const float BottomSheetPaddingLeft = 16f;
        private const float BottomSheetPaddingRight = 16f;
        private const float BottomSheetPaddingTop = 70f;
        private const float BottomSheetPaddingBottom = 20f;
        private const float BottomSheetSpacing = 16f;

        private const float DropdownWidthFrac = 0.60f;
        private const float FormulaWidthFrac = 0.32f;
        private const float WeightRowWidthFrac = 0.77f;
        private const float EvaluarWidthFrac = 0.24f;

        private const string SpriteFolder = "Sprites/Module2/";
        private static Sprite LoadSprite(string name) => Resources.Load<Sprite>(SpriteFolder + name);

        // Referencias recolectadas mientras se arma la jerarquía — al final se vuelcan todas de una
        // en el componente FuzzyHealerDemo vía SerializedObject.
        private class BuiltRefs {
            public RectTransform canvasRoot;
            public Button backButton;
            public Button fullscreenButton;
            public Image fullscreenIcon;
            public RectTransform graphCardRoot;
            public RectTransform graphArea;
            public UIFilledCurve bajoFill;
            public UIFilledCurve altoFill;
            public UIPolyline bajoLine;
            public UIPolyline altoLine;
            public RectTransform bottomSheetRoot;
            public TMP_Dropdown functionDropdown;
            public TextMeshProUGUI formulaText;
            public Slider weightSlider;
            public TextMeshProUGUI weightLabel;
            public Image[] reinhardtRing;
            public Image[] dvaRing;
            public Button evaluarButton;
            public RectTransform resultBackdrop;
            public Button resultBackdropButton;
            public Image resultBorder;
            public TextMeshProUGUI resultText;
            public Button resultCloseButton;
        }

        [MenuItem("Tools/Neuralizer/Módulo 2/Material anterior/Escena Healer", false, 61)]
        public static void BuildHud() {
            FuzzyHealerDemo demo = Object.FindAnyObjectByType<FuzzyHealerDemo>();
            if (!demo) {
                EditorUtility.DisplayDialog("Reconstruir HUD del Ejercicio (Módulo 2)",
                    "No se encontró ningún FuzzyHealerDemo en la escena abierta. Abre FuzzyDemo.unity y vuelve a intentarlo.",
                    "OK");
                return;
            }

            // En modo batch (Module2BatchPipeline) no hay quien conteste el diálogo: se asume "sí".
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Reconstruir HUD del Ejercicio (Módulo 2)",
                    "Esto borra el Canvas/EventSystem generados por una corrida anterior de esta herramienta " +
                    "(si existen) y arma todo de nuevo con las medidas base del mockup. Cualquier ajuste manual " +
                    "que hayas hecho en el Inspector sobre esos objetos se pierde. ¿Continuar?",
                    "Sí, reconstruir", "Cancelar")) {
                return;
            }

            CleanupPreviousHud();

            demo.gameObject.name = "HealerDemoController";

            BuiltRefs refs = new();

            EnsureEventSystem();
            refs.canvasRoot = BuildCanvas();

            BuildBackground(refs.canvasRoot);
            BuildTopBar(refs);
            BuildGraphCard(refs);
            BuildBottomSheet(refs);
            BuildResultPopup(refs);

            SeedPreviewCurves(refs);

            AssignReferences(demo, refs);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(refs.canvasRoot);

            EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
            Debug.Log("[Module2HudBuilder] HUD del Módulo 2 reconstruido y referencias conectadas en FuzzyHealerDemo.");
        }

        // -----------------------------------------------------------------------------------------
        // Limpieza (para que correr la herramienta de nuevo sea seguro / idempotente)
        // -----------------------------------------------------------------------------------------

        private static void CleanupPreviousHud() {
            foreach (Module2HudMarker marker in Object.FindObjectsByType<Module2HudMarker>()) {
                Undo.DestroyObjectImmediate(marker.gameObject);
            }
        }

        private static void EnsureEventSystem() {
            if (Object.FindAnyObjectByType<EventSystem>()) return;
            GameObject es = new("EventSystem");
            Undo.RegisterCreatedObjectUndo(es, "Crear EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // -----------------------------------------------------------------------------------------
        // Canvas / fondo
        // -----------------------------------------------------------------------------------------

        private static RectTransform BuildCanvas() {
            GameObject canvasGO = new("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Crear Canvas HUD Módulo 2");
            canvasGO.AddComponent<Module2HudMarker>();

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

        // -----------------------------------------------------------------------------------------
        // Barra superior
        // -----------------------------------------------------------------------------------------

        private static void BuildTopBar(BuiltRefs refs) {
            RectTransform topBar = EsportsUIKit.CreateRect("TopBar", refs.canvasRoot);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(-2f * TopBarSideMargin, TopBarHeight);
            topBar.anchoredPosition = new Vector2(0f, -TopBarMargin);

            HorizontalLayoutGroup topBarLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            topBarLayout.spacing = TopBarInnerSpacing;
            topBarLayout.childAlignment = TextAnchor.MiddleCenter;
            topBarLayout.childControlWidth = true;
            topBarLayout.childControlHeight = true;
            topBarLayout.childForceExpandWidth = false;
            topBarLayout.childForceExpandHeight = true;

            Sprite backSprite = LoadSprite("Module2_ButtonBack");
            if (backSprite) {
                refs.backButton = EsportsUIKit.CreateSpriteIconButton(topBar, backSprite, 44f, TopBarHeight, out _);
            }
            else {
                refs.backButton = EsportsUIKit.CreateGhostButton(topBar, "‹ Volver", 100f, TopBarHeight);
            }

            // Título dentro del banner hexagonal del mockup — si falta el sprite cae de vuelta al texto
            // simple (sin fondo).
            Sprite bannerSprite = LoadSprite("Module2_Banner");
            RectTransform titleRT;
            if (bannerSprite) {
                titleRT = EsportsUIKit.CreateSpriteCard(topBar, "Title", bannerSprite, 24f, 24f, 4f, 4f);
                // flexibleWidth (no preferredWidth fijo): el banner se estira para llenar todo el
                // espacio entre los botones.
                ((RectTransform)titleRT.parent).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }
            else {
                titleRT = EsportsUIKit.CreateRect("Title", topBar);
                titleRT.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }
            EsportsUIKit.CreateTextFill(titleRT, "Ejercicio 1", EsportsTheme.TextSize.Title, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);

            Sprite fullscreenSprite = LoadSprite("Module2_ButtonFullscreen");
            if (fullscreenSprite) {
                refs.fullscreenButton = EsportsUIKit.CreateSpriteIconButton(topBar, fullscreenSprite, 44f, TopBarHeight, out refs.fullscreenIcon);
            }
            else {
                // Se evitan glifos Unicode poco comunes (ej. ⛶): la fuente TMP del proyecto (LiberationSans
                // SDF) no los tiene y Unity los reemplaza por un cuadro de reemplazo.
                refs.fullscreenButton = EsportsUIKit.CreateGhostButton(topBar, "Completa", 140f, TopBarHeight);
            }
        }

        // -----------------------------------------------------------------------------------------
        // Gráfica
        // -----------------------------------------------------------------------------------------

        private static void BuildGraphCard(BuiltRefs refs) {
            RectTransform graphCardRoot = EsportsUIKit.CreateRect("GraphArea", refs.canvasRoot);
            graphCardRoot.anchorMin = Vector2.zero;
            graphCardRoot.anchorMax = Vector2.one;
            graphCardRoot.offsetMin = new Vector2(FuzzyHealerDemo.GraphSideMargin,
                FuzzyHealerDemo.BottomSheetHeight + FuzzyHealerDemo.BottomSheetMargin + FuzzyHealerDemo.GraphBottomGap);
            graphCardRoot.offsetMax = new Vector2(-FuzzyHealerDemo.GraphSideMargin, -(TopBarMargin + TopBarHeight + GraphTopGap));
            refs.graphCardRoot = graphCardRoot;

            RectTransform plotArea = EsportsUIKit.CreateRect("PlotArea", graphCardRoot);
            plotArea.anchorMin = Vector2.zero;
            plotArea.anchorMax = Vector2.one;
            plotArea.offsetMin = new Vector2(PlotLeftMargin, PlotBottomMargin);
            plotArea.offsetMax = new Vector2(-PlotRightMargin, -PlotTopMargin);
            plotArea.pivot = Vector2.zero;
            refs.graphArea = plotArea;

            BuildAxis(plotArea);

            // Relleno primero (debajo), borde nítido encima.
            refs.bajoFill = EsportsUIKit.CreateFilledCurve(plotArea, new Color(EsportsTheme.CurveBajo.r, EsportsTheme.CurveBajo.g, EsportsTheme.CurveBajo.b, 0.75f));
            refs.altoFill = EsportsUIKit.CreateFilledCurve(plotArea, new Color(EsportsTheme.CurveAlto.r, EsportsTheme.CurveAlto.g, EsportsTheme.CurveAlto.b, 0.75f));
            refs.bajoLine = EsportsUIKit.CreatePolyline(plotArea, EsportsTheme.CurveBajo, 3f);
            refs.altoLine = EsportsUIKit.CreatePolyline(plotArea, EsportsTheme.CurveAlto, 3f);
        }

        /// <summary>Eje X/Y tal como el mockup: una línea por eje con marcas cortas perpendiculares, sin números ni grilla interna.</summary>
        private static void BuildAxis(RectTransform plotArea) {
            Color axisColor = EsportsTheme.TextPrimary;

            RectTransform yAxis = EsportsUIKit.CreateRect("YAxis", plotArea);
            yAxis.anchorMin = new Vector2(0f, 0f);
            yAxis.anchorMax = new Vector2(0f, 1f);
            yAxis.pivot = new Vector2(0.5f, 0.5f);
            yAxis.sizeDelta = new Vector2(2f, 0f);
            yAxis.anchoredPosition = Vector2.zero;
            Image yAxisImg = yAxis.gameObject.AddComponent<Image>();
            yAxisImg.color = axisColor;
            yAxisImg.raycastTarget = false;

            RectTransform xAxis = EsportsUIKit.CreateRect("XAxis", plotArea);
            xAxis.anchorMin = new Vector2(0f, 0f);
            xAxis.anchorMax = new Vector2(1f, 0f);
            xAxis.pivot = new Vector2(0.5f, 0.5f);
            xAxis.sizeDelta = new Vector2(0f, 2f);
            xAxis.anchoredPosition = Vector2.zero;
            Image xAxisImg = xAxis.gameObject.AddComponent<Image>();
            xAxisImg.color = axisColor;
            xAxisImg.raycastTarget = false;

            const int yTickCount = 8;
            for (int i = 0; i <= yTickCount; i++) {
                float t = i / (float)yTickCount;
                RectTransform tick = EsportsUIKit.CreateRect("YTick", plotArea);
                tick.anchorMin = new Vector2(0f, t);
                tick.anchorMax = new Vector2(0f, t);
                tick.pivot = new Vector2(1f, 0.5f);
                tick.sizeDelta = new Vector2(8f, 2f);
                tick.anchoredPosition = new Vector2(-2f, 0f);
                Image img = tick.gameObject.AddComponent<Image>();
                img.color = axisColor;
                img.raycastTarget = false;
            }

            const int xTickCount = 11;
            for (int i = 0; i <= xTickCount; i++) {
                float t = i / (float)xTickCount;
                RectTransform tick = EsportsUIKit.CreateRect("XTick", plotArea);
                tick.anchorMin = new Vector2(t, 0f);
                tick.anchorMax = new Vector2(t, 0f);
                tick.pivot = new Vector2(0.5f, 1f);
                tick.sizeDelta = new Vector2(2f, 8f);
                tick.anchoredPosition = new Vector2(0f, -2f);
                Image img = tick.gameObject.AddComponent<Image>();
                img.color = axisColor;
                img.raycastTarget = false;
            }

            RectTransform xLetterRT = EsportsUIKit.CreateRect("XLetter", plotArea);
            xLetterRT.anchorMin = Vector2.zero;
            xLetterRT.anchorMax = Vector2.zero;
            xLetterRT.pivot = new Vector2(0f, 1f);
            xLetterRT.sizeDelta = new Vector2(30f, 28f);
            xLetterRT.anchoredPosition = new Vector2(-18f, -10f);
            TextMeshProUGUI xLetter = xLetterRT.gameObject.AddComponent<TextMeshProUGUI>();
            xLetter.text = "X";
            xLetter.fontSize = EsportsTheme.TextSize.Small;
            xLetter.color = axisColor;
            xLetter.fontStyle = FontStyles.Bold;
            xLetter.alignment = TextAlignmentOptions.Center;
            xLetter.raycastTarget = false;

            RectTransform yLetterRT = EsportsUIKit.CreateRect("YLetter", plotArea);
            yLetterRT.anchorMin = Vector2.zero;
            yLetterRT.anchorMax = Vector2.zero;
            yLetterRT.pivot = new Vector2(0f, 1f);
            yLetterRT.sizeDelta = new Vector2(30f, 28f);
            yLetterRT.anchoredPosition = new Vector2(-10f, -28f);
            TextMeshProUGUI yLetter = yLetterRT.gameObject.AddComponent<TextMeshProUGUI>();
            yLetter.text = "Y";
            yLetter.fontSize = EsportsTheme.TextSize.Small;
            yLetter.color = axisColor;
            yLetter.fontStyle = FontStyles.Bold;
            yLetter.alignment = TextAlignmentOptions.Center;
            yLetter.raycastTarget = false;
        }

        // -----------------------------------------------------------------------------------------
        // Tarjeta inferior
        // -----------------------------------------------------------------------------------------

        private static void BuildBottomSheet(BuiltRefs refs) {
            Sprite cardFill = LoadSprite("Module2_CardFill");
            Sprite cardFrame = cardFill;
            Color panelColor = cardFill
                ? new Color(0f, 0f, 0f, 0f)
                : new Color(EsportsTheme.BgDeep.r, EsportsTheme.BgDeep.g, EsportsTheme.BgDeep.b, 0.55f);

            RectTransform sheetContent = EsportsUIKit.CreateFramedCard(refs.canvasRoot, "BottomSheet", panelColor, cardFrame,
                BottomSheetPaddingLeft, BottomSheetPaddingRight, BottomSheetPaddingTop, BottomSheetPaddingBottom);
            RectTransform bottomSheetRoot = (RectTransform)sheetContent.parent;
            bottomSheetRoot.anchorMin = new Vector2(0f, 0f);
            bottomSheetRoot.anchorMax = new Vector2(1f, 0f);
            bottomSheetRoot.pivot = new Vector2(0.5f, 0f);
            bottomSheetRoot.sizeDelta = new Vector2(-2f * BottomSheetSideMargin, FuzzyHealerDemo.BottomSheetHeight);
            bottomSheetRoot.anchoredPosition = new Vector2(0f, FuzzyHealerDemo.BottomSheetMargin);
            refs.bottomSheetRoot = bottomSheetRoot;

            float contentWidth = sheetContent.rect.width;

            VerticalLayoutGroup sheetLayout = sheetContent.gameObject.AddComponent<VerticalLayoutGroup>();
            sheetLayout.spacing = BottomSheetSpacing;
            sheetLayout.childAlignment = TextAnchor.UpperCenter;
            sheetLayout.childControlHeight = true;
            sheetLayout.childControlWidth = true;
            sheetLayout.childForceExpandWidth = false;
            sheetLayout.childForceExpandHeight = false;

            TMP_Dropdown functionDropdown = EsportsUIKit.CreateDropdown(sheetContent, FuzzyHealerDemo.CurveShapeLabels, 0, 46f);
            functionDropdown.GetComponent<LayoutElement>().preferredWidth = contentWidth * DropdownWidthFrac;
            refs.functionDropdown = functionDropdown;

            refs.formulaText = BuildFormulaBox(sheetContent, contentWidth * FormulaWidthFrac);
            refs.formulaText.text = "trapezoidal(a, b, c, d)"; // arranca en Trapezoidal (índice 0 del dropdown)

            BuildWeightSliderRow(sheetContent, contentWidth * WeightRowWidthFrac, refs);

            Button evaluarButton = EsportsUIKit.CreateGradientButton(sheetContent, "Evaluar", 64f);
            evaluarButton.GetComponent<LayoutElement>().preferredWidth = contentWidth * EvaluarWidthFrac;
            TextMeshProUGUI evaluarLabel = evaluarButton.GetComponentInChildren<TextMeshProUGUI>();
            if (evaluarLabel) evaluarLabel.fontSizeMax = EsportsTheme.TextSize.Subtitle; // botón angosto: autoajuste con piso legible (ver CreateTextFill)
            refs.evaluarButton = evaluarButton;
        }

        private static void BuildWeightSliderRow(Transform parent, float preferredWidth, BuiltRefs refs) {
            RectTransform container = EsportsUIKit.CreateRect("WeightRow", parent);
            LayoutElement containerLE = container.gameObject.AddComponent<LayoutElement>();
            containerLE.preferredHeight = 160f;
            containerLE.preferredWidth = preferredWidth;
            VerticalLayoutGroup containerLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 10f;
            containerLayout.childControlHeight = true;
            containerLayout.childControlWidth = true;
            containerLayout.childForceExpandWidth = true;

            RectTransform minMaxRow = EsportsUIKit.CreateRect("MinMaxRow", container);
            minMaxRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            RectTransform minLabelRT = EsportsUIKit.CreateRect("MinLabel", minMaxRow);
            minLabelRT.anchorMin = new Vector2(0f, 0.5f);
            minLabelRT.anchorMax = new Vector2(0f, 0.5f);
            minLabelRT.pivot = new Vector2(0f, 0.5f);
            minLabelRT.sizeDelta = new Vector2(52f, 28f);
            TextMeshProUGUI minLabel = minLabelRT.gameObject.AddComponent<TextMeshProUGUI>();
            minLabel.text = "0";
            minLabel.fontSize = EsportsTheme.TextSize.Small;
            minLabel.color = EsportsTheme.TextPrimary;
            minLabel.alignment = TextAlignmentOptions.Left;

            RectTransform maxLabelRT = EsportsUIKit.CreateRect("MaxLabel", minMaxRow);
            maxLabelRT.anchorMin = new Vector2(1f, 0.5f);
            maxLabelRT.anchorMax = new Vector2(1f, 0.5f);
            maxLabelRT.pivot = new Vector2(1f, 0.5f);
            maxLabelRT.sizeDelta = new Vector2(52f, 28f);
            TextMeshProUGUI maxLabel = maxLabelRT.gameObject.AddComponent<TextMeshProUGUI>();
            maxLabel.text = "1";
            maxLabel.fontSize = EsportsTheme.TextSize.Small;
            maxLabel.color = EsportsTheme.TextPrimary;
            maxLabel.alignment = TextAlignmentOptions.Right;

            RectTransform sliderHolder = EsportsUIKit.CreateRect("SliderHolder", container);
            sliderHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            refs.weightSlider = EsportsUIKit.CreateGradientSlider(sliderHolder, 0f, 1f, 0f);

            RectTransform bottomRow = EsportsUIKit.CreateRect("IconValueRow", container);
            bottomRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            HorizontalLayoutGroup bottomLayout = bottomRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.childControlHeight = true;
            bottomLayout.childControlWidth = true;
            bottomLayout.childForceExpandWidth = false;

            refs.reinhardtRing = BuildHeroIcon(bottomRow, "R");

            RectTransform valueHolder = EsportsUIKit.CreateRect("ValueHolder", bottomRow);
            valueHolder.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            refs.weightLabel = EsportsUIKit.CreateTextFill(valueHolder, "0.0000", EsportsTheme.TextSize.Title, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);

            refs.dvaRing = BuildHeroIcon(bottomRow, "D");
        }

        /// <summary>Caja oscura con la fórmula de la función de pertenencia actual — mismo recuadro tipo "código" que el mockup, bajo el selector de Función.</summary>
        private static TextMeshProUGUI BuildFormulaBox(Transform parent, float preferredWidth) {
            RectTransform box = EsportsUIKit.CreateRect("FormulaBox", parent);
            LayoutElement boxLE = box.gameObject.AddComponent<LayoutElement>();
            boxLE.preferredHeight = 52f;
            boxLE.preferredWidth = preferredWidth;
            Image bg = box.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.09f, 1f);

            TextMeshProUGUI text = EsportsUIKit.CreateTextFill(box, "", EsportsTheme.TextSize.Small, EsportsTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center);
            // La caja es angosta a propósito — con fórmulas largas como "trapezoidal(a, b, c, d)" el
            // texto a tamaño fijo se saldría de la caja. Autosize con piso legible (Caption, 12 px reales) deja que se
            // achique solo lo necesario para entrar, en vez de desbordar.
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.NoWrap; // una sola línea: se achica antes que partirse
            text.fontSizeMin = EsportsTheme.TextSize.Caption;
            text.fontSizeMax = EsportsTheme.TextSize.Small;
            return text;
        }

        /// <summary>
        /// PLACEHOLDER: caja con solo contorno + letra, en el mismo lugar donde el mockup muestra un
        /// cuadro con una X ("imagen no encontrada") — reemplazar el Image/Sprite de este GameObject en
        /// el Inspector cambia el ícono en toda la pantalla, no hace falta tocar código.
        /// </summary>
        private static Image[] BuildHeroIcon(Transform parent, string letter) {
            RectTransform icon = EsportsUIKit.CreateRect("Icon", parent);
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
            icon.sizeDelta = new Vector2(48f, 48f);

            // Aro de urgencia: un contorno de 4 barras, no un rectángulo sólido. Arranca invisible
            // (alpha 0) — recién toma color en runtime cuando el sistema difuso calcula una prioridad.
            RectTransform ringRT = EsportsUIKit.CreateRect("Ring", icon);
            EsportsUIKit.Stretch(ringRT, -9f, -9f, -9f, -9f);
            Color ringStart = new(EsportsTheme.NeonGreen.r, EsportsTheme.NeonGreen.g, EsportsTheme.NeonGreen.b, 0f);
            EsportsUIKit.CreateOutline(ringRT, ringStart, 3f, out Image[] ring);

            EsportsUIKit.CreateOutline(icon, EsportsTheme.TextPrimary, 1.5f);

            TextMeshProUGUI label = EsportsUIKit.CreateTextFill(icon, letter, EsportsTheme.TextSize.Subtitle, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
            label.raycastTarget = false;

            return ring;
        }

        // -----------------------------------------------------------------------------------------
        // Popup de resultado
        // -----------------------------------------------------------------------------------------

        private static void BuildResultPopup(BuiltRefs refs) {
            RectTransform backdrop = EsportsUIKit.CreateRect("ResultBackdrop", refs.canvasRoot);
            EsportsUIKit.Stretch(backdrop);
            Image backdropImg = backdrop.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.65f);
            refs.resultBackdropButton = backdrop.gameObject.AddComponent<Button>();
            refs.resultBackdropButton.targetGraphic = backdropImg;
            refs.resultBackdrop = backdrop;

            RectTransform popupContent = EsportsUIKit.CreateAccentCard(backdrop, "ResultPopupCard", EsportsTheme.NeonBlend, out Image resultBorder, 3f, 16f);
            refs.resultBorder = resultBorder;
            RectTransform popupRoot = (RectTransform)popupContent.parent.parent;
            backdrop.gameObject.AddComponent<CanvasGroup>();
            backdrop.gameObject.AddComponent<M2Popup>().SetCard(popupRoot);
            popupRoot.anchorMin = new Vector2(0.5f, 0.5f);
            popupRoot.anchorMax = new Vector2(0.5f, 0.5f);
            popupRoot.pivot = new Vector2(0.5f, 0.5f);
            popupRoot.sizeDelta = new Vector2(600f, 300f);
            popupRoot.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup popupLayout = popupContent.gameObject.AddComponent<VerticalLayoutGroup>();
            popupLayout.spacing = 14f;
            popupLayout.childControlHeight = true;
            popupLayout.childControlWidth = true;
            popupLayout.childForceExpandWidth = true;
            popupLayout.childAlignment = TextAnchor.MiddleCenter;

            refs.resultText = EsportsUIKit.CreateText(popupContent, "Sin evaluar", EsportsTheme.TextSize.Subtitle, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, 120f);
            refs.resultCloseButton = EsportsUIKit.CreateGradientButton(popupContent, "Cerrar", 50f);

            // Arranca oculto — FuzzyHealerDemo.WireUI() también lo fuerza a oculto al entrar a Play,
            // pero dejarlo así de entrada evita que el popup tape la pantalla mientras se edita el HUD.
            backdrop.gameObject.SetActive(false);
        }

        // -----------------------------------------------------------------------------------------
        // Vista previa de las curvas en modo edición (sin Play): mismos valores por defecto que
        // FuzzyHealerDemo.BuildScene() le da a la variable "DañoEntrante" en Trapezoidal — así la
        // gráfica no se ve vacía al abrir la escena. En runtime, FuzzyHealerDemo.RedrawCurves()
        // recalcula con los datos reales y pisa estos puntos.
        // -----------------------------------------------------------------------------------------

        private static void SeedPreviewCurves(BuiltRefs refs) {
            FuzzyVariable dano = new(
                "DañoEntrante", 0f, 100f,
                new FuzzyLabel("Bajo", new MembershipFunction(0f, 0f, 30f, 50f), EsportsTheme.CurveBajo),
                new FuzzyLabel("Alto", new MembershipFunction(30f, 70f, 100f, 100f), EsportsTheme.CurveAlto));

            const int samples = 48;
            Rect rect = refs.graphArea.rect;
            Vector2[] bajo = new Vector2[samples];
            Vector2[] alto = new Vector2[samples];

            for (int i = 0; i < samples; i++) {
                float t = i / (float)(samples - 1);
                float x = Mathf.Lerp(dano.Range.x, dano.Range.y, t);
                bajo[i] = new Vector2(t * rect.width, dano.DegreeOf("Bajo", x) * rect.height * FuzzyHealerDemo.CurveHeadroom);
                alto[i] = new Vector2(t * rect.width, dano.DegreeOf("Alto", x) * rect.height * FuzzyHealerDemo.CurveHeadroom);
            }

            refs.bajoLine.SetPoints(bajo);
            refs.altoLine.SetPoints(alto);
            refs.bajoFill.SetPoints(bajo);
            refs.altoFill.SetPoints(alto);
        }

        // -----------------------------------------------------------------------------------------
        // Volcar las referencias recolectadas en el componente FuzzyHealerDemo
        // -----------------------------------------------------------------------------------------

        private static void AssignReferences(FuzzyHealerDemo demo, BuiltRefs refs) {
            SerializedObject so = new(demo);

            void Set(string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
            void SetArray(string field, Image[] values) {
                SerializedProperty prop = so.FindProperty(field);
                prop.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            }

            Set("canvasRoot", refs.canvasRoot);
            Set("backButton", refs.backButton);
            Set("fullscreenButton", refs.fullscreenButton);
            Set("fullscreenIcon", refs.fullscreenIcon);
            Set("graphCardRoot", refs.graphCardRoot);
            Set("graphArea", refs.graphArea);
            Set("bajoFill", refs.bajoFill);
            Set("altoFill", refs.altoFill);
            Set("bajoLine", refs.bajoLine);
            Set("altoLine", refs.altoLine);
            Set("bottomSheetRoot", refs.bottomSheetRoot);
            Set("functionDropdown", refs.functionDropdown);
            Set("formulaText", refs.formulaText);
            Set("weightSlider", refs.weightSlider);
            Set("weightLabel", refs.weightLabel);
            SetArray("reinhardtRing", refs.reinhardtRing);
            SetArray("dvaRing", refs.dvaRing);
            Set("evaluarButton", refs.evaluarButton);
            Set("resultBackdrop", refs.resultBackdrop);
            Set("resultBackdropButton", refs.resultBackdropButton);
            Set("resultBorder", refs.resultBorder);
            Set("resultText", refs.resultText);
            Set("resultCloseButton", refs.resultCloseButton);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(demo);
        }
    }
}
