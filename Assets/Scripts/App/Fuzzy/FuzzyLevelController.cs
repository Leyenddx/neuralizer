using System.Collections.Generic;
using System.Text;
using Neuralizer.Data;
using Neuralizer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Controlador de la escena genérica de niveles del Módulo 2 (<c>FuzzyLevel.unity</c>): toma un
    /// <see cref="FuzzyScene"/> (el que dejó el menú en <see cref="FuzzyLevelSelection.Pending"/>, o
    /// <see cref="fallbackLevel"/> si se le da Play directo a la escena), lo carga en
    /// <see cref="FuzzyHost"/> y arma la UI que ese nivel necesita: una tarjeta por caso, la gráfica de
    /// funciones de pertenencia de cada variable de entrada, la superficie de control opcional y los
    /// controles que el nivel marque como editables (pesos de reglas y/o puntos a/b/c/d de las curvas).
    /// </summary>
    /// <remarks>
    /// <para>
    /// El "esqueleto" fijo de la pantalla (barra superior, tarjetas contenedoras, botón Evaluar, popup
    /// de resultado) lo arma <c>Tools → Neuralizer → Módulo 2 → 6. Escena de niveles</c> como
    /// GameObjects reales en la escena — editable a mano en el Inspector. Lo que varía por nivel
    /// (cuántos casos, variables, reglas, etiquetas) se instancia aquí por código dentro de esos
    /// contenedores, que es justo lo que <see cref="FuzzyHost"/> pide en su documentación.
    /// </para>
    /// <para>
    /// El jugador modifica una COPIA en memoria del nivel (<see cref="Object.Instantiate(Object)"/>),
    /// nunca el asset original: en el Editor, editar el asset en Play Mode lo dejaría modificado en
    /// disco. La copia conserva el nombre del original para que el progreso guardado
    /// (<see cref="ScorableExercise.IsCompleted"/>, que usa el nombre como clave) siga siendo el mismo.
    /// </para>
    /// </remarks>
    public class FuzzyLevelController : MonoBehaviour {

        private const int CurveSamples = 96;
        private const int SurfaceResolution = 40;

        private static readonly string[] PointNames = {
            "a · empieza a subir", "b · llega a 1", "c · empieza a bajar", "d · vuelve a 0"
        };

        private static readonly Color[] CaseColors = {
            EsportsTheme.NeonBlue, EsportsTheme.NeonPurple, EsportsTheme.NeonGreen,
            EsportsTheme.CurveIntersection, EsportsTheme.NeonRed
        };

        [SerializeField] private FuzzyHost host;
        [Tooltip("Nivel a cargar si se da Play directo a esta escena (sin pasar por el menú).")]
        [SerializeField] private FuzzyScene fallbackLevel;

        [Header("Barra superior")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI narrativeText;

        [Header("Casos")]
        [SerializeField] private RectTransform casesContainer;

        [Header("Gráfica")]
        [SerializeField] private RectTransform viewTabsContainer;
        [SerializeField] private RectTransform plotArea;
        [SerializeField] private RawImage surfaceImage;
        [SerializeField] private TextMeshProUGUI plotCaption;

        [Header("Controles")]
        [SerializeField] private TextMeshProUGUI controlsHint;
        [SerializeField] private RectTransform controlsContent;

        [Header("Evaluar")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button evaluateButton;

        [Header("Popup de resultado")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitle;
        [SerializeField] private TextMeshProUGUI resultDetail;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [Tooltip("Opcional: anima la apertura/cierre del popup. Sin él, el popup se muestra/oculta de golpe.")]
        [SerializeField] private M2Popup resultPopup;
        [Tooltip("Opcional: halo que se dispara al resolver el nivel.")]
        [SerializeField] private M2Pulse resultPulse;

        private class CaseCard {
            public Image border;
            public Color accent;
            public TextMeshProUGUI result;
        }

        private class LabelCurve {
            public UIFilledCurve fill;
            public UIPolyline line;
        }

        private class Marker {
            public RectTransform line;
            public TextMeshProUGUI tag;
            public RectTransform dot;
        }

        private FuzzyScene level;
        private readonly List<CaseCard> caseCards = new();
        private readonly List<LabelCurve> curves = new();
        private readonly List<Marker> markers = new();
        private readonly List<Image> viewTabBackgrounds = new();
        private M2TabIndicator viewTabIndicator;
        private readonly List<Vector2> pointBuffer = new(CurveSamples);
        private RectTransform curvesRoot;
        private RectTransform surfaceDotsRoot;
        private Texture2D surfaceTexture;
        private Color32[] surfacePixels;
        private float[] surfaceSample;

        /// <summary>Índice de la vista activa: 0..N-1 = variable de entrada, N = superficie de control.</summary>
        private int currentView;
        /// <summary>Etiqueta seleccionada para editar sus puntos, por variable de entrada.</summary>
        private int[] selectedLabel = System.Array.Empty<int>();
        private Slider[] pointSliders = System.Array.Empty<Slider>();
        private TextMeshProUGUI[] pointLabels = System.Array.Empty<TextMeshProUGUI>();
        private float lastQuality;
        private Vector2 lastPlotSize;
        private float lastCasesWidth;
        private GridLayoutGroup casesGrid;
        private float caseCellHeight = 176f;
        private bool plotDirty;
        private bool surfaceDirty;

        private int InputCount => level ? level.system.inputs.Length : 0;
        private bool HasSurfaceView => level && level.showControlSurface && InputCount >= 2 && level.system.outputs.Length > 0;
        private bool IsSurfaceView => HasSurfaceView && currentView == InputCount;

        // -----------------------------------------------------------------------------------------
        // Ciclo de vida
        // -----------------------------------------------------------------------------------------

        private void Start() {
            backButton.onClick.AddListener(BackToMenu);
            menuButton.onClick.AddListener(BackToMenu);
            retryButton.onClick.AddListener(CloseResult);
            evaluateButton.onClick.AddListener(() => host.SubmitResult());
            if (resultPopup) resultPopup.HideImmediate();
            else resultPanel.SetActive(false);

            FuzzyScene source = FuzzyLevelSelection.Pending ? FuzzyLevelSelection.Pending : fallbackLevel;
            FuzzyLevelSelection.Pending = null;
            if (!source) {
                titleText.text = "SIN NIVEL";
                narrativeText.text = "No hay ningún nivel seleccionado. Vuelve al menú y elige uno, o asigna 'Fallback Level' en el Inspector.";
                evaluateButton.interactable = false;
                return;
            }

            level = Instantiate(source);
            level.name = source.name;
            selectedLabel = new int[InputCount];

            titleText.text = level.DisplayName.ToUpperInvariant();
            narrativeText.text = level.narrativeContext;

            host.OnRankingChanged += _ => RefreshCases();
            host.OnClassificationChanged += _ => RefreshCases();
            host.OnQualityEvaluated += q => lastQuality = q;
            host.OnResultEvaluated += ShowResult;

            BuildCaseCards();
            BuildPlot();
            BuildViewTabs();
            SelectView(0);

            host.Scene = level;
        }

        private void OnDestroy() {
            if (level) Destroy(level);
            if (surfaceTexture) Destroy(surfaceTexture);
        }

        private void LateUpdate() {
            if (!level) return;
            float casesWidth = casesContainer.parent is RectTransform col ? col.rect.width : casesContainer.rect.width;
            if (!Mathf.Approximately(casesWidth, lastCasesWidth)) {
                lastCasesWidth = casesWidth;
                LayoutCaseGrid();
            }
            Vector2 size = plotArea.rect.size;
            if (size != lastPlotSize) {
                lastPlotSize = size;
                plotDirty = true;
            }
            if (plotDirty) {
                plotDirty = false;
                RedrawPlot();
            }
            if (surfaceDirty && IsSurfaceView) {
                surfaceDirty = false;
                RedrawSurface();
            }
        }

        private static void BackToMenu() {
            MainMenuReturnState.SkipIntro = true;
            MainMenuReturnState.ReturnToModule = 2;
            SceneManager.LoadScene("MainScene");
        }

        /// <summary>Llamar después de cualquier cambio del jugador: re-evalúa y marca la gráfica para redibujar.</summary>
        private void OnSystemEdited() {
            host.Evaluate();
            plotDirty = true;
            surfaceDirty = true;
        }

        // -----------------------------------------------------------------------------------------
        // Tarjetas de casos
        // -----------------------------------------------------------------------------------------

        private void BuildCaseCards() {
            FuzzyCase[] cases = level.cases;
            casesGrid = casesContainer.GetComponent<GridLayoutGroup>();
            bool classification = level.mode == FuzzyExerciseMode.Classification;
            for (int i = 0; i < cases.Length; i++) {
                Color accent = CaseColor(i);
                RectTransform content = EsportsUIKit.CreateAccentCard(casesContainer, "Case_" + cases[i].label, accent, out Image border, 2f);
                VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 4f;
                v.childControlWidth = true;
                v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;
                v.childAlignment = TextAnchor.MiddleCenter;

                TextMeshProUGUI title = EsportsUIKit.CreateText(content, cases[i].label, EsportsTheme.TextSize.Subtitle, accent,
                    FontStyles.Bold, TextAlignmentOptions.Center, 0f);
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.overflowMode = TextOverflowModes.Ellipsis;

                // La línea de valores es la única que puede crecer: toma el espacio libre de la celda y
                // se achica (nunca por debajo de 12 px reales) si no cabe, en vez de encimarse.
                TextMeshProUGUI inputs = EsportsUIKit.CreateText(content, DescribeInputs(cases[i]), EsportsTheme.TextSize.Small,
                    EsportsTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, 0f);
                inputs.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
                inputs.enableAutoSizing = true;
                inputs.fontSizeMax = EsportsTheme.TextSize.Small;
                inputs.fontSizeMin = EsportsTheme.TextSize.Caption;

                if (classification) {
                    EsportsUIKit.CreateText(content, "Meta: " + cases[i].expectedLabel, EsportsTheme.TextSize.Small,
                        EsportsTheme.TextPrimary, FontStyles.Italic, TextAlignmentOptions.Center, 0f);
                }
                TextMeshProUGUI result = EsportsUIKit.CreateText(content, "", EsportsTheme.TextSize.Subtitle, EsportsTheme.TextPrimary,
                    FontStyles.Bold, TextAlignmentOptions.Center, 0f);
                result.textWrappingMode = TextWrappingModes.NoWrap;
                EsportsSkin.ApplyFont(result, TypeRole.Mono);
                result.fontStyle = FontStyles.Bold;

                // Entrada escalonada al cargar el nivel (ocurre una vez por nivel → se permite).
                M2Motion.AddEntrance(((RectTransform)content.parent.parent).gameObject, 0.08f + i * M2Ease.Stagger);

                caseCards.Add(new CaseCard { border = border, accent = accent, result = result });
            }
            caseCellHeight = classification ? 200f : 176f;
        }

        /// <summary>Ajusta la grilla de casos al ancho real: hasta 3 columnas, celdas del mismo ancho.</summary>
        private void LayoutCaseGrid() {
            if (!casesGrid || level.cases.Length == 0) return;
            // Ancho disponible = el de la columna padre menos su padding (no el de la grilla misma, que
            // depende de la celda que estamos por calcular).
            float width = casesContainer.rect.width;
            if (casesContainer.parent is RectTransform column) {
                LayoutGroup group = column.GetComponent<LayoutGroup>();
                float padding = group ? group.padding.horizontal : 0f;
                width = column.rect.width - padding;
            }
            if (width <= 0f) return;
            int columns = Mathf.Min(3, level.cases.Length);
            float cellWidth = (width - casesGrid.spacing.x * (columns - 1)) / columns;
            casesGrid.constraintCount = columns;
            casesGrid.cellSize = new Vector2(Mathf.Floor(cellWidth), caseCellHeight);
        }

        private string DescribeInputs(FuzzyCase fuzzyCase) {
            StringBuilder sb = new();
            FuzzyVariable[] inputs = level.system.inputs;
            for (int v = 0; v < inputs.Length && v < fuzzyCase.inputValues.Length; v++) {
                if (v > 0) sb.Append(" · ");
                sb.Append(inputs[v].Name).Append(' ').Append(FormatValue(inputs[v], fuzzyCase.inputValues[v]));
            }
            return sb.ToString();
        }

        private void RefreshCases() {
            if (!level) return;
            FuzzyCase[] cases = level.cases;

            if (level.mode == FuzzyExerciseMode.Classification) {
                FuzzyLabel[] labels = InputCount > 0 ? level.system.inputs[0].Labels : System.Array.Empty<FuzzyLabel>();
                int good = 0;
                for (int i = 0; i < cases.Length; i++) {
                    int got = host.ClassificationOf(i);
                    bool ok = got >= 0 && labels[got].Label == cases[i].expectedLabel;
                    if (ok) good++;
                    caseCards[i].result.text = got >= 0 ? labels[got].Label : "Empate";
                    caseCards[i].result.color = ok ? EsportsTheme.NeonGreen : EsportsTheme.NeonRed;
                }
                statusText.text = $"Bien clasificados: {good}/{cases.Length}";
                return;
            }

            // Ranking: posición actual y salida de cada caso.
            int[] position = new int[cases.Length];
            List<int> order = new(cases.Length);
            for (int i = 0; i < cases.Length; i++) order.Add(i);
            order.Sort((a, b) => host.ScoreOf(b).CompareTo(host.ScoreOf(a)));
            for (int p = 0; p < order.Count; p++) position[order[p]] = p;

            string outputName = level.system.outputs.Length > 0 ? level.system.outputs[0].Name : "Salida";
            StringBuilder sb = new();
            sb.Append("<size=80%><color=#").Append(ColorUtility.ToHtmlStringRGB(EsportsTheme.TextSecondary))
                .Append(">Orden por ").Append(outputName).Append(":</color></size>  ");
            for (int p = 0; p < order.Count; p++) {
                if (p > 0) sb.Append(host.ScoreOf(order[p - 1]) - host.ScoreOf(order[p]) < 0.01f ? "  =  " : "  >  ");
                sb.Append(cases[order[p]].label);
            }
            statusText.text = sb.ToString();

            // Si el primero empata con el segundo, no hay "#1" real: no se destaca a nadie en ámbar
            // (destacar a uno de los empatados daría una pista falsa).
            bool clearLeader = order.Count < 2 || host.ScoreOf(order[0]) - host.ScoreOf(order[1]) >= 0.01f;
            for (int i = 0; i < cases.Length; i++) {
                bool first = position[i] == 0 && clearLeader;
                caseCards[i].result.text = $"#{position[i] + 1} · {host.ScoreOf(i):0.0}";
                // Ámbar = "lo más importante ahora": el caso que el sistema atendería primero. El
                // contorno también cambia para que no dependa solo del color del texto.
                caseCards[i].result.color = first ? EsportsTheme.Accent : EsportsTheme.TextPrimary;
                if (caseCards[i].border) {
                    caseCards[i].border.color = first
                        ? EsportsTheme.WithAlpha(EsportsTheme.Accent, 0.9f)
                        : EsportsTheme.WithAlpha(caseCards[i].accent, 0.45f);
                }
            }
        }

        // -----------------------------------------------------------------------------------------
        // Pestañas de vista (una por variable de entrada + superficie)
        // -----------------------------------------------------------------------------------------

        private void BuildViewTabs() {
            int tabCount = InputCount + (HasSurfaceView ? 1 : 0);
            viewTabsContainer.gameObject.SetActive(tabCount > 1);
            if (tabCount <= 1) return;

            // Indicador deslizante detrás de las pestañas (primer hijo, fuera del layout).
            Image indicator = EsportsUIKit.CreateImage("Indicator", viewTabsContainer, EsportsTheme.WithAlpha(EsportsTheme.NeonBlue, 0.3f));
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
            viewTabIndicator = viewTabsContainer.gameObject.AddComponent<M2TabIndicator>();
            viewTabIndicator.SetIndicator(indicator.rectTransform);

            for (int t = 0; t < tabCount; t++) {
                string label = t < InputCount ? level.system.inputs[t].Name : "Superficie";
                Button tab = EsportsUIKit.CreateGhostButton(viewTabsContainer, label, 190f, EsportsTheme.Metrics.TouchTarget);
                int view = t;
                tab.onClick.AddListener(() => SelectView(view));
                viewTabBackgrounds.Add(tab.GetComponent<Image>());
            }
        }

        private void SelectView(int view) {
            currentView = view;
            for (int t = 0; t < viewTabBackgrounds.Count; t++) {
                // La pestaña activa queda transparente: el resaltado lo da el indicador que se desliza.
                viewTabBackgrounds[t].color = t == view
                    ? new Color(0f, 0f, 0f, 0f)
                    : EsportsTheme.WithAlpha(EsportsTheme.BgPanelLight, 0.7f);
            }
            if (viewTabIndicator && view < viewTabBackgrounds.Count) {
                viewTabIndicator.MoveTo(viewTabBackgrounds[view].rectTransform);
            }

            bool surface = IsSurfaceView;
            curvesRoot.gameObject.SetActive(!surface);
            surfaceImage.gameObject.SetActive(surface);
            surfaceDotsRoot.gameObject.SetActive(surface);

            if (!surface) RebuildCurves();
            RebuildControls();
            plotDirty = true;
            surfaceDirty = true;
        }

        // -----------------------------------------------------------------------------------------
        // Gráfica de funciones de pertenencia
        // -----------------------------------------------------------------------------------------

        private void BuildPlot() {
            curvesRoot = EsportsUIKit.CreateRect("Curves", plotArea);
            EsportsUIKit.Stretch(curvesRoot);
            curvesRoot.pivot = Vector2.zero;

            surfaceDotsRoot = EsportsUIKit.CreateRect("SurfaceDots", plotArea);
            EsportsUIKit.Stretch(surfaceDotsRoot);
            surfaceDotsRoot.pivot = Vector2.zero;

            // Marcadores de casos: una línea vertical con etiqueta (vista de curvas) y un punto (superficie).
            for (int i = 0; i < level.cases.Length; i++) {
                Color c = CaseColor(i);
                Image line = EsportsUIKit.CreateImage("CaseMarker_" + i, plotArea, new Color(c.r, c.g, c.b, 0.9f));
                line.raycastTarget = false;
                RectTransform lineRT = line.rectTransform;
                lineRT.anchorMin = Vector2.zero;
                lineRT.anchorMax = Vector2.zero;
                lineRT.pivot = new Vector2(0.5f, 0f);

                TextMeshProUGUI tag = EsportsUIKit.CreateText(lineRT, level.cases[i].label, EsportsTheme.TextSize.Caption, c,
                    FontStyles.Bold, TextAlignmentOptions.Bottom, 0f);
                tag.textWrappingMode = TextWrappingModes.NoWrap;
                tag.raycastTarget = false;
                RectTransform tagRT = tag.rectTransform;
                tagRT.anchorMin = new Vector2(0.5f, 1f);
                tagRT.anchorMax = new Vector2(0.5f, 1f);
                tagRT.pivot = new Vector2(0.5f, 0f);
                tagRT.sizeDelta = new Vector2(140f, 22f);

                RectTransform dot = EsportsUIKit.CreateGlowDot(surfaceDotsRoot, c, 18f, 14f);
                markers.Add(new Marker { line = lineRT, tag = tag, dot = dot });
            }
        }

        private void RebuildCurves() {
            foreach (LabelCurve c in curves) {
                Destroy(c.fill.gameObject);
                Destroy(c.line.gameObject);
            }
            curves.Clear();

            FuzzyVariable variable = level.system.inputs[currentView];
            foreach (FuzzyLabel label in variable.Labels) {
                Color col = label.displayColor;
                curves.Add(new LabelCurve {
                    fill = EsportsUIKit.CreateFilledCurve(curvesRoot, new Color(col.r, col.g, col.b, 0.22f)),
                    line = EsportsUIKit.CreatePolyline(curvesRoot, col, 3f)
                });
            }
        }

        private void RedrawPlot() {
            if (IsSurfaceView) {
                RedrawSurfaceDots();
                for (int i = 0; i < markers.Count; i++) markers[i].line.gameObject.SetActive(false);
                FuzzyVariable a = level.system.inputs[0];
                FuzzyVariable b = level.system.inputs[1];
                FuzzyVariable o = level.system.outputs[0];
                plotCaption.text = $"X: {a.Name} · Y: {b.Name} · color: {o.Name} " +
                                   $"(<color=#{ColorUtility.ToHtmlStringRGB(SurfaceColor(0f))}>bajo</color> → " +
                                   $"<color=#{ColorUtility.ToHtmlStringRGB(SurfaceColor(1f))}>alto</color>)";
                return;
            }

            Vector2 size = plotArea.rect.size;
            FuzzyVariable variable = level.system.inputs[currentView];
            Vector2 range = variable.Range;
            FuzzyLabel[] labels = variable.Labels;

            for (int l = 0; l < labels.Length && l < curves.Count; l++) {
                pointBuffer.Clear();
                for (int s = 0; s < CurveSamples; s++) {
                    float t = s / (float)(CurveSamples - 1);
                    float x = Mathf.Lerp(range.x, range.y, t);
                    pointBuffer.Add(new Vector2(t * size.x, labels[l].Evaluate(x) * size.y));
                }
                curves[l].fill.SetPoints(pointBuffer);
                curves[l].line.SetPoints(pointBuffer);
            }

            float span = Mathf.Max(0.0001f, range.y - range.x);
            for (int i = 0; i < markers.Count; i++) {
                float[] values = level.cases[i].inputValues;
                bool visible = currentView < values.Length;
                markers[i].line.gameObject.SetActive(visible);
                if (!visible) continue;
                float t = Mathf.Clamp01((values[currentView] - range.x) / span);
                markers[i].line.sizeDelta = new Vector2(2f, size.y);
                markers[i].line.anchoredPosition = new Vector2(t * size.x, 0f);
                // Escalona las etiquetas para que no se encimen cuando dos casos caen cerca.
                markers[i].tag.rectTransform.anchoredPosition = new Vector2(0f, 2f + (i % 2) * 22f);
            }

            StringBuilder sb = new();
            sb.Append(variable.Name).Append("  [").Append(FormatValue(variable, range.x)).Append(" – ")
                .Append(FormatValue(variable, range.y)).Append("]   ");
            foreach (FuzzyLabel label in labels) {
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(label.displayColor)).Append(">■ ")
                    .Append(label.Label).Append("</color>  ");
            }
            plotCaption.text = sb.ToString();
        }

        // -----------------------------------------------------------------------------------------
        // Superficie de control (salida en función de las dos primeras entradas)
        // -----------------------------------------------------------------------------------------

        private void RedrawSurface() {
            if (!surfaceTexture) {
                surfaceTexture = new Texture2D(SurfaceResolution, SurfaceResolution, TextureFormat.RGBA32, false) {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                surfacePixels = new Color32[SurfaceResolution * SurfaceResolution];
                surfaceImage.texture = surfaceTexture;
            }

            FuzzySystem system = level.system;
            int inputCount = system.inputs.Length;
            if (surfaceSample == null || surfaceSample.Length != inputCount) surfaceSample = new float[inputCount];
            // Las entradas que no son X/Y se fijan en el valor del primer caso (o el centro del rango).
            for (int v = 0; v < inputCount; v++) {
                Vector2 r = system.inputs[v].Range;
                surfaceSample[v] = level.cases.Length > 0 && v < level.cases[0].inputValues.Length
                    ? level.cases[0].inputValues[v]
                    : (r.x + r.y) * 0.5f;
            }

            Vector2 rangeA = system.inputs[0].Range;
            Vector2 rangeB = system.inputs[1].Range;
            Vector2 outRange = system.outputs[0].Range;
            float outSpan = Mathf.Max(0.0001f, outRange.y - outRange.x);

            for (int j = 0; j < SurfaceResolution; j++) {
                surfaceSample[1] = Mathf.Lerp(rangeB.x, rangeB.y, j / (float)(SurfaceResolution - 1));
                for (int i = 0; i < SurfaceResolution; i++) {
                    surfaceSample[0] = Mathf.Lerp(rangeA.x, rangeA.y, i / (float)(SurfaceResolution - 1));
                    float value = (system.EvaluateSingle(surfaceSample) - outRange.x) / outSpan;
                    surfacePixels[j * SurfaceResolution + i] = SurfaceColor(value);
                }
            }
            surfaceTexture.SetPixels32(surfacePixels);
            surfaceTexture.Apply(false);
        }

        private void RedrawSurfaceDots() {
            Vector2 size = plotArea.rect.size;
            Vector2 rangeA = level.system.inputs[0].Range;
            Vector2 rangeB = level.system.inputs[1].Range;
            for (int i = 0; i < markers.Count; i++) {
                float[] v = level.cases[i].inputValues;
                if (v.Length < 2) continue;
                float tx = Mathf.InverseLerp(rangeA.x, rangeA.y, v[0]);
                float ty = Mathf.InverseLerp(rangeB.x, rangeB.y, v[1]);
                markers[i].dot.anchoredPosition = new Vector2(tx * size.x, ty * size.y);
            }
        }

        private static Color SurfaceColor(float t) {
            t = Mathf.Clamp01(t);
            Color low = Color.Lerp(EsportsTheme.BgDeep, EsportsTheme.NeonBlue, 0.55f);
            return t < 0.5f
                ? Color.Lerp(low, EsportsTheme.NeonPurple, t * 2f)
                : Color.Lerp(EsportsTheme.NeonPurple, EsportsTheme.NeonRed, (t - 0.5f) * 2f);
        }

        // -----------------------------------------------------------------------------------------
        // Controles editables
        // -----------------------------------------------------------------------------------------

        private void RebuildControls() {
            for (int c = controlsContent.childCount - 1; c >= 0; c--) {
                Destroy(controlsContent.GetChild(c).gameObject);
            }
            pointSliders = System.Array.Empty<Slider>();
            pointLabels = System.Array.Empty<TextMeshProUGUI>();

            bool any = false;
            if (level.editableMemberships && InputCount > 0) {
                any = true;
                BuildMembershipControls();
            }
            if (level.editableRuleWeights && level.system.rules.Length > 0) {
                any = true;
                BuildRuleWeightControls();
            }

            controlsHint.text = any
                ? HintFor(level)
                : "Este nivel no tiene controles editables.";
        }

        private static string HintFor(FuzzyScene scene) {
            if (scene.mode == FuzzyExerciseMode.Classification) {
                return "Mueve los puntos de cada curva hasta que cada caso caiga en la etiqueta que le corresponde.";
            }
            if (scene.editableMemberships && scene.editableRuleWeights) {
                return "Ajusta las curvas y el peso de las reglas hasta que el orden de los casos sea el correcto.";
            }
            return scene.editableMemberships
                ? "Ajusta las curvas hasta que el orden de los casos sea el correcto."
                : "Ajusta el peso de cada regla hasta que el orden de los casos sea el correcto.";
        }

        private void BuildRuleWeightControls() {
            SectionTitle("PESO DE LAS REGLAS");
            FuzzyRule[] rules = level.system.rules;
            for (int r = 0; r < rules.Length; r++) {
                FuzzyRule rule = rules[r];
                RectTransform row = Row();
                EsportsUIKit.CreateText(row, $"{r + 1}. {DescribeRule(rule)}", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary,
                    FontStyles.Normal, TextAlignmentOptions.Left, 0f);
                TextMeshProUGUI value = EsportsUIKit.CreateText(row, "", EsportsTheme.TextSize.Small, EsportsTheme.TextSecondary,
                    FontStyles.Bold, TextAlignmentOptions.Right, 0f);
                Slider slider = SliderIn(row, 0f, 1f, rule.weight);
                value.text = $"peso {rule.weight:0.00}";
                slider.onValueChanged.AddListener(w => {
                    rule.weight = w;
                    value.text = $"peso {w:0.00}";
                    OnSystemEdited();
                });
            }
        }

        private void BuildMembershipControls() {
            if (IsSurfaceView) {
                SectionTitle("CURVAS");
                EsportsUIKit.CreateText(controlsContent, "Elige una variable en las pestañas de la gráfica para editar sus curvas.",
                    EsportsTheme.TextSize.Body, EsportsTheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Left, 0f);
                return;
            }

            FuzzyVariable variable = level.system.inputs[currentView];
            FuzzyLabel[] labels = variable.Labels;
            if (labels.Length == 0) return;
            int sel = Mathf.Clamp(selectedLabel[currentView], 0, labels.Length - 1);
            selectedLabel[currentView] = sel;

            SectionTitle($"CURVAS DE {variable.Name.ToUpperInvariant()}");

            RectTransform tabs = EsportsUIKit.CreateRect("LabelTabs", controlsContent);
            tabs.gameObject.AddComponent<LayoutElement>().preferredHeight = EsportsTheme.Metrics.TouchTarget;
            HorizontalLayoutGroup h = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            for (int l = 0; l < labels.Length; l++) {
                Button b = EsportsUIKit.CreateGhostButton(tabs, labels[l].Label, 120f, EsportsTheme.Metrics.TouchTarget);
                Color col = labels[l].displayColor;
                b.GetComponent<Image>().color = l == sel ? new Color(col.r, col.g, col.b, 0.5f) : EsportsTheme.BgPanelLight;
                int index = l;
                b.onClick.AddListener(() => {
                    selectedLabel[currentView] = index;
                    RebuildControls();
                });
            }

            FuzzyLabel label = labels[sel];
            if (label.membership.shape != MembershipShape.Trapezoidal) {
                EsportsUIKit.CreateText(controlsContent, "Esta etiqueta usa una curva que no se edita con puntos a/b/c/d.",
                    EsportsTheme.TextSize.Body, EsportsTheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Left, 0f);
                return;
            }

            pointSliders = new Slider[4];
            pointLabels = new TextMeshProUGUI[4];
            Vector2 range = variable.Range;
            bool wholeNumbers = Mathf.Abs(range.y - range.x) >= 100f;
            for (int p = 0; p < 4; p++) {
                RectTransform row = Row();
                pointLabels[p] = EsportsUIKit.CreateText(row, "", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary,
                    FontStyles.Normal, TextAlignmentOptions.Left, 0f);
                pointSliders[p] = SliderIn(row, range.x, range.y, GetPoint(label.membership, p));
                pointSliders[p].wholeNumbers = wholeNumbers;
                int point = p;
                pointSliders[p].onValueChanged.AddListener(v => OnPointChanged(variable, label, point, v));
            }
            RefreshPointLabels(variable, label);
        }

        private void OnPointChanged(FuzzyVariable variable, FuzzyLabel label, int point, float value) {
            MembershipFunction m = label.membership;
            // Mantiene a <= b <= c <= d: cada punto queda acotado por sus vecinos.
            float min = point > 0 ? GetPoint(m, point - 1) : variable.Range.x;
            float max = point < 3 ? GetPoint(m, point + 1) : variable.Range.y;
            value = Mathf.Clamp(value, min, max);
            SetPoint(ref m, point, value);
            label.membership = m;
            pointSliders[point].SetValueWithoutNotify(value);
            RefreshPointLabels(variable, label);
            OnSystemEdited();
        }

        private void RefreshPointLabels(FuzzyVariable variable, FuzzyLabel label) {
            for (int p = 0; p < pointLabels.Length; p++) {
                pointLabels[p].text = $"{PointNames[p]} = {FormatValue(variable, GetPoint(label.membership, p))}";
            }
        }

        private static float GetPoint(MembershipFunction m, int p) => p switch {
            0 => m.a, 1 => m.b, 2 => m.c, _ => m.d
        };

        private static void SetPoint(ref MembershipFunction m, int p, float v) {
            switch (p) {
                case 0: m.a = v; break;
                case 1: m.b = v; break;
                case 2: m.c = v; break;
                default: m.d = v; break;
            }
        }

        private string DescribeRule(FuzzyRule rule) {
            StringBuilder sb = new("SI ");
            FuzzyVariable[] inputs = level.system.inputs;
            for (int c = 0; c < rule.conditions.Length; c++) {
                if (c > 0) sb.Append(rule.combineWith == FuzzyOperator.And ? " Y " : " O ");
                FuzzyCondition cond = rule.conditions[c];
                string varName = cond.inputIndex >= 0 && cond.inputIndex < inputs.Length ? inputs[cond.inputIndex].Name : "?";
                sb.Append(varName).Append(" es <b>").Append(cond.label).Append("</b>");
            }
            FuzzyVariable[] outputs = level.system.outputs;
            string outName = rule.outputIndex >= 0 && rule.outputIndex < outputs.Length ? outputs[rule.outputIndex].Name : "?";
            sb.Append(" → ").Append(outName).Append(" es <b>").Append(rule.outputLabel).Append("</b>");
            return sb.ToString();
        }

        // -----------------------------------------------------------------------------------------
        // Resultado
        // -----------------------------------------------------------------------------------------

        private void CloseResult() {
            if (resultPopup) resultPopup.Close();
            else resultPanel.SetActive(false);
        }

        private void ShowResult(bool correct) {
            if (resultPopup) resultPopup.Open();
            else resultPanel.SetActive(true);
            int quality = Mathf.RoundToInt(lastQuality * 100f);
            if (correct) {
                resultTitle.text = "¡Nivel resuelto!";
                resultTitle.color = EsportsTheme.NeonGreen;
                if (resultPulse) resultPulse.Play(EsportsTheme.NeonGreen);
                resultDetail.text = $"Calidad del resultado: {quality}%\nTiempo: {ScorableExercise.FormatTime(Time.timeSinceLevelLoad)}";
            }
            else {
                resultTitle.text = "Todavía no";
                resultTitle.color = EsportsTheme.NeonRed;
                string hint = level.mode == FuzzyExerciseMode.Classification
                    ? "Revisa los casos en rojo: mueve las curvas para que cada uno caiga en su etiqueta."
                    : "Piensa qué caso debería ir primero y qué regla o curva lo empuja hacia arriba. Los empates no cuentan.";
                resultDetail.text = $"Calidad del resultado: {quality}%\n{hint}";
            }
        }

        // -----------------------------------------------------------------------------------------
        // Utilidades de UI
        // -----------------------------------------------------------------------------------------

        private void SectionTitle(string text) {
            EsportsUIKit.CreateText(controlsContent, text, EsportsTheme.TextSize.Subtitle, EsportsTheme.NeonBlue, FontStyles.Bold,
                TextAlignmentOptions.Left, 0f);
        }

        /// <summary>Fila de control sin alto fijo: mide lo que pidan sus textos + slider, así nunca se encima.</summary>
        private RectTransform Row() {
            RectTransform row = EsportsUIKit.CreateRect("Row", controlsContent);
            VerticalLayoutGroup v = row.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return row;
        }

        private static Slider SliderIn(RectTransform row, float min, float max, float value) {
            RectTransform holder = EsportsUIKit.CreateRect("SliderHolder", row);
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = EsportsTheme.Metrics.SliderHeight;
            return EsportsUIKit.CreateGradientSlider(holder, min, max, value);
        }

        private static Color CaseColor(int index) => CaseColors[index % CaseColors.Length];

        private static string FormatValue(FuzzyVariable variable, float value) {
            float span = Mathf.Abs(variable.Range.y - variable.Range.x);
            return span >= 100f ? Mathf.RoundToInt(value).ToString() : value.ToString("0.#");
        }
    }
}
