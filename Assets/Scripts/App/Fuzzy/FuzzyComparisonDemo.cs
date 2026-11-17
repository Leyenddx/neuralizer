using Neuralizer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Ejercicio 3 del Módulo 2 ("¿A quién curás primero?"): dos personajes en pantalla a la vez
    /// (Reinhardt y Reaper, cada uno con su propio grado de daño al azar), el usuario toca el que
    /// considera en peor estado SOLO por el color/resplandor del contorno — igual que <see
    /// cref="FuzzyDamageDemo"/> pero ahora en modo "triage": comparar y decidir bajo presión, no solo
    /// observar. Después de elegir se revela el acierto/error y el valor real de ambos, para cerrar el
    /// loop pedagógico sin arruinar la lectura intuitiva mientras decide.
    /// </summary>
    /// <remarks>
    /// Vive en su propia escena (<c>FuzzyComparisonDemo.unity</c>). La jerarquía la arma
    /// <c>Tools → Neuralizer → Módulo 2 → 5. Escena Ejercicio 3 (Triage)</c> (ver
    /// <c>Assets/Scripts/Editor/Module2ComparisonDemoBuilder.cs</c>), que deja las referencias de abajo
    /// ya conectadas. Mismos parámetros de μ_Crítico que <see cref="FuzzyDamageDemo"/> — ver ese script
    /// para la discusión de diseño de la curva.
    /// </remarks>
    public class FuzzyComparisonDemo : MonoBehaviour {

        private static readonly MembershipFunction Critico = MembershipFunction.Trapezoidal(50f, 70f, 100f, 100f);

        /// <summary>
        /// Diferencia mínima de GRADO de pertenencia (μ_Crítico, lo que realmente se ve en el contorno)
        /// entre los dos personajes; si es menor, se vuelve a sortear.
        /// </summary>
        /// <remarks>
        /// Antes se exigía separación en el daño crudo (18 puntos), pero dos daños bajo 50 (μ = 0) o
        /// sobre 70 (μ = 1) se ven idénticos aunque estén lejos: el juego declaraba "peor" a uno de los
        /// dos por desempate arbitrario y mostraba "(0% vs 0%)". Medir la separación en μ garantiza que
        /// siempre haya una respuesta correcta visible. 0.3 ≈ el salto de verde a amarillo-naranja.
        /// </remarks>
        private const float MinMembershipGap = 0.3f;

        /// <summary>Tope de re-sorteos por ronda (siempre se encuentra uno en pocos intentos; es solo una red de seguridad).</summary>
        private const int MaxDraws = 200;

        [Header("Canvas / raíz")]
        [SerializeField] private RectTransform canvasRoot;

        [Header("Barra superior")]
        [SerializeField] private Button backButton;

        [Header("Personaje izquierdo (Reinhardt)")]
        [SerializeField] private Button leftButton;
        [SerializeField] private Image[] leftRing = new Image[4];
        [SerializeField] private Image[] leftGlow = new Image[4];
        [SerializeField] private TextMeshProUGUI leftBadge;

        [Header("Personaje derecho (Reaper)")]
        [SerializeField] private Button rightButton;
        [SerializeField] private Image[] rightRing = new Image[4];
        [SerializeField] private Image[] rightGlow = new Image[4];
        [SerializeField] private TextMeshProUGUI rightBadge;

        [Header("Resultado / control de ronda")]
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button nextRoundButton;
        [SerializeField] private TextMeshProUGUI scoreText;

        private float leftT;
        private float rightT;
        private bool roundActive;
        private int rounds;
        private int correctRounds;

        // Alpha original de cada capa de halo/núcleo (CreateGlowDot las deja en distintas opacidades
        // para simular el resplandor) — capturado UNA sola vez en Start(). Usar esto como base en vez
        // de leer layer.color.a cuadro a cuadro evita que el alpha se multiplique contra sí mismo cada
        // Update() y se apague progresivamente hasta desaparecer.
        private float[] leftGlowBaseAlpha;
        private float[] rightGlowBaseAlpha;

        private void Start() {
            if (!ValidateReferences()) return;

            leftGlowBaseAlpha = CaptureBaseAlpha(leftGlow);
            rightGlowBaseAlpha = CaptureBaseAlpha(rightGlow);

            backButton.onClick.AddListener(() => {
                MainMenuReturnState.SkipIntro = true;
                MainMenuReturnState.ReturnToModule = 2;
                SceneManager.LoadScene("MainScene");
            });

            leftButton.onClick.AddListener(() => OnChoose(chooseLeft: true));
            rightButton.onClick.AddListener(() => OnChoose(chooseLeft: false));
            nextRoundButton.onClick.AddListener(NewRound);

            NewRound();
        }

        private bool ValidateReferences() {
            bool ok = true;
            void Check(Object obj, string name) {
                if (!obj) {
                    Debug.LogError($"[FuzzyComparisonDemo] Falta la referencia '{name}'. Ejecuta Tools → Neuralizer → Módulo 2 → " +
                                    "5. Escena Ejercicio 3 (Triage) para regenerar la pantalla.", this);
                    ok = false;
                }
            }

            Check(canvasRoot, nameof(canvasRoot));
            Check(backButton, nameof(backButton));
            Check(leftButton, nameof(leftButton));
            Check(rightButton, nameof(rightButton));
            Check(instructionText, nameof(instructionText));
            Check(resultText, nameof(resultText));
            Check(nextRoundButton, nameof(nextRoundButton));
            Check(scoreText, nameof(scoreText));

            void CheckArray(Image[] arr, string name) {
                if (arr == null || arr.Length == 0 || System.Array.Exists(arr, r => !r)) {
                    Debug.LogError($"[FuzzyComparisonDemo] Falta '{name}'.", this);
                    ok = false;
                }
            }
            CheckArray(leftRing, nameof(leftRing));
            CheckArray(leftGlow, nameof(leftGlow));
            CheckArray(rightRing, nameof(rightRing));
            CheckArray(rightGlow, nameof(rightGlow));

            return ok;
        }

        // ---------------------------------------------------------------
        // Rondas
        // ---------------------------------------------------------------

        private void NewRound() {
            float a = 0f, b = 100f;
            for (int i = 0; i < MaxDraws; i++) {
                a = Random.Range(0f, 100f);
                b = Random.Range(0f, 100f);
                if (Mathf.Abs(Critico.Evaluate(a) - Critico.Evaluate(b)) >= MinMembershipGap) break;
            }

            leftT = Critico.Evaluate(a);
            rightT = Critico.Evaluate(b);

            roundActive = true;
            leftButton.interactable = true;
            rightButton.interactable = true;
            nextRoundButton.gameObject.SetActive(false);
            resultText.text = "";
            if (leftBadge) leftBadge.text = "";
            if (rightBadge) rightBadge.text = "";
            instructionText.text = "¿A quién curas primero? Elige sin pensarlo: mira el contorno.";
        }

        private void OnChoose(bool chooseLeft) {
            if (!roundActive) return;
            roundActive = false;
            leftButton.interactable = false;
            rightButton.interactable = false;
            nextRoundButton.gameObject.SetActive(true);

            bool leftWasWorse = leftT >= rightT;
            bool correct = chooseLeft == leftWasWorse;
            rounds++;
            if (correct) correctRounds++;
            scoreText.text = $"Aciertos: {correctRounds}/{rounds}";

            if (leftBadge) leftBadge.text = leftWasWorse ? "PEOR" : "";
            if (rightBadge) rightBadge.text = !leftWasWorse ? "PEOR" : "";

            int leftPct = Mathf.RoundToInt(leftT * 100f);
            int rightPct = Mathf.RoundToInt(rightT * 100f);
            string winnerName = leftWasWorse ? "Reinhardt" : "Reaper";
            instructionText.text = correct ? "¡Correcto!" : "No esta vez.";
            resultText.text = $"{winnerName} estaba en peor estado ({leftPct}% vs {rightPct}%).";
        }

        // ---------------------------------------------------------------
        // Estímulo visual — mismo lenguaje que FuzzyDamageDemo (verde→amarillo→rojo con pulso)
        // ---------------------------------------------------------------

        private void Update() {
            ApplyUrgencyColor(leftRing, leftGlow, leftGlowBaseAlpha, leftT);
            ApplyUrgencyColor(rightRing, rightGlow, rightGlowBaseAlpha, rightT);
        }

        private static float[] CaptureBaseAlpha(Image[] layers) {
            float[] result = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++) {
                result[i] = layers[i] ? layers[i].color.a : 1f;
            }
            return result;
        }

        private static void ApplyUrgencyColor(Image[] ring, Image[] glow, float[] glowBaseAlpha, float t) {
            Color c = t < 0.5f
                ? Color.Lerp(EsportsTheme.NeonGreen, EsportsTheme.CurveIntersection, t / 0.5f)
                : Color.Lerp(EsportsTheme.CurveIntersection, EsportsTheme.NeonRed, (t - 0.5f) / 0.5f);

            float pulseSpeed = Mathf.Lerp(0.6f, 6f, t);
            float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float ringAlpha = Mathf.Lerp(0.25f, 1f, t) * Mathf.Lerp(0.8f, 1f, pulse);

            foreach (Image bar in ring) {
                if (bar) bar.color = new Color(c.r, c.g, c.b, ringAlpha);
            }
            for (int i = 0; i < glow.Length; i++) {
                Image layer = glow[i];
                if (!layer) continue;
                layer.color = new Color(c.r, c.g, c.b, glowBaseAlpha[i] * Mathf.Lerp(0.3f, 1f, t));
            }
        }
    }
}
