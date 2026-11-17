using Neuralizer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Ejercicio 2 del Módulo 2 ("Lectura visual del daño"): demo aislada, sin números visibles,
    /// donde el usuario mueve un slider de "daño" y el único feedback es visual — el contorno y el
    /// resplandor del personaje pasan de verde a rojo siguiendo directo la función de pertenencia
    /// difusa μ_Crítico(x), igual que el contorno rojo de un aliado dañado en Overwatch. No hay
    /// acierto/error que evaluar acá (a diferencia de <see cref="FuzzyHealerDemo"/>): el objetivo es
    /// puramente perceptivo — que el usuario asocie "más rojo = más urgente" sin leer un número.
    /// </summary>
    /// <remarks>
    /// Vive en su propia escena (<c>FuzzyDamageDemo.unity</c>), no toca <c>MainScene</c> ni
    /// <c>FuzzyDemo.unity</c>. La jerarquía (Canvas, retrato, aro, slider, panel de revelado) la arma
    /// la herramienta de editor <c>Tools → Neuralizer → Módulo 2 → 4. Escena Ejercicio 2 (Lectura del Daño)</c>
    /// (ver <c>Assets/Scripts/Editor/Module2DamageDemoBuilder.cs</c>), que también deja las
    /// referencias de abajo ya asignadas — este script solo conecta comportamiento.
    ///
    /// El retrato (<see cref="heroImage"/>) es intercambiable desde el Inspector sin tocar código:
    /// cualquiera de los tres sprites en <c>Assets/Resources/Sprites/Module2/Heroes/</c>
    /// (Reaper/Reinhardt/Soldier 76, arte con licencia provisto por el usuario) sirve igual, porque el
    /// estímulo que importa es el color del aro/resplandor, no el personaje en sí.
    /// </remarks>
    public class FuzzyDamageDemo : MonoBehaviour {

        /// <summary>
        /// μ_Crítico(x) sobre el universo "% de vida perdida" (0-100), trapezoidal: sube desde 50,
        /// meseta de pertenencia total desde 70 en adelante. Mismos parámetros (a=50, b=70, c=100,
        /// d=100) discutidos para la variable "Daño" del módulo — ver conversación de diseño.
        /// </summary>
        private static readonly MembershipFunction Critico = MembershipFunction.Trapezoidal(50f, 70f, 100f, 100f);

        [Header("Canvas / raíz")]
        [SerializeField] private RectTransform canvasRoot;

        [Header("Barra superior")]
        [SerializeField] private Button backButton;

        [Header("Personaje")]
        [Tooltip("Retrato — intercambiable por cualquiera de los sprites en Resources/Sprites/Module2/Heroes/.")]
        [SerializeField] private Image heroImage;
        [Tooltip("Las 4 barras del contorno de urgencia alrededor del retrato — mismo lenguaje visual que el contorno de daño crítico de Overwatch.")]
        [SerializeField] private Image[] heroRing = new Image[4];
        [Tooltip("Halos + núcleo del resplandor detrás del retrato (ver EsportsUIKit.CreateGlowDot) — se tiñen junto con el aro.")]
        [SerializeField] private Image[] glowLayers = new Image[4];

        [Header("Control")]
        [Tooltip("Sin número visible a propósito — el usuario lee el estado solo por color/resplandor, no por el valor crudo.")]
        [SerializeField] private Slider damageSlider;

        [Header("Revelado (oculto hasta que el usuario lo pide)")]
        [SerializeField] private Button revealButton;
        [SerializeField] private RectTransform curvePanel;
        [SerializeField] private RectTransform curveArea;
        [SerializeField] private UIPolyline curveLine;
        [SerializeField] private UIFilledCurve curveFill;
        [SerializeField] private RectTransform curveMarker;
        [SerializeField] private TextMeshProUGUI formulaText;
        [SerializeField] private TextMeshProUGUI valueReadout;

        private bool revealed;

        private void Start() {
            if (!ValidateReferences()) return;

            backButton.onClick.AddListener(() => {
                MainMenuReturnState.SkipIntro = true;
                MainMenuReturnState.ReturnToModule = 2;
                SceneManager.LoadScene("MainScene");
            });

            damageSlider.minValue = 0f;
            damageSlider.maxValue = 100f;
            damageSlider.onValueChanged.AddListener(OnDamageChanged);

            revealButton.onClick.AddListener(ToggleReveal);
            curvePanel.gameObject.SetActive(false);

            if (formulaText) formulaText.text = "crítico(x) = trapezoidal(50, 70, 100, 100)";

            RedrawCurve();
            // Arranca en 0 (totalmente "a salvo") — el usuario descubre la transición moviendo el
            // slider, no la ve ya a mitad de camino al entrar.
            damageSlider.value = 0f;
            OnDamageChanged(0f);
        }

        private bool ValidateReferences() {
            bool ok = true;
            void Check(Object obj, string name) {
                if (!obj) {
                    Debug.LogError($"[FuzzyDamageDemo] Falta la referencia '{name}'. Ejecuta Tools → Neuralizer → Módulo 2 → " +
                                    "4. Escena Ejercicio 2 (Lectura del Daño) para regenerar la pantalla, o arrastra " +
                                    "el objeto correspondiente a mano en el Inspector.", this);
                    ok = false;
                }
            }

            Check(canvasRoot, nameof(canvasRoot));
            Check(backButton, nameof(backButton));
            Check(heroImage, nameof(heroImage));
            Check(damageSlider, nameof(damageSlider));
            Check(revealButton, nameof(revealButton));
            Check(curvePanel, nameof(curvePanel));
            Check(curveArea, nameof(curveArea));
            Check(curveLine, nameof(curveLine));
            Check(curveFill, nameof(curveFill));
            Check(curveMarker, nameof(curveMarker));

            if (heroRing == null || heroRing.Length != 4 || System.Array.Exists(heroRing, r => !r)) {
                Debug.LogError("[FuzzyDamageDemo] Falta 'heroRing' (deben ser las 4 barras del contorno).", this);
                ok = false;
            }
            if (glowLayers == null || glowLayers.Length == 0 || System.Array.Exists(glowLayers, r => !r)) {
                Debug.LogError("[FuzzyDamageDemo] Falta 'glowLayers' (halos + núcleo del resplandor).", this);
                ok = false;
            }

            return ok;
        }

        // ---------------------------------------------------------------
        // Estímulo visual
        // ---------------------------------------------------------------

        private void OnDamageChanged(float value) {
            float t = Critico.Evaluate(value);
            ApplyUrgencyColor(t);
            if (valueReadout) valueReadout.text = $"{Mathf.RoundToInt(t * 100f)}%"; // solo visible dentro del panel de revelado
            if (revealed) UpdateMarker(value, t);
        }

        /// <summary>Verde → amarillo → rojo, con pulso que se acelera e intensifica mientras más alto es t — misma fórmula que el aro de urgencia de <see cref="FuzzyHealerDemo"/>.</summary>
        private void ApplyUrgencyColor(float t) {
            Color c = t < 0.5f
                ? Color.Lerp(EsportsTheme.NeonGreen, EsportsTheme.CurveIntersection, t / 0.5f)
                : Color.Lerp(EsportsTheme.CurveIntersection, EsportsTheme.NeonRed, (t - 0.5f) / 0.5f);

            float pulseSpeed = Mathf.Lerp(0.6f, 6f, t);
            float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float ringAlpha = Mathf.Lerp(0.25f, 1f, t) * Mathf.Lerp(0.8f, 1f, pulse);

            foreach (Image bar in heroRing) {
                if (bar) bar.color = new Color(c.r, c.g, c.b, ringAlpha);
            }
            foreach (Image layer in glowLayers) {
                if (!layer) continue;
                // Preserva el alpha relativo de cada capa de halo (CreateGlowDot las deja en distintas
                // opacidades para simular el resplandor) — solo se reemplaza el tono, escalado por t.
                float baseAlpha = layer.color.a;
                layer.color = new Color(c.r, c.g, c.b, baseAlpha * Mathf.Lerp(0.3f, 1f, t));
            }
        }

        // ---------------------------------------------------------------
        // Revelado: la curva real, recién después de que el usuario ya miró/decidió con el ojo
        // ---------------------------------------------------------------

        private void ToggleReveal() {
            revealed = !revealed;
            curvePanel.gameObject.SetActive(revealed);
            if (revealed) {
                // Recalcular acá (no solo en Start()) es necesario: UIPolyline/UIFilledCurve no
                // reconstruyen su malla mientras el GameObject está inactivo, así que los puntos
                // seteados en Start() (con el panel todavía apagado) nunca llegaban a dibujarse al
                // activar el panel por primera vez — quedaba el área de la curva vacía.
                Canvas.ForceUpdateCanvases();
                RedrawCurve();
                UpdateMarker(damageSlider.value, Critico.Evaluate(damageSlider.value));
            }
        }

        private void RedrawCurve() {
            const int samples = 48;
            Rect rect = curveArea.rect;
            Vector2[] points = new Vector2[samples];
            for (int i = 0; i < samples; i++) {
                float tx = i / (float)(samples - 1);
                float x = Mathf.Lerp(0f, 100f, tx);
                points[i] = new Vector2(tx * rect.width, Critico.Evaluate(x) * rect.height * 0.88f);
            }
            curveLine.SetPoints(points);
            curveFill.SetPoints(points);
        }

        private void UpdateMarker(float value, float t) {
            Rect rect = curveArea.rect;
            float tx = Mathf.InverseLerp(0f, 100f, value);
            curveMarker.anchoredPosition = new Vector2(tx * rect.width, t * rect.height * 0.88f);
        }
    }
}
