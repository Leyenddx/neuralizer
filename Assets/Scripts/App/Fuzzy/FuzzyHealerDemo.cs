using Neuralizer.Data;
using Neuralizer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Demo jugable y aislada del Módulo 2: "El Dilema del Healer" (temática Overwatch, ficticio,
    /// solo para verificar la dirección de UI/UX). Construye una <see cref="FuzzyScene"/> real en
    /// memoria y la corre a través de <see cref="FuzzyHost"/> — el mismo camino que usaría el juego
    /// final.
    /// </summary>
    /// <remarks>
    /// Vive en su propia escena (<c>FuzzyDemo.unity</c>), no toca <c>MainScene</c>.
    ///
    /// A PARTIR DE ESTE REFACTOR la interfaz YA NO se arma por código en tiempo de ejecución: toda la
    /// jerarquía (Canvas, barra superior, gráfica, tarjeta inferior, popup de resultado) vive como
    /// GameObjects reales guardados en la escena, editable a mano desde el Editor de Unity (arrastrar,
    /// redimensionar, cambiar colores/textos/espaciados en el Inspector) sin tocar una línea de código.
    /// Este script ya solo conecta comportamiento (clicks, recálculo de curvas, animación del aro de
    /// urgencia) sobre esos objetos, que llegan acá como referencias serializadas — ver los campos
    /// [SerializeField] más abajo, agrupados por sección con [Header].
    ///
    /// La jerarquía se genera (o se regenera desde cero) con la herramienta de editor
    /// <c>Tools → Neuralizer → Módulo 2 → 3. Escena Ejercicio 1 (Healer)</c>
    /// (ver <c>Assets/Scripts/Editor/Module2HudBuilder.cs</c>), que además deja las referencias de
    /// abajo ya asignadas. Si alguna vez hace falta volver a una base "de fábrica" (por ejemplo después
    /// de desordenar el layout a propósito), esa misma herramienta reconstruye todo con las medidas
    /// medidas directo del mockup.
    ///
    /// Los íconos "R"/"D" junto al slider son marcadores genéricos (un círculo con una letra), NO arte
    /// de Blizzard — reemplazarlos por arte con licencia es cuestión de cambiar el sprite/Image de esos
    /// GameObjects en el Inspector, no de tocar este script. El estilo visual base (colores, fuentes)
    /// vive en <see cref="EsportsTheme"/>/<see cref="EsportsUIKit"/>, usado solo por el builder de
    /// editor — en runtime este script no depende de esa caja de herramientas para nada.
    /// </remarks>
    public class FuzzyHealerDemo : MonoBehaviour {

        /// <summary>Formas de curva que el jugador puede elegir en vivo desde el selector "Función".</summary>
        public enum CurveShape { Trapezoidal, Triangular, Sigmoide, Gaussiana }

        /// <summary>
        /// Público porque <c>Module2HudBuilder</c> (herramienta de editor) lo usa para poblar las
        /// opciones del dropdown "Función" sin tener que duplicar esta lista en dos archivos.
        /// </summary>
        public static readonly string[] CurveShapeLabels = { "Trapezoidal", "Triangular", "Sigmoide", "Gaussiana" };

        // ---------------------------------------------------------------
        // Geometría usada en RUNTIME (ToggleFullscreen/RedrawCurves recalculan posiciones con estos
        // números mientras el juego corre, así que tienen que seguir viviendo en código). El resto de
        // las medidas de layout (márgenes de la barra superior, paddings internos, anchos de cada
        // elemento de la tarjeta, etc.) YA NO están acá — una vez armada la jerarquía esos valores se
        // ajustan directo en el Inspector de cada objeto (RectTransform, LayoutElement,
        // HorizontalLayoutGroup/VerticalLayoutGroup...), no hay que volver a tocar código para eso.
        // Públicas a propósito: Module2HudBuilder las lee para que la construcción inicial y el
        // recálculo en runtime partan siempre de los mismos números medidos del mockup, sin duplicar
        // valores que puedan desincronizarse entre los dos archivos.
        // ---------------------------------------------------------------

        /// <summary>Medido directo del mockup (imagen de 4000px de alto): el marco de la tarjeta inferior llega pegado al borde de abajo de la pantalla, sin margen debajo.</summary>
        public const float BottomSheetHeight = 483f;
        public const float BottomSheetMargin = 0f;
        /// <summary>Margen lateral de la gráfica — también el valor al que vuelve al salir de pantalla completa.</summary>
        public const float GraphSideMargin = 24f;
        /// <summary>Espacio entre el final del eje de la gráfica y el inicio de la tarjeta inferior (pantalla normal, no pantalla completa).</summary>
        public const float GraphBottomGap = 117f;
        /// <summary>Qué tan cerca del techo del cuadrante puede llegar una curva (0.88 = nunca toca el borde de arriba) — ver <see cref="RedrawCurves"/>.</summary>
        public const float CurveHeadroom = 0.88f;

        private FuzzyScene scene;
        private FuzzyHost host;
        private bool isFullscreen;

        // ---------------------------------------------------------------
        // Referencias a la jerarquía (asignadas por Module2HudBuilder al generar/regenerar el HUD, o
        // a mano arrastrando objetos desde la escena al Inspector). Agrupadas por sección de pantalla
        // para que sea fácil ubicar qué tocar — ver GUIA_AJUSTE_TAMANOS.md para más contexto de cada
        // una. NINGUNA de estas controla tamaño/posición por sí misma: eso ya lo hacen los componentes
        // normales de Unity (RectTransform, LayoutElement, etc.) en los objetos referenciados — estas
        // variables solo le dicen al script DÓNDE está cada pieza para poder cambiarle texto/color o
        // escuchar sus eventos (clicks, sliders) en tiempo real.
        // ---------------------------------------------------------------

        [Header("Canvas / raíz")]
        [Tooltip("RectTransform del Canvas principal de esta pantalla.")]
        [SerializeField] private RectTransform canvasRoot;

        [Header("Barra superior")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button fullscreenButton;
        [Tooltip("Ícono del botón de pantalla completa — se tiñe para marcar el estado activo.")]
        [SerializeField] private Image fullscreenIcon;

        [Header("Gráfica")]
        [Tooltip("Contenedor de toda la zona de la gráfica — su offsetMin se recalcula al entrar/salir de pantalla completa.")]
        [SerializeField] private RectTransform graphCardRoot;
        [Tooltip("Área exacta donde se dibujan las curvas (PlotArea) — su tamaño define la escala de las curvas.")]
        [SerializeField] private RectTransform graphArea;
        [SerializeField] private UIFilledCurve bajoFill;
        [SerializeField] private UIFilledCurve altoFill;
        [SerializeField] private UIPolyline bajoLine;
        [SerializeField] private UIPolyline altoLine;

        [Header("Tarjeta inferior")]
        [Tooltip("Se oculta por completo al entrar en pantalla completa.")]
        [SerializeField] private RectTransform bottomSheetRoot;
        [SerializeField] private TMP_Dropdown functionDropdown;
        [SerializeField] private TextMeshProUGUI formulaText;
        [SerializeField] private Slider weightSlider;
        [SerializeField] private TextMeshProUGUI weightLabel;
        [Tooltip("Las 4 barras del aro de urgencia de Reinhardt (arriba/abajo/izq/der) — ver UpdateHeroRing.")]
        [SerializeField] private Image[] reinhardtRing = new Image[4];
        [Tooltip("Las 4 barras del aro de urgencia de D.Va.")]
        [SerializeField] private Image[] dvaRing = new Image[4];
        [SerializeField] private Button evaluarButton;

        [Header("Resultado (popup)")]
        [Tooltip("Fondo oscuro de pantalla completa del popup — se activa/desactiva para mostrar u ocultar el resultado.")]
        [SerializeField] private RectTransform resultBackdrop;
        [Tooltip("Click en cualquier parte del fondo oscuro también cierra el popup (opcional).")]
        [SerializeField] private Button resultBackdropButton;
        [SerializeField] private Image resultBorder;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button resultCloseButton;

        // Aro de urgencia por héroe (ver Module2HudBuilder.BuildHeroIcon/OnRanking/Update): verde→
        // amarillo→rojo según qué tan alta es la prioridad que el propio sistema difuso ya calculó para
        // ESE héroe — mismo lenguaje visual que el contorno de daño crítico de un aliado en Overwatch.
        private float reinhardtUrgency;
        private float dvaUrgency;

        private void Start() {
            if (!ValidateReferences()) return;

            BuildScene();
            WireUI();

            host.OnCasesLoaded += _ => RefreshRanking();
            host.OnRankingChanged += OnRanking;
            host.OnQualityEvaluated += OnQualityEvaluated;

            host.Scene = scene;
        }

        /// <summary>
        /// Chequeo defensivo: si falta alguna referencia (por ejemplo porque nunca se corrió el builder
        /// de editor, o alguien borró un objeto por error) se avisa con claridad en la consola en vez de
        /// tirar NullReferenceException en algún punto al azar más adelante.
        /// </summary>
        private bool ValidateReferences() {
            bool ok = true;
            void Check(Object obj, string name) {
                if (!obj) {
                    Debug.LogError($"[FuzzyHealerDemo] Falta la referencia '{name}'. Ejecuta Tools → Neuralizer → Módulo 2 → " +
                                    "3. Escena Ejercicio 1 (Healer) para regenerar el HUD, o arrastra el " +
                                    "objeto correspondiente a mano en el Inspector.", this);
                    ok = false;
                }
            }

            Check(canvasRoot, nameof(canvasRoot));
            Check(backButton, nameof(backButton));
            Check(fullscreenButton, nameof(fullscreenButton));
            Check(graphCardRoot, nameof(graphCardRoot));
            Check(graphArea, nameof(graphArea));
            Check(bajoFill, nameof(bajoFill));
            Check(altoFill, nameof(altoFill));
            Check(bajoLine, nameof(bajoLine));
            Check(altoLine, nameof(altoLine));
            Check(bottomSheetRoot, nameof(bottomSheetRoot));
            Check(functionDropdown, nameof(functionDropdown));
            Check(formulaText, nameof(formulaText));
            Check(weightSlider, nameof(weightSlider));
            Check(weightLabel, nameof(weightLabel));
            Check(evaluarButton, nameof(evaluarButton));
            Check(resultBackdrop, nameof(resultBackdrop));
            Check(resultBorder, nameof(resultBorder));
            Check(resultText, nameof(resultText));
            Check(resultCloseButton, nameof(resultCloseButton));
            if (reinhardtRing == null || reinhardtRing.Length != 4 || System.Array.Exists(reinhardtRing, r => !r)) {
                Debug.LogError("[FuzzyHealerDemo] Falta 'reinhardtRing' (deben ser las 4 barras del aro de urgencia).", this);
                ok = false;
            }
            if (dvaRing == null || dvaRing.Length != 4 || System.Array.Exists(dvaRing, r => !r)) {
                Debug.LogError("[FuzzyHealerDemo] Falta 'dvaRing' (deben ser las 4 barras del aro de urgencia).", this);
                ok = false;
            }

            return ok;
        }

        // ---------------------------------------------------------------
        // Datos del ejercicio
        // ---------------------------------------------------------------

        private void BuildScene() {
            FuzzyVariable hp = new(
                "HP", 0f, 100f,
                new FuzzyLabel("Bajo", new MembershipFunction(0f, 0f, 30f, 55f), EsportsTheme.CurveBajo),
                new FuzzyLabel("Alto", new MembershipFunction(35f, 70f, 100f, 100f), EsportsTheme.CurveAlto));

            FuzzyVariable dano = new(
                "DañoEntrante", 0f, 100f,
                new FuzzyLabel("Bajo", new MembershipFunction(0f, 0f, 30f, 50f), EsportsTheme.CurveBajo),
                new FuzzyLabel("Alto", new MembershipFunction(30f, 70f, 100f, 100f), EsportsTheme.CurveAlto));

            FuzzyVariable prioridad = new(
                "Prioridad", 0f, 100f,
                new FuzzyLabel("Baja", new MembershipFunction(0f, 0f, 25f, 50f), EsportsTheme.CurveBajo),
                new FuzzyLabel("Alta", MembershipFunction.Triangular(50f, 75f, 100f), new Color(0.9f, 0.75f, 0.2f)),
                new FuzzyLabel("MuyAlta", new MembershipFunction(75f, 90f, 100f, 100f), EsportsTheme.CurveAlto));

            FuzzySystem system = new() {
                inputs = new[] { hp, dano },
                outputs = new[] { prioridad },
                rules = new[] {
                    new FuzzyRule {
                        description = "SI HP es Bajo Y DañoEntrante es Alto ENTONCES Prioridad es MuyAlta",
                        conditions = new[] {
                            new FuzzyCondition { inputIndex = 0, label = "Bajo" },
                            new FuzzyCondition { inputIndex = 1, label = "Alto" }
                        },
                        combineWith = FuzzyOperator.And,
                        outputIndex = 0,
                        outputLabel = "MuyAlta",
                        weight = 0f // el jugador debe subir este peso — así arranca "mal" el ejercicio
                    },
                    new FuzzyRule {
                        description = "SI HP es Bajo Y DañoEntrante es Bajo ENTONCES Prioridad es Alta",
                        conditions = new[] {
                            new FuzzyCondition { inputIndex = 0, label = "Bajo" },
                            new FuzzyCondition { inputIndex = 1, label = "Bajo" }
                        },
                        combineWith = FuzzyOperator.And,
                        outputIndex = 0,
                        outputLabel = "Alta",
                        weight = 1f
                    },
                    new FuzzyRule {
                        description = "SI HP es Alto ENTONCES Prioridad es Baja",
                        conditions = new[] { new FuzzyCondition { inputIndex = 0, label = "Alto" } },
                        combineWith = FuzzyOperator.And,
                        outputIndex = 0,
                        outputLabel = "Baja",
                        weight = 1f
                    }
                }
            };

            scene = ScriptableObject.CreateInstance<FuzzyScene>();
            scene.name = "El Dilema del Healer (Demo)";
            scene.narrativeContext = "Reinhardt aguanta la línea con el escudo roto. A dos pasos, D.Va salió de su mech " +
                                      "y un Zenyatta enemigo la tiene en la mira. Solo puedes curar a uno primero. ¿A quién eliges?";
            scene.system = system;
            scene.cases = new[] {
                new FuzzyCase { label = "Reinhardt", inputValues = new[] { 18f, 20f }, expectedRank = 1 },
                new FuzzyCase { label = "D.Va", inputValues = new[] { 16f, 85f }, expectedRank = 0 }
            };

            GameObject hostGO = new("FuzzyHost (Demo)");
            host = hostGO.AddComponent<FuzzyHost>();
        }

        // ---------------------------------------------------------------
        // Conectar comportamiento sobre la jerarquía ya armada (ver Module2HudBuilder)
        // ---------------------------------------------------------------

        private void WireUI() {
            // "Volver" recarga MainScene por nombre — el mismo mecanismo, a la inversa, que usa
            // Module2ExerciseEntry.Load() para entrar acá. Marca MainMenuReturnState ANTES de cargar:
            // sin esto, MainScene arranca desde cero en vez de caer directo en el menú de ejercicios.
            backButton.onClick.AddListener(() => {
                MainMenuReturnState.SkipIntro = true;
                MainMenuReturnState.ReturnToModule = 2;
                SceneManager.LoadScene("MainScene");
            });

            fullscreenButton.onClick.AddListener(ToggleFullscreen);

            functionDropdown.onValueChanged.AddListener(OnCurveShapeChanged);
            UpdateFormulaText((CurveShape)functionDropdown.value);

            weightSlider.onValueChanged.AddListener(OnWeightChanged);

            // Si el builder le puso M2Popup al fondo, el popup entra/sale animado (modal: 240 ms / 150 ms);
            // si no, se comporta como antes (mostrar/ocultar de golpe).
            Neuralizer.UI.M2Popup popup = resultBackdrop.GetComponent<Neuralizer.UI.M2Popup>();
            void OpenResult() {
                if (popup) popup.Open();
                else resultBackdrop.gameObject.SetActive(true);
            }
            void CloseResult() {
                if (popup) popup.Close();
                else resultBackdrop.gameObject.SetActive(false);
            }

            evaluarButton.onClick.AddListener(() => {
                host.SubmitResult();
                OpenResult();
            });

            if (resultBackdropButton) {
                resultBackdropButton.onClick.AddListener(CloseResult);
            }
            resultCloseButton.onClick.AddListener(CloseResult);
            if (popup) popup.HideImmediate();
            else resultBackdrop.gameObject.SetActive(false);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRoot);
            RedrawCurves();
        }

        // ---------------------------------------------------------------
        // Pantalla completa
        // ---------------------------------------------------------------

        private void ToggleFullscreen() {
            isFullscreen = !isFullscreen;
            bottomSheetRoot.gameObject.SetActive(!isFullscreen);
            float bottomOffset = isFullscreen ? GraphSideMargin : (BottomSheetHeight + BottomSheetMargin + GraphBottomGap);
            graphCardRoot.offsetMin = new Vector2(GraphSideMargin, bottomOffset);
            // Sin un ícono "salir de pantalla completa" aparte en el mockup, se reusa el mismo botón
            // y se marca el estado activo con un tinte de acento en vez de agregar texto.
            if (fullscreenIcon) {
                fullscreenIcon.color = isFullscreen ? EsportsTheme.NeonBlue : Color.white;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRoot);
            RedrawCurves();
        }

        // ---------------------------------------------------------------
        // Gráfica
        // ---------------------------------------------------------------

        private void RedrawCurves() {
            const int samples = 48;
            Rect rect = graphArea.rect;
            Vector2[] bajo = new Vector2[samples];
            Vector2[] alto = new Vector2[samples];
            FuzzyVariable dano = scene.system.inputs[1];

            for (int i = 0; i < samples; i++) {
                float t = i / (float)(samples - 1);
                float x = Mathf.Lerp(dano.Range.x, dano.Range.y, t);
                // * CurveHeadroom deja un margen fijo entre el punto más alto posible (pertenencia = 1.0)
                // y el borde superior del cuadrante — sin esto, cualquier curva trapezoidal/plana queda
                // literalmente pegada al techo.
                bajo[i] = new Vector2(t * rect.width, dano.DegreeOf("Bajo", x) * rect.height * CurveHeadroom);
                alto[i] = new Vector2(t * rect.width, dano.DegreeOf("Alto", x) * rect.height * CurveHeadroom);
            }

            bajoLine.SetPoints(bajo);
            altoLine.SetPoints(alto);
            bajoFill.SetPoints(bajo);
            altoFill.SetPoints(alto);
        }

        private void OnCurveShapeChanged(int index) {
            FuzzyVariable dano = scene.system.inputs[1];
            FuzzyLabel bajoLabel = dano.Labels[0];
            FuzzyLabel altoLabel = dano.Labels[1];

            switch ((CurveShape)index) {
                case CurveShape.Trapezoidal:
                    bajoLabel.membership = MembershipFunction.Trapezoidal(0f, 0f, 30f, 50f);
                    altoLabel.membership = MembershipFunction.Trapezoidal(30f, 70f, 100f, 100f);
                    break;
                case CurveShape.Triangular:
                    bajoLabel.membership = MembershipFunction.Triangular(0f, 0f, 50f);
                    altoLabel.membership = MembershipFunction.Triangular(50f, 100f, 100f);
                    break;
                case CurveShape.Sigmoide:
                    bajoLabel.membership = MembershipFunction.SigmoideInversa(50f, 0.12f);
                    altoLabel.membership = MembershipFunction.Sigmoide(50f, 0.12f);
                    break;
                case CurveShape.Gaussiana:
                    bajoLabel.membership = MembershipFunction.Gaussiana(0f, 45f);
                    altoLabel.membership = MembershipFunction.Gaussiana(100f, 45f);
                    break;
            }

            UpdateFormulaText((CurveShape)index);
            RedrawCurves();
            host.Evaluate();
        }

        /// <summary>Texto de la fórmula matemática correspondiente a cada forma de curva — aproximación en texto plano (TMP no renderiza LaTeX real) de lo que muestra el mockup para Sigmoide.</summary>
        private void UpdateFormulaText(CurveShape shape) {
            if (!formulaText) return;
            formulaText.text = shape switch {
                CurveShape.Trapezoidal => "trapezoidal(a, b, c, d)",
                CurveShape.Triangular => "triangular(a, b, c)",
                CurveShape.Sigmoide => "σ(x) = 1 / (1 + e⁻ˣ)",
                CurveShape.Gaussiana => "g(x) = e^(−(x−c)² / 2σ²)",
                _ => ""
            };
        }

        // ---------------------------------------------------------------
        // Evaluación
        // ---------------------------------------------------------------

        private void RefreshRanking() {
            host.Evaluate();
        }

        private void OnWeightChanged(float value) {
            scene.system.rules[0].weight = value;
            // Mockup: solo el número crudo del slider, grande y centrado (ej. "0.0023") — sin la frase
            // "Peso de la regla...:" de antes, que no está en la referencia.
            weightLabel.text = value.ToString("0.0000");
            host.Evaluate();
        }

        private void OnRanking(int[] order) {
            float pa = host.EvaluateSingle(scene.cases[0].inputValues);
            float pb = host.EvaluateSingle(scene.cases[1].inputValues);

            // Urgencia RELATIVA entre los dos (quién domina sobre el otro), no el valor absoluto de cada
            // uno. Se usa la DIFERENCIA, no la proporción, estirada para que una brecha de ~30 puntos
            // (sobre un rango de Prioridad de 0-100) ya sature a rojo/verde llenos — así diferencias
            // moderadas sí se notan, no solo las extremas.
            float diff = pb - pa; // positivo = D.Va necesita más urgencia que Reinhardt
            float maxRange = scene.system.outputs[0].Range.y;
            float saturationGap = maxRange * 0.3f;
            float t = saturationGap > 0f ? Mathf.Clamp(diff / saturationGap, -1f, 1f) : 0f;
            dvaUrgency = (t + 1f) * 0.5f;
            reinhardtUrgency = 1f - dvaUrgency;
        }

        /// <summary>
        /// Pulso continuo de los aros de urgencia — no hace falta re-evaluar el sistema difuso cada
        /// cuadro, solo animar sobre el último <see cref="reinhardtUrgency"/>/<see cref="dvaUrgency"/>
        /// que calculó OnRanking.
        /// </summary>
        private void Update() {
            UpdateHeroRing(reinhardtRing, reinhardtUrgency);
            UpdateHeroRing(dvaRing, dvaUrgency);
        }

        /// <summary>Verde → amarillo → rojo según urgencia, con un pulso que se acelera e intensifica mientras más crítico — el mismo lenguaje visual que el contorno de daño de Overwatch.</summary>
        private static void UpdateHeroRing(Image[] ring, float urgency) {
            if (ring == null) return;

            Color color;
            if (urgency <= 0.02f) {
                color = new Color(EsportsTheme.NeonGreen.r, EsportsTheme.NeonGreen.g, EsportsTheme.NeonGreen.b, 0f);
            }
            else {
                Color c = urgency < 0.5f
                    ? Color.Lerp(EsportsTheme.NeonGreen, EsportsTheme.CurveIntersection, urgency / 0.5f)
                    : Color.Lerp(EsportsTheme.CurveIntersection, EsportsTheme.NeonRed, (urgency - 0.5f) / 0.5f);

                float pulseSpeed = Mathf.Lerp(1f, 7f, urgency);
                float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(0.18f, 0.9f, urgency) * Mathf.Lerp(0.75f, 1f, pulse);
                color = new Color(c.r, c.g, c.b, alpha);
            }

            foreach (Image bar in ring) {
                if (bar) bar.color = color;
            }
        }

        /// <summary>
        /// Muestra el resultado con las prioridades reales que calculó el sistema y el margen entre
        /// ambos (en puntos de Prioridad), en vez de un porcentaje de "calidad".
        /// </summary>
        /// <remarks>
        /// Antes el veredicto salía de la calidad continua (0.5 + diferencia / rango), pero en este
        /// ejercicio Reinhardt siempre vale ~75 y D.Va como mucho ~90: la calidad máxima alcanzable es
        /// ~65 %, así que "¡Excelente!" (≥ 85 %) era imposible aun resuelto. El margen en puntos es lo que
        /// de verdad explica la decisión ("D.Va va primero por 15 puntos") y se lee sin fórmulas.
        /// <paramref name="quality"/> se sigue recibiendo para respetar la firma del evento.
        /// </remarks>
        private void OnQualityEvaluated(float quality) {
            float pr = host.EvaluateSingle(scene.cases[0].inputValues); // Reinhardt
            float pd = host.EvaluateSingle(scene.cases[1].inputValues); // D.Va
            float margin = pd - pr;

            Color accent;
            string verdict;
            if (margin >= 10f) {
                accent = EsportsTheme.NeonGreen;
                verdict = "¡Excelente! El sistema prioriza claramente a D.Va.";
            }
            else if (margin >= 0.01f) {
                accent = EsportsTheme.NeonGreen;
                verdict = "¡Correcto! D.Va va primero, aunque por poco margen.";
            }
            else if (margin > -0.01f) {
                accent = EsportsTheme.Accent;
                verdict = "Empate: el sistema no sabe a quién curar. Sube el peso de la regla.";
            }
            else {
                accent = EsportsTheme.NeonRed;
                verdict = "El sistema sigue priorizando a Reinhardt. Sube el peso de la regla.";
            }

            resultBorder.color = accent;
            resultText.color = accent;
            resultText.text = $"Prioridad — D.Va {pd:0.0} · Reinhardt {pr:0.0}\n{verdict}";
        }
    }
}
