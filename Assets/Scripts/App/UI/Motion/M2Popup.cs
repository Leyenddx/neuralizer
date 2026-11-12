using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>
    /// Modal animado: el fondo oscuro se funde y la tarjeta entra con escala 0.96→1 + opacidad
    /// (240 ms ease-out). Al cerrar sale más rápido (150 ms) y recién al terminar se desactiva.
    /// Va en la raíz del popup (el fondo oscuro, que lleva un <see cref="CanvasGroup"/>).
    /// </summary>
    /// <remarks>
    /// Un modal no está anclado a un botón, así que escala desde el centro (la excepción a la regla de
    /// "escalar desde el disparador"). Se puede interrumpir: abrir mientras se cierra retoma desde el
    /// estado actual en vez de reiniciar.
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    public class M2Popup : MonoBehaviour {

        [SerializeField] private RectTransform card;
        [SerializeField] private float fromScale = 0.96f;

        private CanvasGroup group;
        private float from, to, start, duration;
        private bool closing;
        private bool running;

        public bool IsOpen => gameObject.activeSelf && !closing;

        public void SetCard(RectTransform cardRect) => card = cardRect;

        private void Awake() {
            group = GetComponent<CanvasGroup>();
        }

        /// <summary>Muestra el popup con animación de entrada.</summary>
        public void Open() {
            if (!group) group = GetComponent<CanvasGroup>();
            float current = gameObject.activeSelf ? group.alpha : 0f;
            gameObject.SetActive(true);
            closing = false;
            group.blocksRaycasts = true;
            group.interactable = true;
            Begin(current, 1f, M2Ease.ModalIn);
        }

        /// <summary>Oculta el popup con animación de salida (más rápida) y luego lo desactiva.</summary>
        public void Close() {
            if (!gameObject.activeSelf) return;
            closing = true;
            // Deja pasar los toques de inmediato: el usuario ya decidió cerrar, no debe esperar la animación.
            group.blocksRaycasts = false;
            group.interactable = false;
            Begin(group.alpha, 0f, M2Ease.ModalOut);
        }

        /// <summary>Oculta sin animar (estado inicial al cargar la escena).</summary>
        public void HideImmediate() {
            if (!group) group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            running = false;
            closing = false;
            gameObject.SetActive(false);
        }

        private void Begin(float fromValue, float toValue, float dur) {
            from = fromValue;
            to = toValue;
            start = Time.unscaledTime;
            duration = dur;
            running = true;
            Apply(fromValue);
        }

        private void Update() {
            if (!running) return;
            float p = Mathf.Clamp01((Time.unscaledTime - start) / duration);
            Apply(Mathf.Lerp(from, to, M2Ease.Out(p)));
            if (p < 1f) return;
            running = false;
            if (closing) {
                closing = false;
                gameObject.SetActive(false);
            }
        }

        private void Apply(float v) {
            group.alpha = v;
            if (!card) return;
            float s = M2Motion.ReducedMotion ? 1f : Mathf.LerpUnclamped(fromScale, 1f, v);
            card.localScale = new Vector3(s, s, 1f);
        }
    }
}
