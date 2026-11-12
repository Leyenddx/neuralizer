using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Movimiento de un elemento de UI del Módulo 2: entrada (fundido + escala 0.96→1, con retraso
    /// para escalonar listas), feedback al presionar (escala 0.97) y halo al pasar el puntero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Solo anima <c>localScale</c> y la opacidad de un <see cref="CanvasGroup"/>: los LayoutGroup de
    /// uGUI controlan posición y tamaño, así que animar esas propiedades pelearía con el layout. La
    /// escala no la toca ningún layout.
    /// </para>
    /// <para>
    /// La entrada nunca bloquea la interacción (el CanvasGroup no corta raycasts) y nunca arranca desde
    /// escala 0. Con <see cref="ReducedMotion"/> se quedan solo los fundidos.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class M2Motion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {

        /// <summary>Modo de movimiento reducido: sin escalas ni desplazamientos, solo fundidos.</summary>
        public static bool ReducedMotion;

        [Header("Entrada")]
        [SerializeField] private bool playEntrance;
        [SerializeField] private float entranceDelay;
        [SerializeField] private float entranceFromScale = 0.96f;

        [Header("Presionar")]
        [SerializeField] private bool pressFeedback = true;
        [SerializeField] private float pressScale = 0.97f;

        [Header("Halo al pasar el puntero (opcional)")]
        [SerializeField] private Graphic hoverGlow;
        [SerializeField] private float glowIdleAlpha;
        [SerializeField] private float glowHoverAlpha = 0.5f;

        private CanvasGroup group;
        private float entranceStart = -1f;
        private float pressFrom = 1f, pressTo = 1f, pressStart, pressDuration = M2Ease.Press;
        private float glowFrom, glowTo, glowStart;
        private bool pointerInside;
        private bool animating;

        /// <summary>Configura la entrada (lo usan los builders/controladores al crear el elemento).</summary>
        public void SetEntrance(float delay, float fromScale = 0.96f) {
            playEntrance = true;
            entranceDelay = delay;
            entranceFromScale = fromScale;
        }

        public void SetPressFeedback(bool enabled, float scale = 0.97f) {
            pressFeedback = enabled;
            pressScale = scale;
        }

        public void SetHoverGlow(Graphic glow, float idleAlpha, float hoverAlpha) {
            hoverGlow = glow;
            glowIdleAlpha = idleAlpha;
            glowHoverAlpha = hoverAlpha;
            glowFrom = glowTo = idleAlpha;
            ApplyGlow(idleAlpha);
        }

        /// <summary>Vuelve a reproducir la entrada (p. ej. al mostrar de nuevo un panel).</summary>
        public void Replay() {
            if (!playEntrance || !isActiveAndEnabled) return;
            StartEntrance();
        }

        private void OnEnable() {
            pressFrom = pressTo = 1f;
            pointerInside = false;
            if (hoverGlow) {
                glowFrom = glowTo = glowIdleAlpha;
                ApplyGlow(glowIdleAlpha);
            }
            if (playEntrance && Application.isPlaying) StartEntrance();
            else Apply(1f, 1f);
        }

        private void OnDisable() {
            // Deja el elemento en su estado final: si se desactiva a mitad de animación, la próxima
            // vez no debe aparecer "a medias".
            Apply(1f, 1f);
            if (group) group.alpha = 1f;
            entranceStart = -1f;
            animating = false;
        }

        private void StartEntrance() {
            if (!group) group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            entranceStart = Time.unscaledTime + entranceDelay;
            animating = true;
            Apply(ReducedMotion ? 1f : entranceFromScale, 1f);
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!pressFeedback || ReducedMotion) return;
            StartPress(pressScale, M2Ease.Press);
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (!pressFeedback || ReducedMotion) return;
            StartPress(1f, M2Ease.Release);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            pointerInside = true;
            StartGlow(glowHoverAlpha);
        }

        public void OnPointerExit(PointerEventData eventData) {
            pointerInside = false;
            StartGlow(glowIdleAlpha);
            if (pressFeedback && !ReducedMotion && pressTo < 1f) StartPress(1f, M2Ease.Release);
        }

        private void StartPress(float target, float duration) {
            pressFrom = CurrentPress();
            pressTo = target;
            pressStart = Time.unscaledTime;
            pressDuration = duration;
            animating = true;
        }

        private void StartGlow(float target) {
            if (!hoverGlow) return;
            glowFrom = hoverGlow.color.a;
            glowTo = target;
            glowStart = Time.unscaledTime;
            animating = true;
        }

        private float CurrentPress() {
            float p = Mathf.Clamp01((Time.unscaledTime - pressStart) / pressDuration);
            return Mathf.LerpUnclamped(pressFrom, pressTo, M2Ease.Out(p));
        }

        private void Update() {
            if (!animating) return;
            float now = Time.unscaledTime;
            bool busy = false;

            float entranceScale = 1f;
            if (entranceStart >= 0f) {
                float p = Mathf.Clamp01((now - entranceStart) / M2Ease.Enter);
                float e = M2Ease.Out(p);
                if (group) group.alpha = now < entranceStart ? 0f : e;
                entranceScale = ReducedMotion ? 1f : Mathf.LerpUnclamped(entranceFromScale, 1f, e);
                if (p >= 1f) entranceStart = -1f;
                else busy = true;
            }

            float press = CurrentPress();
            if (now - pressStart < pressDuration) busy = true;

            if (hoverGlow) {
                float g = Mathf.Clamp01((now - glowStart) / M2Ease.Hover);
                ApplyGlow(Mathf.Lerp(glowFrom, glowTo, M2Ease.Out(g)));
                if (g < 1f) busy = true;
            }

            Apply(entranceScale, press);
            animating = busy || pointerInside && hoverGlow && !Mathf.Approximately(hoverGlow.color.a, glowTo);
        }

        private void Apply(float entranceScale, float press) {
            float s = entranceScale * press;
            transform.localScale = new Vector3(s, s, 1f);
        }

        private void ApplyGlow(float alpha) {
            if (!hoverGlow) return;
            Color c = hoverGlow.color;
            c.a = alpha;
            hoverGlow.color = c;
        }

        /// <summary>Agrega (o reutiliza) un <see cref="M2Motion"/> con entrada escalonada.</summary>
        public static M2Motion AddEntrance(GameObject go, float delay, float fromScale = 0.96f) {
            M2Motion m = go.GetComponent<M2Motion>();
            if (!m) {
                m = go.AddComponent<M2Motion>();
                m.pressFeedback = false;
            }
            m.SetEntrance(delay, fromScale);
            return m;
        }
    }
}
