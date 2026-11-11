using Neuralizer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Escena de demostración aislada: construye por código un ejercicio ficticio completo
    /// (calculadora de propina difusa) con su gráfica de funciones de pertenencia en vivo, para
    /// verificar rápidamente el look &amp; feel de una interfaz de lógica difusa sin tener que
    /// diseñar prefabs a mano todavía.
    /// </summary>
    /// <remarks>
    /// No forma parte del juego final ni toca <c>MainScene</c> — vive en su propia escena
    /// (<c>FuzzyDemo.unity</c>) pensada solo para revisión. El ejemplo (calidad de servicio →
    /// propina) es intencionalmente ajeno a la temática del proyecto para que no se confunda con
    /// contenido real.
    /// </remarks>
    public class FuzzyTipDemo : MonoBehaviour {

        private static readonly Color BgTop = new(0.75f, 0.93f, 0.87f);
        private static readonly Color BgBottom = new(0.29f, 0.61f, 0.50f);
        private static readonly Color PanelColor = new(0.87f, 0.91f, 0.86f);
        private static readonly Color GraphBg = new(0.15f, 0.18f, 0.17f);
        private static readonly Color MalaColor = new(0.85f, 0.33f, 0.31f);
        private static readonly Color ExcelenteColor = new(0.29f, 0.61f, 0.50f);
        private static readonly Color MarkerColor = Color.white;
        private static readonly Color NeutralResult = new(0.76f, 0.80f, 0.75f);
        private static readonly Color SuccessResult = new(0.31f, 0.78f, 0.47f);

        private const float OutputSuccessThreshold = 15f;

        private FuzzySystem system;
        private RectTransform graphArea;
        private UIPolyline malaLine;
        private UIPolyline excelenteLine;
        private UIPolyline markerLine;
        private TextMeshProUGUI outputValueText;
        private TextMeshProUGUI resultText;
        private Image resultPanel;
        private float currentQuality;

        private void Start() {
            BuildFuzzySystem();
            BuildUI();
            OnQualityChanged(3f);
        }

        /// <summary>
        /// Arma el sistema difuso del ejercicio: una sola entrada (Calidad de servicio) y una
        /// sola salida (Propina), con dos reglas.
        /// </summary>
        private void BuildFuzzySystem() {
            FuzzyVariable calidad = new(
                "Calidad", 0f, 10f,
                new FuzzyLabel("Mala", new MembershipFunction(0f, 0f, 3f, 5f), MalaColor),
                new FuzzyLabel("Excelente", new MembershipFunction(5f, 7f, 10f, 10f), ExcelenteColor));

            FuzzyVariable propina = new(
                "Propina", 0f, 25f,
                new FuzzyLabel("Baja", new MembershipFunction(0f, 0f, 8f, 12f), MalaColor),
                new FuzzyLabel("Alta", new MembershipFunction(13f, 17f, 25f, 25f), ExcelenteColor));

            system = new FuzzySystem {
                inputs = new[] { calidad },
                outputs = new[] { propina },
                rules = new[] {
                    new FuzzyRule {
                        description = "SI Calidad es Mala ENTONCES Propina es Baja",
                        conditions = new[] { new FuzzyCondition { inputIndex = 0, label = "Mala" } },
                        outputIndex = 0,
                        outputLabel = "Baja",
                        weight = 1f
                    },
                    new FuzzyRule {
                        description = "SI Calidad es Excelente ENTONCES Propina es Alta",
                        conditions = new[] { new FuzzyCondition { inputIndex = 0, label = "Excelente" } },
                        outputIndex = 0,
                        outputLabel = "Alta",
                        weight = 1f
                    }
                }
            };
        }

        // ---------------------------------------------------------------
        // Construcción de UI
        // ---------------------------------------------------------------

        private void BuildUI() {
            if (!FindAnyObjectByType<EventSystem>()) {
                GameObject es = new("EventSystem");
                es.AddComponent<EventSystem>();
                // El proyecto usa el Input System nuevo exclusivamente (Project Settings >
                // Active Input Handling), así que el módulo de UI debe ser este, no StandaloneInputModule.
                es.AddComponent<InputSystemUIInputModule>();
            }

            GameObject canvasGO = new("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform root = canvasGO.GetComponent<RectTransform>();

            Image bg = CreateImage("Background", root, BgBottom);
            Stretch(bg.rectTransform);

            RectTransform column = CreateRect("Column", root);
            Stretch(column, 32f, 32f, 32f, 32f);
            VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            CreateText("Title", column, "PRESS START TO LEARN", 30f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, 40f);
            CreateText("Subtitle", column, "Demo — Calculadora de Propina (ejercicio ficticio)", 20f, new Color(0.9f, 0.98f, 0.95f), FontStyles.Normal, TextAlignmentOptions.Center, 30f);
            CreateText("Narrative", column, "Ajusta la calidad del servicio y observa cómo el sistema difuso decide la propina. Objetivo: logra que la propina caiga en la zona ALTA (verde).", 18f, new Color(0.92f, 0.98f, 0.95f), FontStyles.Italic, TextAlignmentOptions.Center, 70f);

            // --- Panel de la gráfica ---
            RectTransform graphPanel = CreatePanelRect("GraphPanel", column, PanelColor, 340f);
            RectTransform graphInner = CreateRect("GraphArea", graphPanel);
            graphInner.anchorMin = new Vector2(0f, 0f);
            graphInner.anchorMax = new Vector2(1f, 1f);
            graphInner.offsetMin = new Vector2(20f, 60f);
            graphInner.offsetMax = new Vector2(-20f, -20f);
            graphInner.pivot = Vector2.zero;
            Image graphBgImage = graphInner.gameObject.AddComponent<Image>();
            graphBgImage.color = GraphBg;
            graphArea = graphInner;

            malaLine = CreatePolyline("MalaLine", graphInner, MalaColor, 5f);
            excelenteLine = CreatePolyline("ExcelenteLine", graphInner, ExcelenteColor, 5f);
            markerLine = CreatePolyline("Marker", graphInner, MarkerColor, 3f);

            CreateText("AxisMin", graphPanel, "0", 16f, Color.black, FontStyles.Normal, TextAlignmentOptions.Left, 24f, new Vector2(0f, 0f), new Vector2(0.3f, 0f), new Vector2(20f, 8f));
            CreateText("AxisMax", graphPanel, "10 (Calidad)", 16f, Color.black, FontStyles.Normal, TextAlignmentOptions.Right, 24f, new Vector2(0.7f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 8f));

            DrawStaticCurves();

            // --- Slider ---
            RectTransform sliderRow = CreateRect("SliderRow", column);
            LayoutElement sliderRowLE = sliderRow.gameObject.AddComponent<LayoutElement>();
            sliderRowLE.preferredHeight = 60f;
            Slider slider = CreateSlider(sliderRow, 0f, 10f, currentQuality);
            slider.onValueChanged.AddListener(OnQualityChanged);

            // --- Resultado ---
            outputValueText = CreateText("OutputValue", column, "Propina: --", 24f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, 36f);

            resultPanel = CreateImage("ResultPanel", column, NeutralResult);
            LayoutElement resultLE = resultPanel.gameObject.AddComponent<LayoutElement>();
            resultLE.preferredHeight = 60f;
            resultText = CreateText("ResultText", resultPanel.rectTransform, "Sin evaluar", 22f, Color.black, FontStyles.Bold, TextAlignmentOptions.Center, 0f);
            Stretch(resultText.rectTransform);

            // --- Botón Evaluar ---
            Button evaluarButton = CreateButton("EvaluarButton", column, "Evaluar", new Color(0.56f, 0.85f, 0.69f));
            evaluarButton.onClick.AddListener(OnEvaluar);
        }

        private void DrawStaticCurves() {
            const int samples = 48;
            Vector2[] mala = new Vector2[samples];
            Vector2[] excelente = new Vector2[samples];
            FuzzyVariable input = system.inputs[0];

            for (int i = 0; i < samples; i++) {
                float t = i / (float)(samples - 1);
                float x = Mathf.Lerp(input.Range.x, input.Range.y, t);
                float px = t; // se escala en LateUpdate/al redimensionar
                mala[i] = new Vector2(px, input.DegreeOf("Mala", x));
                excelente[i] = new Vector2(px, input.DegreeOf("Excelente", x));
            }

            ApplyToRect(malaLine, mala);
            ApplyToRect(excelenteLine, excelente);
        }

        private void ApplyToRect(UIPolyline line, Vector2[] normalizedPoints) {
            Rect rect = graphArea.rect;
            Vector2[] scaled = new Vector2[normalizedPoints.Length];
            for (int i = 0; i < normalizedPoints.Length; i++) {
                scaled[i] = new Vector2(normalizedPoints[i].x * rect.width, normalizedPoints[i].y * rect.height);
            }
            line.SetPoints(scaled);
        }

        private void OnQualityChanged(float value) {
            currentQuality = value;
            float output = system.EvaluateSingle(new[] { value });
            outputValueText.text = $"Propina: {output:0.0}%";

            Rect rect = graphArea.rect;
            float px = Mathf.InverseLerp(system.inputs[0].Range.x, system.inputs[0].Range.y, value) * rect.width;
            markerLine.SetPoints(new[] { new Vector2(px, 0f), new Vector2(px, rect.height) });
        }

        private void OnEvaluar() {
            float output = system.EvaluateSingle(new[] { currentQuality });
            bool success = output >= OutputSuccessThreshold;
            resultPanel.color = success ? SuccessResult : NeutralResult;
            resultText.text = success
                ? "¡Correcto! Propina en zona Alta"
                : $"Aún no — Propina {output:0.0}%, sigue en zona Baja. Sube la Calidad.";
        }

        // ---------------------------------------------------------------
        // Helpers de construcción de UI
        // ---------------------------------------------------------------

        private static RectTransform CreateRect(string name, Transform parent) {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static Image CreateImage(string name, Transform parent, Color color) {
            RectTransform rt = CreateRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static RectTransform CreatePanelRect(string name, Transform parent, Color color, float preferredHeight) {
            Image img = CreateImage(name, parent, color);
            LayoutElement le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            return img.rectTransform;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color,
            FontStyles style, TextAlignmentOptions alignment, float preferredHeight,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? offset = null) {
            RectTransform rt = CreateRect(name, parent);
            if (anchorMin.HasValue) {
                rt.anchorMin = anchorMin.Value;
                rt.anchorMax = anchorMax ?? anchorMin.Value;
                rt.pivot = new Vector2(alignment == TextAlignmentOptions.Right ? 1f : 0f, 0f);
                rt.sizeDelta = new Vector2(200f, preferredHeight > 0 ? preferredHeight : 24f);
                rt.anchoredPosition = offset ?? Vector2.zero;
            }
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            if (preferredHeight > 0f && !anchorMin.HasValue) {
                LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = preferredHeight;
            }
            return tmp;
        }

        private static UIPolyline CreatePolyline(string name, Transform parent, Color color, float thickness) {
            RectTransform rt = CreateRect(name, parent);
            Stretch(rt);
            rt.pivot = Vector2.zero;
            UIPolyline line = rt.gameObject.AddComponent<UIPolyline>();
            line.color = color;
            line.Thickness = thickness;
            return line;
        }

        private static Slider CreateSlider(Transform parent, float min, float max, float value) {
            RectTransform root = CreateRect("Slider", parent);
            root.sizeDelta = new Vector2(0f, 40f);
            Slider slider = root.gameObject.AddComponent<Slider>();

            Image bg = CreateImage("Background", root, new Color(1f, 1f, 1f, 0.85f));
            Stretch(bg.rectTransform, 0f, 0f, 10f, 10f);

            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, 10f, 10f, 14f, 14f);
            Image fill = CreateImage("Fill", fillArea, ExcelenteColor);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.sizeDelta = Vector2.zero;

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, 10f, 10f, 0f, 0f);
            Image handle = CreateImage("Handle", handleArea, new Color(0.2f, 0.5f, 0.4f));
            handle.rectTransform.sizeDelta = new Vector2(28f, 28f);

            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            return slider;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color) {
            Image img = CreateImage(name, parent, color);
            LayoutElement le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 70f;
            Button button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            TextMeshProUGUI text = CreateText("Label", img.rectTransform, label, 24f, Color.black, FontStyles.Bold, TextAlignmentOptions.Center, 0f);
            Stretch(text.rectTransform);
            return button;
        }
    }
}
