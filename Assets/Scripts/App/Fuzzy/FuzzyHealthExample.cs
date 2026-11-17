using System.Collections.Generic;
using System.Text;
using Neuralizer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Tema 1 del Módulo 2 — EJEMPLO "No todo es verdad o mentira": el estado de salud de un aliado
    /// visto como lo ve un healer de Overwatch (el contorno del personaje cambia de color según su
    /// salud). Introduce los tres conceptos del tema: conjunto difuso, variable lingüística y función
    /// de pertenencia.
    /// </summary>
    /// <remarks>
    /// <para>
    /// La variable lingüística es <b>Salud</b> (0–100) con tres conjuntos difusos: <b>Critical</b>,
    /// <b>Caution</b> y <b>Fine</b>. El usuario mueve la salud con un slider y ve, a la vez: el
    /// contorno del personaje, el grado de pertenencia μ a cada conjunto, y dónde cae la salud sobre
    /// las curvas.
    /// </para>
    /// <para>
    /// Dos selectores: <b>Lógica</b> (clásica: cada salud pertenece a un solo estado y el contorno
    /// salta de color en los umbrales; difusa: pertenece a varios en distinto grado y el color se
    /// mezcla) y <b>Forma</b> de las funciones de pertenencia (triangular o trapezoidal), para ver que
    /// el mismo concepto se puede modelar de más de una manera.
    /// </para>
    /// <para>
    /// La jerarquía la arma <c>Tools → Neuralizer → Módulo 2 → 3. Escena Tema 1 · Ejemplo</c>
    /// (<c>Editor/Module2Tema1ExampleBuilder.cs</c>); este script solo conecta comportamiento. No hay
    /// nada que calificar: es una demostración guiada.
    /// </para>
    /// </remarks>
    public class FuzzyHealthExample : MonoBehaviour {

        private const int CurveSamples = 96;
        /// <summary>Umbrales de la lógica clásica: salud ≤ 30 es Critical, ≤ 70 es Caution, el resto Fine.</summary>
        private const float ClassicLow = 30f;
        private const float ClassicHigh = 70f;

        private static readonly string[] StateNames = { "Critical", "Caution", "Fine" };

        /// <summary>Triangulares (a, pico, d): suman 1 en todo el rango.</summary>
        private static readonly MembershipFunction[] Triangular = {
            MembershipFunction.Triangular(0f, 0f, 50f),
            MembershipFunction.Triangular(0f, 50f, 100f),
            MembershipFunction.Triangular(50f, 100f, 100f),
        };

        /// <summary>Trapezoidales (a, b, c, d): mesetas de pertenencia total y transiciones más cortas.</summary>
        private static readonly MembershipFunction[] Trapezoidal = {
            MembershipFunction.Trapezoidal(0f, 0f, 20f, 40f),
            MembershipFunction.Trapezoidal(20f, 40f, 60f, 80f),
            MembershipFunction.Trapezoidal(60f, 80f, 100f, 100f),
        };

        [Header("Barra superior")]
        [SerializeField] private Button backButton;

        [Header("Personaje")]
        [SerializeField] private Image[] heroRing = new Image[4];
        [SerializeField] private Image[] glowLayers = new Image[4];

        [Header("Lectura de estados (mismo orden: Critical, Caution, Fine)")]
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI[] degreeLabels = new TextMeshProUGUI[3];
        [SerializeField] private Image[] degreeBars = new Image[3];

        [Header("Selectores")]
        [SerializeField] private Button[] logicButtons = new Button[2];
        [SerializeField] private M2TabIndicator logicIndicator;
        [SerializeField] private Button[] shapeButtons = new Button[2];
        [SerializeField] private M2TabIndicator shapeIndicator;
        [SerializeField] private CanvasGroup shapeGroup;

        [Header("Gráfica")]
        [SerializeField] private RectTransform plotArea;
        [SerializeField] private TextMeshProUGUI plotCaption;

        [Header("Control")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthLabel;
        [SerializeField] private TextMeshProUGUI explanationText;

        private readonly UIFilledCurve[] fills = new UIFilledCurve[3];
        private readonly UIPolyline[] lines = new UIPolyline[3];
        private readonly List<Vector2> points = new(CurveSamples);
        private readonly float[] degrees = new float[3];
        private float[] glowBaseAlpha = System.Array.Empty<float>();
        private RectTransform marker;
        private bool classic;
        private bool trapezoidal;
        private Vector2 lastPlotSize;
        private Color ringColor = Color.white;
        private float urgency;

        private static Color StateColor(int i) => i switch {
            0 => EsportsTheme.NeonRed,
            1 => EsportsTheme.Accent,
            _ => EsportsTheme.NeonGreen
        };

        private MembershipFunction[] Functions => trapezoidal ? Trapezoidal : Triangular;

        private void Start() {
            backButton.onClick.AddListener(() => {
                MainMenuReturnState.SkipIntro = true;
                MainMenuReturnState.ReturnToModule = 2;
                SceneManager.LoadScene("MainScene");
            });

            glowBaseAlpha = new float[glowLayers.Length];
            for (int i = 0; i < glowLayers.Length; i++) glowBaseAlpha[i] = glowLayers[i] ? glowLayers[i].color.a : 1f;

            BuildPlot();

            logicButtons[0].onClick.AddListener(() => SetLogic(false));
            logicButtons[1].onClick.AddListener(() => SetLogic(true));
            shapeButtons[0].onClick.AddListener(() => SetShape(false));
            shapeButtons[1].onClick.AddListener(() => SetShape(true));

            healthSlider.minValue = 0f;
            healthSlider.maxValue = 100f;
            healthSlider.wholeNumbers = true;
            healthSlider.onValueChanged.AddListener(_ => Refresh());
            healthSlider.SetValueWithoutNotify(30f);

            SetLogic(false);
            SetShape(false);
        }

        private void BuildPlot() {
            for (int i = 0; i < 3; i++) {
                Color c = StateColor(i);
                fills[i] = EsportsUIKit.CreateFilledCurve(plotArea, EsportsTheme.WithAlpha(c, 0.2f));
                lines[i] = EsportsUIKit.CreatePolyline(plotArea, c, 3f);
            }
            Image line = EsportsUIKit.CreateImage("HealthMarker", plotArea, EsportsTheme.TextPrimary);
            line.raycastTarget = false;
            marker = line.rectTransform;
            marker.anchorMin = marker.anchorMax = Vector2.zero;
            marker.pivot = new Vector2(0.5f, 0f);
        }

        private void SetLogic(bool useClassic) {
            classic = useClassic;
            HighlightTabs(logicButtons, logicIndicator, classic ? 1 : 0);
            // En lógica clásica no hay curvas: la forma no aplica.
            if (shapeGroup) {
                shapeGroup.alpha = classic ? 0.35f : 1f;
                shapeGroup.interactable = !classic;
                shapeGroup.blocksRaycasts = !classic;
            }
            RedrawCurves();
            Refresh();
        }

        private void SetShape(bool useTrapezoidal) {
            trapezoidal = useTrapezoidal;
            HighlightTabs(shapeButtons, shapeIndicator, trapezoidal ? 1 : 0);
            RedrawCurves();
            Refresh();
        }

        private static void HighlightTabs(Button[] buttons, M2TabIndicator indicator, int selected) {
            for (int i = 0; i < buttons.Length; i++) {
                Image bg = buttons[i].GetComponent<Image>();
                if (indicator) {
                    bg.color = i == selected ? new Color(0f, 0f, 0f, 0f) : EsportsTheme.WithAlpha(EsportsTheme.BgPanelLight, 0.7f);
                }
                else {
                    bg.color = i == selected ? EsportsTheme.WithAlpha(EsportsTheme.NeonBlue, 0.45f) : EsportsTheme.BgPanelLight;
                }
            }
            if (indicator) indicator.MoveTo((RectTransform)buttons[selected].transform);
        }

        /// <summary>Grado de pertenencia de <paramref name="health"/> al estado <paramref name="state"/> con la lógica activa.</summary>
        private float Degree(int state, float health) {
            if (!classic) return Functions[state].Evaluate(health);
            int crisp = health <= ClassicLow ? 0 : health <= ClassicHigh ? 1 : 2;
            return crisp == state ? 1f : 0f;
        }

        private void LateUpdate() {
            Vector2 size = plotArea.rect.size;
            if (size != lastPlotSize) {
                lastPlotSize = size;
                RedrawCurves();
                Refresh();
            }
            ApplyRing();
        }

        private void RedrawCurves() {
            Vector2 size = plotArea.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;

            for (int s = 0; s < 3; s++) {
                points.Clear();
                if (classic) {
                    // Escalones: 1 dentro del intervalo del estado, 0 fuera (bordes verticales).
                    float from = s == 0 ? 0f : s == 1 ? ClassicLow : ClassicHigh;
                    float to = s == 0 ? ClassicLow : s == 1 ? ClassicHigh : 100f;
                    points.Add(new Vector2(from / 100f * size.x, 0f));
                    points.Add(new Vector2(from / 100f * size.x, size.y));
                    points.Add(new Vector2(to / 100f * size.x, size.y));
                    points.Add(new Vector2(to / 100f * size.x, 0f));
                }
                else {
                    for (int i = 0; i < CurveSamples; i++) {
                        float t = i / (float)(CurveSamples - 1);
                        points.Add(new Vector2(t * size.x, Functions[s].Evaluate(t * 100f) * size.y));
                    }
                }
                fills[s].SetPoints(points);
                lines[s].SetPoints(points);
            }

            if (plotCaption) {
                plotCaption.text = classic
                    ? $"Lógica clásica: cortes fijos en {ClassicLow:0} y {ClassicHigh:0}"
                    : trapezoidal ? "Funciones trapezoidales (a, b, c, d)" : "Funciones triangulares (a, pico, d)";
            }
        }

        private void Refresh() {
            float health = healthSlider.value;
            Vector2 size = plotArea.rect.size;
            marker.sizeDelta = new Vector2(3f, size.y);
            marker.anchoredPosition = new Vector2(health / 100f * size.x, 0f);

            float total = 0f;
            int dominant = 0;
            for (int s = 0; s < 3; s++) {
                degrees[s] = Degree(s, health);
                total += degrees[s];
                if (degrees[s] > degrees[dominant]) dominant = s;
            }

            // Color del contorno: mezcla de los tres colores ponderada por su grado (en clásica, uno solo).
            Color mix = Color.black;
            for (int s = 0; s < 3; s++) mix += StateColor(s) * (total > 0f ? degrees[s] / total : 0f);
            mix.a = 1f;
            ringColor = mix;
            urgency = Mathf.Clamp01(degrees[0] + 0.45f * degrees[1]);

            for (int s = 0; s < 3; s++) {
                if (degreeLabels[s]) degreeLabels[s].text = $"μ {StateNames[s]} = {degrees[s]:0.00}";
                if (degreeBars[s]) degreeBars[s].fillAmount = degrees[s];
            }

            if (healthLabel) healthLabel.text = $"Salud del aliado: {health:0}";
            if (stateText) {
                stateText.text = StateNames[dominant];
                stateText.color = StateColor(dominant);
            }
            if (explanationText) explanationText.text = Explain(health, dominant);
        }

        private string Explain(float health, int dominant) {
            if (classic) {
                return $"Lógica clásica: con {health:0} de salud el aliado es <b>{StateNames[dominant]}</b> y nada más. " +
                       $"Prueba cruzar el {ClassicLow:0} o el {ClassicHigh:0}: un solo punto cambia todo el contorno.";
            }

            StringBuilder sb = new();
            int count = 0;
            for (int s = 0; s < 3; s++) {
                if (degrees[s] < 0.005f) continue;
                if (count > 0) sb.Append(" y ");
                sb.Append("<b>").Append(StateNames[s]).Append("</b> en ").Append(degrees[s].ToString("0.00"));
                count++;
            }
            return count > 1
                ? $"Lógica difusa: con {health:0} de salud el aliado es {sb} al mismo tiempo. El contorno mezcla esos colores."
                : $"Lógica difusa: con {health:0} de salud el aliado es {sb}. Muévelo hacia una zona donde dos curvas se cruzan.";
        }

        /// <summary>Contorno y resplandor con un pulso que se acelera mientras más crítico (como en el juego).</summary>
        private void ApplyRing() {
            float pulseSpeed = Mathf.Lerp(0.8f, 6f, urgency);
            float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.85f, 1f, pulse);
            foreach (Image bar in heroRing) {
                if (bar) bar.color = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
            }
            for (int i = 0; i < glowLayers.Length; i++) {
                if (!glowLayers[i]) continue;
                glowLayers[i].color = new Color(ringColor.r, ringColor.g, ringColor.b,
                    glowBaseAlpha[i] * Mathf.Lerp(0.55f, 1f, urgency) * Mathf.Lerp(0.8f, 1f, pulse));
            }
        }
    }
}
